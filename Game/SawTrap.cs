using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

public enum SawPlacement { Floating, FloorMounted }

/// <summary>
/// A saw blade trap that moves back and forth along a path and damages the player on contact.
/// </summary>
public class SawTrap : Trap
{
    // Movement range from starting position (in pixels)
    public float MoveRange { get; set; } = 150f;

    // Movement speed in pixels per second
    public float MoveSpeed { get; set; } = 80f;

    // Blade size in pixels (width and height)
    public float BladeSize { get; set; } = 30f;

    // Axis of movement: true = horizontal (X), false = vertical (Y)
    public bool MoveHorizontal { get; set; } = true;

    // Floating saw uses row 0, floor-mounted saw uses row 1
    public SawPlacement Placement { get; set; } = SawPlacement.Floating;

    // Frames per row in the saw spritesheet (Large/Medium = 4, Small = 3)
    public int AnimationColumns { get; set; } = 4;

    // Animation speed for sprite-sheet saws
    public float AnimationFrameDuration { get; set; } = 0.06f;

    private Vector2 _startPosition;
    private float _moveDirection = 1f;
    private Animator _animator;

    protected override void OnInitialize()
    {
        Damage = 1;
        _startPosition = Position;

        SpriteTextureName = SpriteTextureName ?? "pixel";
        var texture = ResourceManager.Instance.GetTexture(SpriteTextureName);
        bool usingPixel = texture == ResourceManager.Instance.GetTexture("pixel");
        if (usingPixel)
            SpriteTint = Color.Red;

        ApplySpriteTexture(new Vector2(BladeSize, BladeSize));

        if (!usingPixel)
            TrySetupSawAnimation(texture);

        if (usingPixel)
            Scale = new Vector2(BladeSize, BladeSize);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = WorldTime.Dt((float)gameTime.ElapsedGameTime.TotalSeconds);

        if (MoveHorizontal)
        {
            Position = new Vector2(Position.X + MoveSpeed * _moveDirection * dt, Position.Y);

            float distanceMoved = Position.X - _startPosition.X;
            if (distanceMoved >= MoveRange) _moveDirection = -1f;
            else if (distanceMoved <= 0f)   _moveDirection =  1f;
        }
        else
        {
            Position = new Vector2(Position.X, Position.Y + MoveSpeed * _moveDirection * dt);

            float distanceMoved = Position.Y - _startPosition.Y;
            if (distanceMoved >= MoveRange) _moveDirection = -1f;
            else if (distanceMoved <= 0f)   _moveDirection =  1f;
        }
    }

    protected override void OnPlayerEnter(Player player)
    {
        player.Die();
    }

    private void TrySetupSawAnimation(Texture2D texture)
    {
        if (texture == null) return;

        const int rows = 2;
        int columns = Math.Max(1, AnimationColumns);

        if (texture.Width < columns || texture.Height < rows) return;

        int row = Placement == SawPlacement.FloorMounted ? 1 : 0;

        var factory = new AnimationFactory(texture, rows: rows, columns: columns);
        var spin = factory.CreateFromRow(
            row: row,
            totalFrames: columns,
            frameDuration: AnimationFrameDuration,
            isLooping: true
        );

        _animator = AddComponent<Animator>();
        _animator.AddAnimation("spin", spin);
        _animator.Play("spin");

        // Animator draws a frame (not full sheet), so scale from frame size.
        // Use uniform scale to avoid stretching when frame width/height are not equal.
        float frameWidth = texture.Width / (float)columns;
        float frameHeight = texture.Height / (float)rows;
        float uniformScale = BladeSize / MathF.Max(frameWidth, frameHeight);
        Scale = new Vector2(uniformScale, uniformScale);
    }
}
