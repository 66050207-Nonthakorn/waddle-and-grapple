using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using System;

namespace ComputerGameFinal.Game;

/// <summary>
/// A CCTV camera that rotates and detects the player within its field of view.
/// When the player is spotted, it triggers an alert.
/// </summary>
public class CCTV : GameObject
{
    // Detection range in pixels
    public float DetectionRange { get; set; } = 200f;

    // Field of view angle in degrees (e.g. 60 = sees in a 60° cone)
    public float FieldOfViewDegrees { get; set; } = 60f;

    // Rotation sweep speed in degrees per second
    public float SweepSpeed { get; set; } = 45f;

    // Min and max angle of sweep (relative to facing direction, in degrees)
    public float SweepMinAngle { get; set; } = -45f;
    public float SweepMaxAngle { get; set; } = 45f;

    public bool IsAlerted { get; private set; } = false;

    /// <summary>Reference to the player. Set this from the scene after creating the CCTV.</summary>
    public Player Player { get; set; }

    private SpriteRenderer _spriteRenderer;
    private float _currentSweepAngle = 0f;
    private float _sweepDirection = 1f;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.4f;

        // TODO: Load CCTV sprite
        // _spriteRenderer.Texture = ResourceManager.Instance.GetTexture("cctv");
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Sweep(dt);

        if (Player != null && CanSeePlayer(Player))
            OnPlayerSpotted(Player);
    }

    private void Sweep(float dt)
    {
        _currentSweepAngle += SweepSpeed * _sweepDirection * dt;

        if (_currentSweepAngle >= SweepMaxAngle)
        {
            _currentSweepAngle = SweepMaxAngle;
            _sweepDirection = -1f;
        }
        else if (_currentSweepAngle <= SweepMinAngle)
        {
            _currentSweepAngle = SweepMinAngle;
            _sweepDirection = 1f;
        }

        // Apply to visual rotation
        Rotation = MathHelper.ToRadians(_currentSweepAngle);
    }

    private bool CanSeePlayer(Player player)
    {
        Vector2 toPlayer = player.Position - Position;
        float distance = toPlayer.Length();

        if (distance > DetectionRange) return false;

        // Check if player is within the field of view cone
        float angleToPlayer = MathHelper.ToDegrees((float)Math.Atan2(toPlayer.Y, toPlayer.X));
        float diff = Math.Abs(angleToPlayer - _currentSweepAngle);

        return diff <= FieldOfViewDegrees / 2f;

        // TODO: Add raycast / line-of-sight check (no walls in between)
    }

    private void OnPlayerSpotted(Player player)
    {
        if (IsAlerted) return;
        IsAlerted = true;

        // TODO: Play alert sound, trigger enemies, switch to alert state
        // AudioManager.Instance.Play("alert");
    }

    public void ResetAlert()
    {
        IsAlerted = false;
    }
}
