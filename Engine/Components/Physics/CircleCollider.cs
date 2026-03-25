using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components.Physics;

public class CircleCollider : Collider
{
    public float Radius { get; set; }

    public Vector2 GetCenter()
    {
        return base.GameObject.Position + Offset;
    }

    public override bool IsIntersect(Collider other)
    {
        return other.IsIntersect(this);
    }

    // Circle vs Circle collision
    public override bool IsIntersect(CircleCollider other)
    {
        float distanceSquared = Vector2.DistanceSquared(GetCenter(), other.GetCenter());
        float radiusSum = Radius + other.Radius;

        return distanceSquared <= radiusSum * radiusSum;
    }

    // Circle vs Box collision
    public override bool IsIntersect(BoxCollider other)
    {
        Vector2 circleCenter = GetCenter();
        Vector2 boxCenter = other.GetCenter();
        var worldBounds = other.WorldBounds;
        Vector2 boxHalfSize = new Vector2(worldBounds.Width / 2f, worldBounds.Height / 2f);

        Vector2 difference = circleCenter - boxCenter;
        Vector2 clamped = Vector2.Clamp(difference, -boxHalfSize, boxHalfSize);
        Vector2 closest = boxCenter + clamped;

        difference = closest - circleCenter;
        
        return difference.LengthSquared() <= Radius * Radius;
    }

    // ── MTV ──────────────────────────────────────────────────────────────────

    public override Vector2 GetMTV(Collider other)
    {
        return other switch
        {
            CircleCollider circleCollider => GetMTV(circleCollider),
            BoxCollider boxCollider => GetMTV(boxCollider),
            _ => Vector2.Zero,
        };
    }

    // Circle vs Circle
    public override Vector2 GetMTV(CircleCollider other)
    {
        Vector2 delta = GetCenter() - other.GetCenter();
        float dist = delta.Length();
        float overlap = Radius + other.Radius - dist;
        if (overlap <= 0f) return Vector2.Zero;

        Vector2 normal = dist > 1e-6f ? delta / dist : Vector2.UnitY;
        return normal * overlap;
    }

    // Circle vs Box
    public override Vector2 GetMTV(BoxCollider other)
    {
        Vector2 circleCenter = GetCenter();
        Vector2 boxCenter = other.GetCenter();
        var worldBounds = other.WorldBounds;
        Vector2 boxHalf = new Vector2(worldBounds.Width / 2f, worldBounds.Height / 2f);

        Vector2 diff    = circleCenter - boxCenter;
        Vector2 clamped = Vector2.Clamp(diff, -boxHalf, boxHalf);
        Vector2 closest = boxCenter + clamped;
        Vector2 toCircle = circleCenter - closest;

        float dist = toCircle.Length();
        if (dist <= 1e-6f)
        {
            float left = circleCenter.X - worldBounds.Left;
            float right = worldBounds.Right - circleCenter.X;
            float top = circleCenter.Y - worldBounds.Top;
            float bottom = worldBounds.Bottom - circleCenter.Y;

            float minPenetration = left;
            Vector2 normal = -Vector2.UnitX;

            if (right < minPenetration)
            {
                minPenetration = right;
                normal = Vector2.UnitX;
            }

            if (top < minPenetration)
            {
                minPenetration = top;
                normal = -Vector2.UnitY;
            }

            if (bottom < minPenetration)
            {
                minPenetration = bottom;
                normal = Vector2.UnitY;
            }

            return normal * (Radius + minPenetration);
        }

        float overlap = Radius - dist;
        if (overlap <= 0f) return Vector2.Zero;

        Vector2 direction = toCircle / dist;
        return direction * overlap;
    }
}