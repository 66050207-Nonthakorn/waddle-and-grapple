using System;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components.Physics;

public class Transform : Component
{
    public Vector2 Forward
        => new Vector2((float)Math.Cos(base.GameObject.Rotation.Z), (float)Math.Sin(base.GameObject.Rotation.Z));

    public void Translate(Vector2 distance)
    {
        base.GameObject.Position += distance;
    }
    
    public void Rotate(float amount)
    {
        base.GameObject.Rotation += new Vector3(0, 0, amount);
    }
}