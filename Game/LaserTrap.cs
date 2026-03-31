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

    // Length of the beam in pixels (head + beam + tail)
    public float BeamLength { get; set; } = 200f;

    // If true, beam never toggles off (ignores OnDuration/OffDuration)
    public bool AlwaysOn { get; set; } = false;

    // Read by LaserRenderer to decide which colour to draw
    public bool BeamOn => _beamOn;

    private float _timer = 0f;
    private bool _beamOn = true;

    protected override void OnInitialize()
    {
        Damage = 1;

        // Collision bounding box = full head-to-tail rectangle
        float E = LaserRenderer.EndpointSize;
        Scale = IsHorizontal
            ? new Vector2(BeamLength, E)
            : new Vector2(E, BeamLength);

        AddComponent<LaserRenderer>();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (AlwaysOn) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _timer += dt;

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
    }

    protected override void OnPlayerEnter(Player player)
    {
        if (!_beamOn) return;
        player.Die();
    }
}