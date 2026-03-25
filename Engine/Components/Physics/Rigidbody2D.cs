using System;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components.Physics;

/// <summary>
/// Simple 2D rigid body. Handles velocity integration and gravity.
/// Depenetration (position correction) is driven by Scene.ProcessCollisions.
/// </summary>
public class Rigidbody2D : Component
{
    // ── Properties ────────────────────────────────────────────────────────────

    /// <summary>Current velocity in pixels per second.</summary>
    public Vector2 Velocity { get; set; } = Vector2.Zero;

    /// <summary>Mass in arbitrary units. Used to split depenetration impulse.</summary>
    public float Mass { get; set; } = 1f;

    /// <summary>
    /// When true the body is moved by code only (e.g. a platform).
    /// It still blocks dynamic bodies but is never pushed by the solver.
    /// </summary>
    public bool IsKinematic { get; set; } = false;

    /// <summary>Gravity scale. 0 = no gravity (e.g. top-down).</summary>
    public float GravityScale { get; set; } = 1f;

    /// <summary>Linear drag — fraction of velocity removed per second (0–1).</summary>
    public float Drag { get; set; } = 0f;

    /// <summary>Bounciness (coefficient of restitution). 0 = no bounce, 1 = perfect.</summary>
    public float Bounciness { get; set; } = 0f;

    // ── Gravity constant (pixels/s²) ─────────────────────────────────────────
    public static float Gravity { get; set; } = 980f;

    // ── Integration ───────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        if (IsKinematic) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Apply gravity
        Velocity += Vector2.UnitY * Gravity * GravityScale * dt;

        // Apply drag
        if (Drag > 0f)
            Velocity *= MathF.Max(0f, 1f - Drag * dt);

        // Integrate position
        GameObject.Position += Velocity * dt;
    }

    /// <summary>
    /// Called by the physics solver to apply an impulse that separates two bodies.
    /// Also reflects the velocity component along the collision normal.
    /// </summary>
    internal void ApplyDepenetration(Vector2 correction, Vector2 normal,
                                     float otherBounciness)
    {
        if (IsKinematic) return;

        GameObject.Position += correction;

        // Reflect velocity along the collision normal
        float restitution = MathF.Max(Bounciness, otherBounciness);
        float vn = Vector2.Dot(Velocity, normal);
        if (vn < 0f)                                 // only if moving into the surface
            Velocity -= (1f + restitution) * vn * normal;
    }
}
