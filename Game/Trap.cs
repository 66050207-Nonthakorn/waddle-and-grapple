using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Abstract base class for all traps in the game.
/// </summary>
public abstract class Trap : GameObject
{
    public int Damage { get; protected set; } = 1;
    public bool IsActive { get; protected set; } = true;

    /// <summary>Reference to the player. Set this from the scene after creating the trap.</summary>
    public Player Player { get; set; }

    /// <summary>Enemies that this trap can kill. Set from the level after spawning.</summary>
    public List<Enemy> Enemies { get; set; } = new();

    /// <summary>Optional sprite texture name for the trap.</summary>
    public string SpriteTextureName { get; set; } = "pixel";

    /// <summary>Optional tint to apply when using a sprite texture.</summary>
    public Color SpriteTint { get; set; } = Color.White;

    protected SpriteRenderer _spriteRenderer;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();
        OnInitialize();
    }

    protected void ApplySpriteTexture(Vector2? targetSize = null)
    {
        if (_spriteRenderer == null) return;

        var texture = ResourceManager.Instance.GetTexture(SpriteTextureName)
            ?? ResourceManager.Instance.GetTexture("pixel");
        var pixelTexture = ResourceManager.Instance.GetTexture("pixel");

        if (texture == null) return;

        _spriteRenderer.Texture = texture;
        _spriteRenderer.Tint    = SpriteTint;
        _spriteRenderer.LayerDepth = 0.6f;

        if (targetSize.HasValue && texture != pixelTexture)
        {
            _spriteRenderer.Origin = Vector2.Zero;
            ApplySpriteScale(targetSize.Value);
        }
    }

    protected void ApplySpriteScale(Vector2 targetSize)
    {
        if (_spriteRenderer == null || _spriteRenderer.Texture == null) return;

        var texture = _spriteRenderer.Texture;
        var pixelTexture = ResourceManager.Instance.GetTexture("pixel");

        if (texture == pixelTexture)
        {
            Scale = targetSize;
        }
        else
        {
            _spriteRenderer.Origin = Vector2.Zero;
            Scale = new Vector2(targetSize.X / texture.Bounds.Width,
                                targetSize.Y / texture.Bounds.Height);
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive) return;
        OnUpdate(gameTime);

        if (Player != null && IsPlayerInRange(Player))
            OnPlayerEnter(Player);

        foreach (var enemy in Enemies)
        {
            if (enemy.Active && GetCollisionBounds().Intersects(enemy.ColliderBounds))
                OnEnemyEnter(enemy);
        }
    }

    protected virtual Rectangle GetCollisionBounds() =>
        new Rectangle((int)Position.X, (int)Position.Y, (int)Scale.X, (int)Scale.Y);

    private bool IsPlayerInRange(Player player) =>
        GetCollisionBounds().Intersects(player.ColliderBounds);

    /// <summary>Called once when the trap is set up. Override to load sprites, set size, etc.</summary>
    protected virtual void OnInitialize() { }

    /// <summary>Called every frame while trap is active. Override to add movement or animation logic.</summary>
    protected virtual void OnUpdate(GameTime gameTime) { }

    /// <summary>Called when a player enters the trap's trigger area.</summary>
    protected abstract void OnPlayerEnter(Player player);

    /// <summary>Called when an enemy enters the trap's trigger area. Default: kills the enemy.</summary>
    protected virtual void OnEnemyEnter(Enemy enemy) => enemy.Die();

    public virtual void Activate()
    {
        IsActive = true;
    }

    public virtual void Deactivate()
    {
        IsActive = false;
    }
}