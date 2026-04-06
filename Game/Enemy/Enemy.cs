using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Abstract base class for all enemies.
/// Provides the common interface used by Player, IcePickaxe, and the level loader.
/// </summary>
public abstract class Enemy : GameObject
{
    /// <summary>Axis-aligned bounding box used for collision detection.</summary>
    public abstract Rectangle ColliderBounds { get; }

    /// <summary>Kill this enemy immediately.</summary>
    public abstract void Die();

    /// <summary>Provide the player reference so the enemy can track and react to them.</summary>
    public abstract void SetPlayer(Player player);

    /// <summary>Provide the solid tile rectangles for physics collision.</summary>
    public abstract void SetSolids(List<Rectangle> solids);

    /// <summary>
    /// The key under which this enemy was registered in the scene.
    /// Set by the level so the enemy can remove itself on death.
    /// </summary>
    public string SceneKey { get; set; }

    /// <summary>Reset enemy back to spawn position (optional — override if supported).</summary>
    public virtual void ResetToSpawn() { }
}
