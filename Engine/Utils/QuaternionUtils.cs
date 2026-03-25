using System;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Utils;

/// <summary>
/// Static helpers for working with rotations stored as Vector3 Euler angles (radians)
/// and XNA Quaternions. All angle parameters are in radians unless the method name
/// contains "Degrees".
/// </summary>
public static class QuaternionUtils
{
    private const float Epsilon = 1e-6f;

    /// <summary>Converts a Vector3 of degree angles to radians.</summary>
    public static Vector3 Euler(Vector3 euler) => new(
        MathHelper.ToRadians(euler.X),
        MathHelper.ToRadians(euler.Y),
        MathHelper.ToRadians(euler.Z));

    /// <summary>Converts degree angles to a radian Vector3.</summary>
    public static Vector3 Euler(float x, float y, float z)
        => Euler(new Vector3(x, y, z));

    /// <summary>Converts a radian Vector3 to degrees.</summary>
    public static Vector3 EulerDegrees(Vector3 euler) => new(
        MathHelper.ToDegrees(euler.X),
        MathHelper.ToDegrees(euler.Y),
        MathHelper.ToDegrees(euler.Z));

    /// <summary>Converts radian angles to a degree Vector3.</summary>
    public static Vector3 EulerDegrees(float x, float y, float z)
        => EulerDegrees(new Vector3(x, y, z));

    // -------------------------------------------------------------------------
    // Angle math
    // -------------------------------------------------------------------------

    /// <summary>
    /// Wraps an angle to the range (-π, π].
    /// </summary>
    public static float WrapAngle(float radians)
    {
        radians %= MathF.Tau;                         // bring into (-2π, 2π)
        if (radians > MathF.PI)  radians -= MathF.Tau;
        if (radians <= -MathF.PI) radians += MathF.Tau;
        return radians;
    }

    /// <summary>Wraps all three Euler angles to (-π, π].</summary>
    public static Vector3 WrapEuler(Vector3 euler) => new(
        WrapAngle(euler.X),
        WrapAngle(euler.Y),
        WrapAngle(euler.Z));

    /// <summary>
    /// Returns the shortest signed angular difference (in radians) from
    /// <paramref name="current"/> to <paramref name="target"/>.
    /// Result is in the range (-π, π].
    /// </summary>
    public static float DeltaAngle(float current, float target)
        => WrapAngle(target - current);

    /// <summary>Per-axis shortest signed delta between two Euler angles.</summary>
    public static Vector3 DeltaEuler(Vector3 current, Vector3 target) => new(
        DeltaAngle(current.X, target.X),
        DeltaAngle(current.Y, target.Y),
        DeltaAngle(current.Z, target.Z));

    /// <summary>
    /// Returns the absolute angle (radians) between two quaternions.
    /// </summary>
    public static float Angle(Quaternion a,
                              Quaternion b)
    {
        float dot = Math.Abs(Quaternion.Dot(a, b));
        return 2f * MathF.Acos(Math.Clamp(dot, 0f, 1f));
    }

    /// <summary>Clamps an angle to [min, max] (radians), respecting wrap-around.</summary>
    public static float ClampAngle(float angle, float min, float max)
    {
        angle = WrapAngle(angle);
        return Math.Clamp(angle, min, max);
    }

    /// <summary>Clamps each Euler axis independently.</summary>
    public static Vector3 ClampEuler(Vector3 euler, Vector3 min, Vector3 max) => new(
        ClampAngle(euler.X, min.X, max.X),
        ClampAngle(euler.Y, min.Y, max.Y),
        ClampAngle(euler.Z, min.Z, max.Z));

    // -------------------------------------------------------------------------
    // Interpolation
    // -------------------------------------------------------------------------

    /// <summary>Linearly interpolates each Euler axis. <paramref name="t"/> clamped to [0,1].</summary>
    public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector3(
            MathHelper.Lerp(a.X, b.X, t),
            MathHelper.Lerp(a.Y, b.Y, t),
            MathHelper.Lerp(a.Z, b.Z, t));
    }

    /// <summary>
    /// Lerp that always takes the shortest path for each axis.
    /// </summary>
    public static Vector3 LerpAngle(Vector3 a, Vector3 b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return new Vector3(
            a.X + DeltaAngle(a.X, b.X) * t,
            a.Y + DeltaAngle(a.Y, b.Y) * t,
            a.Z + DeltaAngle(a.Z, b.Z) * t);
    }

    /// <summary>
    /// Spherical linear interpolation between two quaternions (t clamped to [0,1]).
    /// </summary>
    public static Quaternion Slerp(
        Quaternion a,
        Quaternion b,
        float t)
        => Quaternion.Slerp(a, b, Math.Clamp(t, 0f, 1f));

    // -------------------------------------------------------------------------
    // Move / step
    // -------------------------------------------------------------------------

    /// <summary>
    /// Moves <paramref name="current"/> toward <paramref name="target"/> by at most
    /// <paramref name="maxDelta"/> radians per axis (never overshoots).
    /// </summary>
    public static float MoveTowardsAngle(float current, float target, float maxDelta)
    {
        float delta = DeltaAngle(current, target);
        if (MathF.Abs(delta) <= maxDelta) return target;
        return current + MathF.Sign(delta) * maxDelta;
    }

    /// <summary>Per-axis <see cref="MoveTowardsAngle"/>.</summary>
    public static Vector3 MoveTowardsEuler(Vector3 current, Vector3 target, float maxDelta) => new(
        MoveTowardsAngle(current.X, target.X, maxDelta),
        MoveTowardsAngle(current.Y, target.Y, maxDelta),
        MoveTowardsAngle(current.Z, target.Z, maxDelta));

    /// <summary>
    /// Rotates <paramref name="from"/> toward <paramref name="to"/> by at most
    /// <paramref name="maxRadiansDelta"/>, never overshooting.
    /// </summary>
    public static Quaternion RotateTowards(
        Quaternion from,
        Quaternion to,
        float maxRadiansDelta)
    {
        float angle = Angle(from, to);
        if (angle < Epsilon) return to;
        float t = Math.Min(1f, maxRadiansDelta / angle);
        return Quaternion.Slerp(from, to, t);
    }

    // -------------------------------------------------------------------------
    // Smooth damp (velocity-based)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Gradually changes an angle toward a target, similar to SmoothDamp.
    /// Updates <paramref name="currentVelocity"/> in place.
    /// </summary>
    public static float SmoothDampAngle(float current, float target,
        ref float currentVelocity, float smoothTime, float deltaTime,
        float maxSpeed = float.MaxValue)
    {
        target = current + DeltaAngle(current, target);

        float omega = 2f / smoothTime;
        float x = omega * deltaTime;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);

        float delta = current - target;
        float maxDelta = maxSpeed * smoothTime;
        delta = Math.Clamp(delta, -maxDelta, maxDelta);

        float temp = (currentVelocity + omega * delta) * deltaTime;
        currentVelocity = (currentVelocity - omega * temp) * exp;
        float result = target + (delta + temp) * exp;

        // Prevent overshoot
        if ((target - current > 0f) == (result > target))
        {
            result = target;
            currentVelocity = 0f;
        }
        return result;
    }

    /// <summary>Per-axis <see cref="SmoothDampAngle"/> for a full Euler Vector3.</summary>
    public static Vector3 SmoothDampEuler(Vector3 current, Vector3 target,
        ref Vector3 velocity, float smoothTime, float deltaTime,
        float maxSpeed = float.MaxValue)
    {
        float vx = velocity.X, vy = velocity.Y, vz = velocity.Z;
        Vector3 result = new(
            SmoothDampAngle(current.X, target.X, ref vx, smoothTime, deltaTime, maxSpeed),
            SmoothDampAngle(current.Y, target.Y, ref vy, smoothTime, deltaTime, maxSpeed),
            SmoothDampAngle(current.Z, target.Z, ref vz, smoothTime, deltaTime, maxSpeed));
        velocity = new Vector3(vx, vy, vz);
        return result;
    }

    // -------------------------------------------------------------------------
    // Direction helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the Z rotation (radians) needed to face a 2D direction vector.
    /// </summary>
    public static float LookAt2D(Vector2 direction)
        => MathF.Atan2(direction.Y, direction.X);

    /// <summary>
    /// Returns the Z rotation (radians) for a GameObject at <paramref name="from"/>
    /// to face the point <paramref name="to"/>.
    /// </summary>
    public static float LookAt2D(Vector2 from, Vector2 to)
        => LookAt2D(to - from);

    /// <summary>
    /// Converts a Z rotation angle (radians) to a normalised 2D direction vector.
    /// </summary>
    public static Vector2 AngleToDirection(float radians)
        => new(MathF.Cos(radians), MathF.Sin(radians));

    // -------------------------------------------------------------------------
    // Quaternion factory helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a rotation of <paramref name="radians"/> around <paramref name="axis"/>.
    /// </summary>
    public static Quaternion AngleAxis(float radians, Vector3 axis)
        => Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), radians);

    /// <summary>
    /// Creates the shortest-arc rotation from <paramref name="from"/> to <paramref name="to"/>.
    /// </summary>
    public static Quaternion FromToRotation(Vector3 from, Vector3 to)
    {
        from.Normalize(); to.Normalize();
        float dot = Vector3.Dot(from, to);

        if (dot < -1f + Epsilon)
        {
            Vector3 perp = Vector3.Cross(Vector3.UnitX, from);
            if (perp.LengthSquared() < Epsilon)
                perp = Vector3.Cross(Vector3.UnitY, from);
            perp.Normalize();
            return Quaternion.CreateFromAxisAngle(perp, MathF.PI);
        }
        if (dot > 1f - Epsilon)
            return Quaternion.Identity;

        Vector3 axis = Vector3.Cross(from, to);
        var q = new Quaternion(axis.X, axis.Y, axis.Z, 1f + dot);
        q.Normalize();
        return q;
    }

    /// <summary>
    /// Converts a GameObject Euler Vector3 (radians, XYZ) to a Quaternion.
    /// </summary>
    public static Quaternion ToQuaternion(Vector3 euler)
    {
        var qx = Quaternion.CreateFromAxisAngle(Vector3.UnitX, euler.X);
        var qy = Quaternion.CreateFromAxisAngle(Vector3.UnitY, euler.Y);
        var qz = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, euler.Z);
        return Quaternion.Concatenate(
               Quaternion.Concatenate(qx, qy), qz);
    }

    /// <summary>
    /// Converts a Quaternion back to Euler angles (radians, XYZ).
    /// </summary>
    public static Vector3 ToEuler(Quaternion q)
    {
        q.Normalize();
        float roll  = MathF.Atan2(2f * (q.W * q.X + q.Y * q.Z),
                                  1f - 2f * (q.X * q.X + q.Y * q.Y));
        float sinp  = 2f * (q.W * q.Y - q.Z * q.X);
        float pitch = MathF.Abs(sinp) >= 1f
            ? MathF.CopySign(MathF.PI / 2f, sinp)
            : MathF.Asin(sinp);
        float yaw   = MathF.Atan2(2f * (q.W * q.Z + q.X * q.Y),
                                  1f - 2f * (q.Y * q.Y + q.Z * q.Z));
        return new Vector3(roll, pitch, yaw);
    }


    /// <summary>
    /// Returns true if two Euler angles represent approximately the same orientation
    /// (per-axis difference less than <paramref name="tolerance"/> radians).
    /// </summary>
    public static bool Approximately(Vector3 a, Vector3 b, float tolerance = Epsilon)
    {
        Vector3 d = DeltaEuler(a, b);
        return MathF.Abs(d.X) < tolerance
            && MathF.Abs(d.Y) < tolerance
            && MathF.Abs(d.Z) < tolerance;
    }
}