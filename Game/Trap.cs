using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

/// <summary>
/// Abstract base class for all traps in the game.
/// </summary>
public abstract class Trap : GameObject
{
    public int Damage { get; protected set; } = 1;
    public bool IsActive { get; protected set; } = true;

    /// <summary>Reference to the player. Set this from the scene after creating the trap.</summary>
    public Player Player { get; set; }

    protected SpriteRenderer _spriteRenderer;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();
        OnInitialize();
    }

    public override void Update(GameTime gameTime)
    {
        if (!IsActive) return;
        OnUpdate(gameTime);

        if (Player != null && IsPlayerInRange(Player))
            OnPlayerEnter(Player);
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

    public virtual void Activate()
    {
        IsActive = true;
    }

    public virtual void Deactivate()
    {
        IsActive = false;
    }
}