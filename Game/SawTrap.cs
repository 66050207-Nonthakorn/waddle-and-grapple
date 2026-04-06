using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Saw placement — controls which quadrant(s) of the sprite are rendered
/// and where the attachment point (Position) sits relative to the visible blade.
/// </summary>
/// <summary>Discrete size tiers matching the sprite sheets (1 tile = 75 px).</summary>
public enum SawSize
{
    Small  =  16,   // 1×1 tile,  spritesheet 48×32,  3 cols
    Medium = 32,   // 2×2 tiles, spritesheet 128×64, 4 cols
    Large  = 64,   // 4×4 tiles, spritesheet 256×128,4 cols
}

public enum SawPlacement
{
    Full,              // Full blade, Position = centre
    FloorMounted,      // Top half visible, Position = bottom-centre (floor contact)
    CeilingMounted,    // Bottom half visible, Position = top-centre (ceiling contact)
    LeftWallMounted,   // Left half visible, Position = right-centre (wall contact)
    RightWallMounted,  // Right half visible, Position = left-centre (wall contact)
}

/// <summary>
/// A saw blade trap that moves back and forth and damages the player on contact.
/// Rendering is handled by SawRenderer.
/// </summary>
public class SawTrap : Trap
{
    // Movement
    public float MoveRange      { get; set; } = 150f;
    public float MoveSpeed      { get; set; } = 80f;
    public bool  MoveHorizontal { get; set; } = true;

    // Appearance
    /// <summary>Discrete size tiers: Small=1 tile (75px), Medium=2 tiles (150px), Large=4 tiles (300px).</summary>
    public SawSize Size { get; set; } = SawSize.Medium;

    /// <summary>Rendered size of the full blade in pixels (derived from Size).</summary>
    public float BladeSize => (float)Size;

    /// <summary>
    /// Which portion of the blade is visible (for wall/floor/ceiling mounting).
    /// Small saws (AnimationColumns == 3) always use Full regardless of this value.
    /// </summary>
    public SawPlacement Placement { get; set; } = SawPlacement.Full;

    /// <summary>Number of animation frames (columns) in the spritesheet row.</summary>
    public int   AnimationColumns       { get; set; } = 4;
    public float AnimationFrameDuration { get; set; } = 0.06f;

    private Vector2 _startPosition;
    private float   _moveDirection = 1f;

    protected override void OnInitialize()
    {
        Damage = 1;
        _startPosition    = Position;
        SpriteTextureName = SpriteTextureName ?? "pixel";
        if (Size == SawSize.Small) // Small saws always use the Full blade spritesheet row, even if Placement is set to something else.
            AnimationColumns = 3;
        AddComponent<SawRenderer>();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = WorldTime.Dt((float)gameTime.ElapsedGameTime.TotalSeconds);

        if (MoveHorizontal)
        {
            Position = new Vector2(Position.X + MoveSpeed * _moveDirection * dt, Position.Y);
            float d = Position.X - _startPosition.X;
            if (d >= MoveRange) _moveDirection = -1f;
            else if (d <= 0f)   _moveDirection =  1f;
        }
        else
        {
            Position = new Vector2(Position.X, Position.Y + MoveSpeed * _moveDirection * dt);
            float d = Position.Y - _startPosition.Y;
            if (d >= MoveRange) _moveDirection = -1f;
            else if (d <= 0f)   _moveDirection =  1f;
        }
    }

    protected override void OnPlayerEnter(Player player) => player.Die();

    protected override Rectangle GetCollisionBounds()
    {
        int x    = (int)Position.X;
        int y    = (int)Position.Y;
        int half = (int)(BladeSize * 0.5f);
        int full = (int)BladeSize;

        // Small saw (3 cols) is always a full circle.
        var p = AnimationColumns == 3 ? SawPlacement.Full : Placement;

        return p switch
        {
            SawPlacement.Full =>
                new Rectangle(x - half, y - half, full, full),

            SawPlacement.FloorMounted =>
                new Rectangle(x - half, y - half, full, half),

            SawPlacement.CeilingMounted =>
                new Rectangle(x - half, y,        full, half),

            SawPlacement.LeftWallMounted =>
                new Rectangle(x,        y - half, half, full),

            SawPlacement.RightWallMounted =>
                new Rectangle(x - half, y - half, half, full),

            _ => base.GetCollisionBounds()
        };
    }
}
