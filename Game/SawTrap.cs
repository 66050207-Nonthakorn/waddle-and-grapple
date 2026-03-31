using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

/// <summary>
/// A saw blade trap that moves back and forth along a path and damages the player on contact.
/// </summary>
public class SawTrap : Trap
{
    // Movement range from starting position (in pixels)
    public float MoveRange { get; set; } = 150f;

    // Movement speed in pixels per second
    public float MoveSpeed { get; set; } = 80f;

    // Axis of movement: true = horizontal (X), false = vertical (Y)
    public bool MoveHorizontal { get; set; } = true;

    private Vector2 _startPosition;
    private float _moveDirection = 1f;

    protected override void OnInitialize()
    {
        Damage = 1;
        _startPosition = Position;

        Scale = new Vector2(30, 30);
        _spriteRenderer.Texture    = ResourceManager.Instance.GetTexture("pixel");
        _spriteRenderer.Tint       = Color.Red;
        _spriteRenderer.LayerDepth = 0.6f;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (MoveHorizontal)
        {
            Position = new Vector2(Position.X + MoveSpeed * _moveDirection * dt, Position.Y);

            float distanceMoved = Position.X - _startPosition.X;
            if (distanceMoved >= MoveRange) _moveDirection = -1f;
            else if (distanceMoved <= 0f)   _moveDirection =  1f;
        }
        else
        {
            Position = new Vector2(Position.X, Position.Y + MoveSpeed * _moveDirection * dt);

            float distanceMoved = Position.Y - _startPosition.Y;
            if (distanceMoved >= MoveRange) _moveDirection = -1f;
            else if (distanceMoved <= 0f)   _moveDirection =  1f;
        }
    }

    protected override void OnPlayerEnter(Player player)
    {
        player.Die();
    }
}
