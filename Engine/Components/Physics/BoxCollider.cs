using System;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components.Physics;

public class BoxCollider : Collider
{
    public Rectangle Bounds { get; set; }

    private float WorldLeft => GameObject.Position.X + Offset.X + Bounds.X;
    private float WorldTop => GameObject.Position.Y + Offset.Y + Bounds.Y;
    private float WorldRight => WorldLeft + Bounds.Width;
    private float WorldBottom => WorldTop + Bounds.Height;

    public Rectangle WorldBounds => new(
        (int)WorldLeft,
        (int)WorldTop,
        Bounds.Width,
        Bounds.Height
    );

    // public override void Draw(SpriteBatch spriteBatch)
    // {        
    //     spriteBatch.Draw(
    //         ResourceManager.Instance.GetTexture("pixel"), 
    //         Bounds, 
    //         null, 
    //         IsTrigger ? Color.Red * 0.5f : Color.Green * 0.5f, 
    //         0f, 
    //         Vector2.Zero, 
    //         SpriteEffects.None, 
    //         0f
    //     );
    // }

    public Vector2 GetCenter()
    {
        return new Vector2(
            WorldLeft + Bounds.Width / 2f,
            WorldTop + Bounds.Height / 2f
        );
    }

    public override bool IsIntersect(Collider other)
    {
        return other.IsIntersect(this);
    }

    // Box vs Circle collision (Reuse from CircleCollider)
    public override bool IsIntersect(CircleCollider other)
    {
        return other.IsIntersect(this);
    }

    // Box vs Box collision        
    public override bool IsIntersect(BoxCollider other)
    {
        return WorldLeft < other.WorldRight
            && WorldRight > other.WorldLeft
            && WorldTop < other.WorldBottom
            && WorldBottom > other.WorldTop;
    }

    // ── MTV ──────────────────────────────────────────────────────────────────

    public override Vector2 GetMTV(Collider other)
    {
        return other switch
        {
            BoxCollider boxCollider => GetMTV(boxCollider),
            CircleCollider circleCollider => GetMTV(circleCollider),
            _ => Vector2.Zero,
        };
    }

    public override Vector2 GetMTV(CircleCollider other) => -other.GetMTV(this);

    // Box vs Box: push along the axis of least penetration
    public override Vector2 GetMTV(BoxCollider other)
    {
        float overlapX = Math.Min(WorldRight, other.WorldRight) - Math.Max(WorldLeft, other.WorldLeft);
        float overlapY = Math.Min(WorldBottom, other.WorldBottom) - Math.Max(WorldTop, other.WorldTop);

        if (overlapX <= 0 || overlapY <= 0) return Vector2.Zero;

        Vector2 center = GetCenter();
        Vector2 otherCenter = other.GetCenter();

        if (overlapX <= overlapY)
        {
            float sign = center.X < otherCenter.X ? -1f : 1f;
            return new Vector2(sign * overlapX, 0f);
        }
        else
        {
            float sign = center.Y < otherCenter.Y ? -1f : 1f;
            return new Vector2(0f, sign * overlapY);
        }
    }
}
