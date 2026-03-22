using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

/// <summary>
/// A laser beam trap that damages the player when they cross the beam.
/// The laser can be toggled on/off on a timer or by a trigger.
/// </summary>
public class LaserTrap : Trap
{
    // How long the laser stays ON / OFF in seconds
    public float OnDuration { get; set; } = 2f;
    public float OffDuration { get; set; } = 1f;

    // Direction of the laser beam: horizontal or vertical
    public bool IsHorizontal { get; set; } = true;

    private float _timer = 0f;
    private bool _beamOn = true;

    protected override void OnInitialize()
    {
        Damage = 1;

        // TODO: Load laser sprite / texture
        // _spriteRenderer.Texture = ResourceManager.Instance.GetTexture("laser");
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

        // Toggle beam on/off based on duration
        if (_beamOn && _timer >= OnDuration)
        {
            _beamOn = false;
            _timer = 0f;
        }
        else if (!_beamOn && _timer >= OffDuration)
        {
            _beamOn = true;
            _timer = 0f;
        }

        // TODO: Update laser beam visual (enable/disable sprite or draw line)
    }

    protected override void OnPlayerEnter(Player player)
    {
        if (!_beamOn) return;

        // TODO: Deal damage to player
        // player.TakeDamage(Damage);
    }
}