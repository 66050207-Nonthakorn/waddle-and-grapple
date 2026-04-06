using System;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// A spike trap that extends and retracts from a surface.
/// Rendering is tile-based (see SpikeRenderer).
///
/// RotationAngle controls the extension direction:
///   0        = pointing UP   (floor spike)
///   PI/2     = pointing RIGHT (left-wall spike)
///   PI       = pointing DOWN  (ceiling spike)
///   -PI/2    = pointing LEFT  (right-wall spike)
///   any other value = custom angle
/// </summary>
public class SpikeTrap : Trap
{
    /// <summary>Extension direction in radians. 0 = up, PI/2 = right, PI = down, -PI/2 = left.</summary>
    public float RotationAngle  { get; set; } = 0f;

    /// <summary>Total number of tiles when fully extended (minimum 1).</summary>
    public int   SpikeTiles     { get; set; } = 3;

    public float ExtendDuration  { get; set; } = 0.2f;
    public float HoldDuration    { get; set; } = 1.0f;
    public float RetractDuration { get; set; } = 0.2f;
    public float PauseDuration   { get; set; } = 1.5f;

    /// <summary>Phase shift so multiple spikes don't activate simultaneously.</summary>
    public float PhaseOffset { get; set; } = 0f;

    /// <summary>Current extension fraction (0.0 to 1.0). Read by SpikeRenderer.</summary>
    public float ExtensionRatio => _extensionRatio;

    private enum SpikeState { Paused, Extending, Extended, Retracting }
    private SpikeState _spikeState = SpikeState.Paused;
    private float      _stateTimer;
    private float      _extensionRatio;

    protected override void OnInitialize()
    {
        Damage            = 1;
        _stateTimer       = PhaseOffset;
        SpriteTextureName = SpriteTextureName ?? "pixel";
        AddComponent<SpikeRenderer>();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = WorldTime.Dt((float)gameTime.ElapsedGameTime.TotalSeconds);
        _stateTimer += dt;

        switch (_spikeState)
        {
            case SpikeState.Paused:
                _extensionRatio = 0f;
                if (_stateTimer >= PauseDuration)
                { _spikeState = SpikeState.Extending; _stateTimer = 0f; }
                break;

            case SpikeState.Extending:
                _extensionRatio = Math.Min(_stateTimer / ExtendDuration, 1f);
                if (_stateTimer >= ExtendDuration)
                { _spikeState = SpikeState.Extended; _stateTimer = 0f; }
                break;

            case SpikeState.Extended:
                _extensionRatio = 1f;
                if (_stateTimer >= HoldDuration)
                { _spikeState = SpikeState.Retracting; _stateTimer = 0f; }
                break;

            case SpikeState.Retracting:
                _extensionRatio = Math.Max(1f - _stateTimer / RetractDuration, 0f);
                if (_stateTimer >= RetractDuration)
                { _spikeState = SpikeState.Paused; _stateTimer = 0f; }
                break;
        }
    }

    protected override Rectangle GetCollisionBounds()
    {
        int ts  = SpikeRenderer.TileSize;
        var dir = new Vector2(MathF.Sin(RotationAngle), -MathF.Cos(RotationAngle));
        var right = new Vector2(MathF.Cos(RotationAngle), MathF.Sin(RotationAngle));

        // คำนวณขอบเขตแนวนอน (ซ้ายสุดไปขวาสุดของความกว้างหนามทั้งหมด)
        var p1 = Position - right * (ts * 0.5f);
        var p2 = Position + right * ((SpikeTiles - 0.5f) * ts);
        var p3 = p1 + dir * ts;
        var p4 = p2 + dir * ts;

        int minX = (int)Math.Min(Math.Min(p1.X, p2.X), Math.Min(p3.X, p4.X));
        int minY = (int)Math.Min(Math.Min(p1.Y, p2.Y), Math.Min(p3.Y, p4.Y));
        int maxX = (int)Math.Max(Math.Max(p1.X, p2.X), Math.Max(p3.X, p4.X));
        int maxY = (int)Math.Max(Math.Max(p1.Y, p2.Y), Math.Max(p3.Y, p4.Y));
        return new Rectangle(minX, minY, maxX - minX, maxY - minY);
    }

    protected override void OnPlayerEnter(Player player)
    {
        if (_spikeState == SpikeState.Paused) return;
        player.Die();
    }
}
