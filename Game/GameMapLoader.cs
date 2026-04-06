using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components.Tile;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Game-specific map loader that pre-registers every spawnable object type.
///
/// Create one per level, then call Load() to produce tile layers and game objects.
///
/// Tiled object "Class" → C# type mapping:
///   "SawTrap"           → SawTrap
///   "LaserTrap"         → LaserTrap
///   "SpikeTrap"         → SpikeTrap
///   "Enemy"             → Enemy
///   "Coin"              → Coin
///   "GoalFlag"          → GoalFlag
///   "DoubleJumpPowerUp" → DoubleJumpPowerUp
///   "SpeedBoostPowerUp" → SpeedBoostPowerUp
///   "SlowTimePowerUp"   → SlowTimePowerUp
///
/// Properties you can set on each Tiled object (all optional; defaults match class defaults):
///
///   SawTrap:
///     moveRange          (float)  - travel distance in px           default 150
///     moveSpeed          (float)  - px / second                     default 80
///     bladeSize          (float)  - width and height in px          default 30
///     moveHorizontal     (bool)   - true = X axis, false = Y axis   default true
///     placement          (enum)   - Floating | FloorMounted         default Floating
///     spriteName         (string) - ResourceManager texture key
///
///   LaserTrap:
///     beamLength         (float)  - total beam length in px         default 200
///     isHorizontal       (bool)   - true = horizontal beam          default true
///     alwaysOn           (bool)   - skip toggle cycle               default false
///     onDuration         (float)  - seconds beam stays ON           default 2
///     offDuration        (float)  - seconds beam stays OFF          default 1
///     style              (enum)   - WallMounted | Floating          default WallMounted
///     endpointScale      (float)  - visual scale of caps            default 1
///     beamThicknessScale (float)  - visual scale of beam thickness  default 1
///
///   SpikeTrap:
///     origin             (enum)   - Floor | Ceiling | LeftWall | RightWall  default Floor
///     spikeLength        (float)  - spike width in px              default 45
///     phaseOffset        (float)  - stagger offset in seconds      default 0
///     extendDuration     (float)                                   default 0.2
///     holdDuration       (float)                                   default 1.0
///     retractDuration    (float)                                   default 0.2
///     pauseDuration      (float)                                   default 1.5
///
///   Enemy:
///     patrolRadius       (float)  - half-width of patrol zone       default 150
///     detectionRange     (float)  - aggro distance                  default 250
///     attackRange        (float)  - melee reach                     default 60
///     leashRange         (float)  - max distance before returning   default 400
///     patrolSpeed        (float)                                    default 100
///     chaseSpeed         (float)                                    default 200
///
///   Coin:
///     value              (int)    - coins added on pickup           default 1
///
///   GoalFlag: (no extra properties needed)
///
///   DoubleJumpPowerUp / SpeedBoostPowerUp / SlowTimePowerUp: (no extra properties)
/// </summary>
public class GameMapLoader
{
    private readonly MapLoader _loader;
    private Player _player;

    public GameMapLoader(Scene scene, string mapPath, Player player)
    {
        _player = player;
        _loader = new MapLoader(scene, mapPath);
        RegisterAll();
    }

    // ── Load ─────────────────────────────────────────────────────────────────

    /// <summary>Load the map, create tile layers, and spawn all registered objects.</summary>
    public MapLoader.LoadResult Load(
        Texture2D tileset,
        float baseLayer = 0.5f,
        int[] solidTileIndices = null)
        => _loader.Load(tileset, baseLayer, solidTileIndices);

    // ── Registration ─────────────────────────────────────────────────────────

    private void RegisterAll()
    {
        RegisterSawTrap();
        RegisterLaserTrap();
        RegisterSpikeTrap();
        RegisterEnemy();
        RegisterCoin();
        RegisterGoalFlag();
        RegisterDoubleJumpPowerUp();
        RegisterSpeedBoostPowerUp();
        RegisterSlowTimePowerUp();
    }

    private void RegisterSawTrap()
    {
        _loader.Register<SawTrap>("SawTrap", (trap, obj) =>
        {
            trap.MoveRange             = obj.FloatProp("moveRange",     trap.MoveRange);
            trap.MoveSpeed             = obj.FloatProp("moveSpeed",     trap.MoveSpeed);
            trap.BladeSize             = obj.FloatProp("bladeSize",     trap.BladeSize);
            trap.MoveHorizontal        = obj.BoolProp ("moveHorizontal",trap.MoveHorizontal);
            trap.Placement             = obj.EnumProp ("placement",     trap.Placement);
            trap.AnimationColumns      = obj.IntProp  ("animationColumns", trap.AnimationColumns);
            trap.AnimationFrameDuration= obj.FloatProp("animationFrameDuration", trap.AnimationFrameDuration);
            if (obj.Properties.ContainsKey("spriteName"))
                trap.SpriteTextureName = obj.StringProp("spriteName");
            trap.Player = _player;
        });
    }

    private void RegisterLaserTrap()
    {
        _loader.Register<LaserTrap>("LaserTrap", (laser, obj) =>
        {
            laser.BeamLength          = obj.FloatProp("beamLength",          laser.BeamLength);
            laser.IsHorizontal        = obj.BoolProp ("isHorizontal",        laser.IsHorizontal);
            laser.AlwaysOn            = obj.BoolProp ("alwaysOn",            laser.AlwaysOn);
            laser.OnDuration          = obj.FloatProp("onDuration",          laser.OnDuration);
            laser.OffDuration         = obj.FloatProp("offDuration",         laser.OffDuration);
            laser.Style               = obj.EnumProp ("style",               laser.Style);
            laser.EndpointScale       = obj.FloatProp("endpointScale",       laser.EndpointScale);
            laser.BeamThicknessScale  = obj.FloatProp("beamThicknessScale",  laser.BeamThicknessScale);
            laser.Player = _player;
        });
    }

    private void RegisterSpikeTrap()
    {
        _loader.Register<SpikeTrap>("SpikeTrap", (spike, obj) =>
        {
            spike.Origin          = obj.EnumProp ("origin",          spike.Origin);
            spike.SpikeLength     = obj.FloatProp("spikeLength",     spike.SpikeLength);
            spike.PhaseOffset     = obj.FloatProp("phaseOffset",     spike.PhaseOffset);
            spike.ExtendDuration  = obj.FloatProp("extendDuration",  spike.ExtendDuration);
            spike.HoldDuration    = obj.FloatProp("holdDuration",    spike.HoldDuration);
            spike.RetractDuration = obj.FloatProp("retractDuration", spike.RetractDuration);
            spike.PauseDuration   = obj.FloatProp("pauseDuration",   spike.PauseDuration);
            if (obj.Properties.ContainsKey("spriteName"))
                spike.SpriteTextureName = obj.StringProp("spriteName");
            spike.Player = _player;
        });
    }

    private void RegisterEnemy()
    {
        _loader.Register<Enemy>("Enemy", (enemy, obj) =>
        {
            enemy.PatrolRadius   = obj.FloatProp("patrolRadius",   enemy.PatrolRadius);
            enemy.DetectionRange = obj.FloatProp("detectionRange", enemy.DetectionRange);
            enemy.AttackRange    = obj.FloatProp("attackRange",    enemy.AttackRange);
            enemy.LeashRange     = obj.FloatProp("leashRange",     enemy.LeashRange);
            enemy.PatrolSpeed    = obj.FloatProp("patrolSpeed",    enemy.PatrolSpeed);
            enemy.ChaseSpeed     = obj.FloatProp("chaseSpeed",     enemy.ChaseSpeed);
            enemy.ReturnSpeed    = obj.FloatProp("returnSpeed",    enemy.ReturnSpeed);
            enemy.SetPlayer(_player);
        });
    }

    private void RegisterCoin()
    {
        _loader.Register<Coin>("Coin", (coin, obj) =>
        {
            coin.Value = obj.IntProp("value", coin.Value);
            coin.SetPlayer(_player);
        });
    }

    private void RegisterGoalFlag()
    {
        _loader.Register<GoalFlag>("GoalFlag", (flag, obj) =>
        {
            flag.Player = _player;
        });
    }

    private void RegisterDoubleJumpPowerUp()
    {
        _loader.Register<DoubleJumpPowerUp>("DoubleJumpPowerUp", (pu, obj) =>
        {
            pu.SetPlayer(_player);
        });
    }

    private void RegisterSpeedBoostPowerUp()
    {
        _loader.Register<SpeedBoostPowerUp>("SpeedBoostPowerUp", (pu, obj) =>
        {
            pu.SetPlayer(_player);
        });
    }

    private void RegisterSlowTimePowerUp()
    {
        _loader.Register<SlowTimePowerUp>("SlowTimePowerUp", (pu, obj) =>
        {
            pu.SetPlayer(_player);
        });
    }
}
