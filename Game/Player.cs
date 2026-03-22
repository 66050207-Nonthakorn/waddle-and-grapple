using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Physics;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace ComputerGameFinal.Game;

public class Player : GameObject
{
    private const float MoveSpeed = 300f;

    /// <summary>The level's original start position, used as fallback when no section is active.</summary>
    public Vector2 StartPosition { get; set; }

    private SpriteRenderer _spriteRenderer;
    private CircleCollider _collider;

    // Player stats
    public int Health { get; private set; } = 3;
    public bool IsAlive => Health > 0;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;
        _spriteRenderer.Texture = ResourceManager.Instance.GetTexture("bird");

        // Add collider for collision detection
        _collider = AddComponent<CircleCollider>();
        _collider.Radius = 16f; // Adjust radius as needed
        _collider.IsTrigger = true; // For checkpoint activation
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsAlive) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Vector2 speed = Vector2.Zero;

        if (InputManager.Instance.IsKeyDown(Keys.A))
        {
            speed.X = -MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.D))
        {
            speed.X = MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.W))
        {
            speed.Y = -MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.S))
        {
            speed.Y = MoveSpeed;
        }

        Position += speed * dt;

        // Notify CheckpointManager of current X position so section transitions are detected.
        CheckpointManager.Instance.UpdateSection(Position.X);
    }

    /// <summary>Called when player takes damage from traps.</summary>
    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        Health -= damage;
        if (Health <= 0)
        {
            Die();
        }
        else
        {
            // TODO: Play hurt sound and animation
            // Respawn at last checkpoint
            Respawn();
        }
    }

    private void Die()
    {
        // TODO: Play death animation and sound
        // For now, just respawn
        Respawn();
    }

    private void Respawn()
    {
        Position = CheckpointManager.Instance.GetRespawnPosition(StartPosition);

        if (!IsAlive)
            Health = 3;
    }
}