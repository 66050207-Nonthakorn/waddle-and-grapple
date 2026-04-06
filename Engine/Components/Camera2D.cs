using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.Components;

public class Camera2D : Component
{
    private Viewport _viewport;
    private float _zoom;
    private float _rotation;
    private Vector2 _position;
    private Matrix _transformMatrix;
    private bool _isViewTransformationDirty = true;

    // Screen shake
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private readonly Random _random = new Random();

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            _isViewTransformationDirty = true;
        }
    }

    public float Rotation
    {
        get => _rotation;
        set
        {
            _rotation = value;
            _isViewTransformationDirty = true;
        }
    }

    public float Zoom
    {
        get => _zoom;
        set
        {
            _zoom = MathHelper.Clamp(value, 0.1f, 10f);
            _isViewTransformationDirty = true;
        }
    }

    public Vector2 Origin { get; private set; }
    public Matrix TransformMatrix
    {
        get
        {
            if (_isViewTransformationDirty)
            {
                UpdateTransformMatrix();
            }
            return _transformMatrix;
        }
    }

    // Follow target settings
    public GameObject FollowTarget { get; set; }
    public float FollowSpeed { get; set; } = 5f;
    public bool SmoothFollow { get; set; } = true;

    // Bounds (optional - for limiting camera movement)
    public Rectangle? Bounds { get; set; }

    // Section clamp — camera X cannot go outside this range (set by CheckpointManager section)
    public float? ClampMinX { get; set; }
    public float? ClampMaxX { get; set; }

    public Camera2D()
    {
        _zoom = 1f;
        _rotation = 0f;
        _position = Vector2.Zero;
    }

    public void SetViewport(Viewport viewport)
    {
        _viewport = viewport;
        Origin = new Vector2(_viewport.Width / 2f, _viewport.Height / 2f);
        _isViewTransformationDirty = true;
    }

    public override void Initialize()
    {
        // Default viewport - can be overridden
        if (_viewport.Width == 0)
        {
            _viewport = new Viewport(0, 0, 800, 600);
            Origin = new Vector2(_viewport.Width / 2f, _viewport.Height / 2f);
        }
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Follow target logic
        if (FollowTarget != null)
        {
            Vector2 targetPosition = FollowTarget.Position;

            if (SmoothFollow)
            {
                _position = Vector2.Lerp(_position, targetPosition, FollowSpeed * dt);
            }
            else
            {
                _position = targetPosition;
            }

            _isViewTransformationDirty = true;
        }

        // Section clamp (Celeste-style: camera can't move past section boundary)
        if (ClampMinX.HasValue)
            _position.X = Math.Max(_position.X, ClampMinX.Value);
        if (ClampMaxX.HasValue)
            _position.X = Math.Min(_position.X, ClampMaxX.Value);
        if (ClampMinX.HasValue || ClampMaxX.HasValue)
            _isViewTransformationDirty = true;

        // Apply bounds constraints
        if (Bounds.HasValue)
        {
            Vector2 min = new Vector2(
                Bounds.Value.Left + Origin.X / Zoom,
                Bounds.Value.Top + Origin.Y / Zoom
            );
            Vector2 max = new Vector2(
                Bounds.Value.Right - Origin.X / Zoom,
                Bounds.Value.Bottom - Origin.Y / Zoom
            );

            _position = Vector2.Clamp(_position, min, max);
            _isViewTransformationDirty = true;
        }

        // Screen shake update
        if (_shakeTimer > 0)
        {
            _shakeTimer -= dt;
            if (_shakeTimer <= 0)
            {
                _shakeIntensity = 0;
                _isViewTransformationDirty = true;
            }
        }
    }

    private void UpdateTransformMatrix()
    {
        Vector2 shakeOffset = Vector2.Zero;
        if (_shakeTimer > 0)
        {
            shakeOffset = new Vector2(
                ((float)_random.NextDouble() - 0.5f) * 2f * _shakeIntensity,
                ((float)_random.NextDouble() - 0.5f) * 2f * _shakeIntensity
            );
        }

        _transformMatrix =
            Matrix.CreateTranslation(new Vector3(-_position - shakeOffset, 0f)) *
            Matrix.CreateRotationZ(_rotation) *
            Matrix.CreateScale(_zoom, _zoom, 1f) *
            Matrix.CreateTranslation(new Vector3(Origin, 0f));

        _isViewTransformationDirty = false;
    }

    /// <summary>
    /// Shake the camera for a specified duration and intensity
    /// </summary>
    public void Shake(float intensity, float duration)
    {
        _shakeIntensity = intensity;
        _shakeDuration = duration;
        _shakeTimer = duration;
        _isViewTransformationDirty = true;
    }

    /// <summary>
    /// Convert world position to screen position
    /// </summary>
    public Vector2 WorldToScreen(Vector2 worldPosition)
    {
        return Vector2.Transform(worldPosition, TransformMatrix);
    }

    /// <summary>
    /// Convert screen position to world position
    /// </summary>
    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        return Vector2.Transform(screenPosition, Matrix.Invert(TransformMatrix));
    }

    /// <summary>
    /// Get the visible area of the world in camera view
    /// </summary>
    public Rectangle GetVisibleArea()
    {
        Vector2 topLeft = ScreenToWorld(Vector2.Zero);
        Vector2 bottomRight = ScreenToWorld(new Vector2(_viewport.Width, _viewport.Height));

        return new Rectangle(
            (int)topLeft.X,
            (int)topLeft.Y,
            (int)(bottomRight.X - topLeft.X),
            (int)(bottomRight.Y - topLeft.Y)
        );
    }

    /// <summary>
    /// Check if a point is visible in the camera view
    /// </summary>
    public bool IsInView(Vector2 worldPosition)
    {
        Vector2 screenPos = WorldToScreen(worldPosition);
        return screenPos.X >= 0 && screenPos.X <= _viewport.Width &&
               screenPos.Y >= 0 && screenPos.Y <= _viewport.Height;
    }
}