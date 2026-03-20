using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components.Physics;

public abstract class Collider : Component
{
    public bool IsTrigger { get; set; }
    public Vector2 Offset { get; set; }

    public abstract bool IsIntersect(Collider other);
    public abstract bool IsIntersect(CircleCollider other);
    public abstract bool IsIntersect(BoxCollider other);

    ///  Minimum Translation Vector - the smallest vector to separate two colliders
    
    public abstract Vector2 GetMTV(Collider other);
    public abstract Vector2 GetMTV(CircleCollider other);
    public abstract Vector2 GetMTV(BoxCollider other);
}
