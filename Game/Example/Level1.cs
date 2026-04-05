using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GamePlayer = WaddleAndGrapple.Game.Player;
using GameEnemy  = WaddleAndGrapple.Game.Enemy;

namespace WaddleAndGrapple.Game.Example;

/// <summary>
/// Level 1 — 2400px wide, 4 zones
///
///  Zone A (0–600):    Tutorial — platforms 2 ชั้น, saw ช้า
///  Zone B (600–1200): Timing   — timed laser, floor spikes
///  Zone C (1200–1800): Grapple — ช่องว่าง 220px, hook wall, ceiling spikes
///  Zone D (1800–2400): Gauntlet — wall spikes, fast saw, always-on laser
/// </summary>
class Level1 : BaseLevel
{
    GamePlayer player;
    GoalFlag   goal;

    static readonly Color ColFloorA   = new(50,  70, 110);
    static readonly Color ColFloorB   = new(35,  50,  85);
    static readonly Color ColPlatform = new(70, 110,  70);
    static readonly Color ColWall     = new(90,  70,  50);
    static readonly Color ColMarker   = new(60, 180, 120);
    static readonly Color ColCheckpt  = new(80,  80, 200);

    public override void Setup()
    {
        LevelIndex = 1;
        SetTotalFish(15);   // 15 coins กระจายทั่วด่าน

        // ── Camera ────────────────────────────────────────────────────────────
        var cameraObject = base.AddGameObject<GameObject>("camera");
        var camera       = cameraObject.AddComponent<Camera2D>();
        camera.SetViewport(new Viewport(0, 0,
            ScreenManager.Instance.nativeWidth,
            ScreenManager.Instance.nativeHeight));
        camera.Zoom         = 1f;
        camera.SmoothFollow = false;
        base.Camera         = camera;

        // ── Parallax Background ───────────────────────────────────────────────
        var bgObj = base.AddGameObject<GameObject>("background");
        var bg    = bgObj.AddComponent<ParallaxBackground>();
        bg.AddLayer("background", scrollFactor: 0.3f, layerDepth: 0.00f);

        // ── Player ────────────────────────────────────────────────────────────
        player = base.AddGameObject<GamePlayer>("player");
        var startSpawn = new Vector2(80, 380);
        player.Position = startSpawn;
        player.SetSpawnPoint(startSpawn);

        // ── Enemy ─────────────────────────────────────────────────────────────
        var enemy = base.AddGameObject<GameEnemy>("enemy");
        enemy.Position = new Vector2(600, 460);
        enemy.SetPlayer(player);

        // ── Solids ────────────────────────────────────────────────────────────
        var solids = new List<Rectangle>
        {
            // พื้น (ช่องว่าง Zone C: x=1300–1520)
            new(   0, 450, 1300, 150),
            new(1520, 450,  880, 150),

            // Zone A
            new( 150, 370, 300, 20),   // plat_a1
            new( 440, 295, 260, 20),   // plat_a2

            // Zone B
            new( 640, 370, 300, 20),   // plat_b1
            new( 990, 295, 270, 20),   // plat_b2

            // Zone C
            new(1320, 258,  20, 197),  // hook_wall
            new(1540, 360, 300, 20),   // plat_c1
            new(1810, 280, 250, 20),   // plat_c2

            // Zone D
            new(1865, 360, 300, 20),   // plat_d1
            new(2195, 285, 265, 20),   // plat_d2
        };
        player.SetSolids(solids);
        enemy.SetSolids(solids);

        // ── Floor visuals ─────────────────────────────────────────────────────
        for (int i = 0; i < 9; i++)
            AddBlock($"fl_{i}", i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);
        for (int i = 0; i < 6; i++)
            AddBlock($"fl_{10+i}", 1520 + i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // ── Platform visuals ──────────────────────────────────────────────────
        AddBlock("plat_a1",   150, 370, 300, 20, ColPlatform);
        AddBlock("plat_a2",   440, 295, 260, 20, ColPlatform);
        AddBlock("plat_b1",   640, 370, 300, 20, ColPlatform);
        AddBlock("plat_b2",   990, 295, 270, 20, ColPlatform);
        AddBlock("hook_wall",1320, 258,  20, 197, ColWall);
        AddBlock("plat_c1",  1540, 360, 300, 20, ColPlatform);
        AddBlock("plat_c2",  1810, 280, 250, 20, ColPlatform);
        AddBlock("plat_d1",  1865, 360, 300, 20, ColPlatform);
        AddBlock("plat_d2",  2195, 285, 265, 20, ColPlatform);

        // ── Checkpoints ───────────────────────────────────────────────────────
        CheckpointManager.Instance.Reset();
        CheckpointManager.Instance.RegisterSections(new[]
        {
            new Section { Id=0, LeftBound=   0, RightBound= 600,
                LeftSpawnPoint  = startSpawn,
                RightSpawnPoint = new Vector2(560, 380) },
            new Section { Id=1, LeftBound= 600, RightBound=1200,
                LeftSpawnPoint  = new Vector2(620, 380),
                RightSpawnPoint = new Vector2(1170, 380) },
            new Section { Id=2, LeftBound=1200, RightBound=1800,
                LeftSpawnPoint  = new Vector2(1230, 380),
                RightSpawnPoint = new Vector2(1760, 380) },
            new Section { Id=3, LeftBound=1800, RightBound=2400,
                LeftSpawnPoint  = new Vector2(1830, 380),
                RightSpawnPoint = new Vector2(2360, 380) },
        });

        AddBlock("spawn_marker", (int)startSpawn.X, (int)startSpawn.Y - 40, 8, 50, ColMarker);
        foreach (var (nm, x) in new[] { ("cp1",600), ("cp2",1200), ("cp3",1800) })
            AddBlock(nm, x, 375, 10, 75, ColCheckpt);

        camera.FollowTarget = player;

        // ── Traps ─────────────────────────────────────────────────────────────

        // Zone A: saw ช้า
        var sawA = base.AddGameObject<SawTrap>("saw_a");
        sawA.Position          = new Vector2(310, 420);
        sawA.MoveRange         = 220f;
        sawA.MoveSpeed         = 70f;
        sawA.MoveHorizontal    = true;
        sawA.BladeSize         = 50f;
        sawA.SpriteTextureName = "Traps/Saw/LargeSaw";
        sawA.SpriteTint        = Color.White;
        sawA.Placement         = SawPlacement.FloorMounted;
        sawA.Player            = player;

        // Zone B: timed laser
        var laserB = base.AddGameObject<LaserTrap>("laser_b");
        laserB.Position     = new Vector2(755, 426);
        laserB.BeamLength   = 220f;
        laserB.IsHorizontal = true;
        laserB.Style        = LaserStyle.WallMounted;
        laserB.AlwaysOn     = false;
        laserB.OnDuration   = 1.5f;
        laserB.OffDuration  = 2.0f;
        laserB.Player       = player;

        // Zone B: floor spikes
        foreach (var (nx, ph) in new[] { (895, 0f), (940, 0.7f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_b_{nx}");
            s.Position          = new Vector2(nx, 450);
            s.Origin            = SpikeOrigin.Floor;
            s.SpikeLength       = 45f;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // Zone C: pit spikes
        foreach (var (nx, ph) in new[] { (1330,0f),(1375,0.5f),(1420,1.0f),(1465,1.5f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_c_{nx}");
            s.Position          = new Vector2(nx, 600);
            s.Origin            = SpikeOrigin.Floor;
            s.SpikeLength       = 50f;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // Zone C: ceiling spikes
        foreach (var (nx, ph) in new[] { (1840, 0f), (1885, 0.8f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_cc_{nx}");
            s.Position          = new Vector2(nx, 280);
            s.Origin            = SpikeOrigin.Ceiling;
            s.SpikeLength       = 45f;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // Zone D: wall spikes
        var swL = base.AddGameObject<SpikeTrap>("spk_wL");
        swL.Position          = new Vector2(1948, 428);
        swL.Origin            = SpikeOrigin.LeftWall;
        swL.SpikeLength       = 55f;
        swL.PhaseOffset       = 0f;
        swL.SpriteTextureName = "Traps/Spike/Spike";
        swL.SpriteTint        = Color.White;
        swL.Player            = player;

        var swR = base.AddGameObject<SpikeTrap>("spk_wR");
        swR.Position          = new Vector2(1995, 428);
        swR.Origin            = SpikeOrigin.RightWall;
        swR.SpikeLength       = 55f;
        swR.PhaseOffset       = 0.9f;
        swR.SpriteTextureName = "Traps/Spike/Spike";
        swR.SpriteTint        = Color.White;
        swR.Player            = player;

        // Zone D: fast saw
        var sawD = base.AddGameObject<SawTrap>("saw_d");
        sawD.Position          = new Vector2(2075, 420);
        sawD.MoveRange         = 160f;
        sawD.MoveSpeed         = 165f;
        sawD.MoveHorizontal    = true;
        sawD.BladeSize         = 50f;
        sawD.SpriteTextureName = "Traps/Saw/LargeSaw";
        sawD.SpriteTint        = Color.White;
        sawD.Placement         = SawPlacement.FloorMounted;
        sawD.Player            = player;

        // Zone D: always-on laser
        var laserD = base.AddGameObject<LaserTrap>("laser_d");
        laserD.Position     = new Vector2(2165, 426);
        laserD.BeamLength   = 110f;
        laserD.IsHorizontal = true;
        laserD.Style        = LaserStyle.WallMounted;
        laserD.AlwaysOn     = true;
        laserD.Player       = player;

        // Zone D: floor spikes
        foreach (var (nx, ph) in new[] { (2275, 0f), (2315, 0.5f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_d_{nx}");
            s.Position          = new Vector2(nx, 450);
            s.Origin            = SpikeOrigin.Floor;
            s.SpikeLength       = 45f;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // ── Coins & Power-Ups ─────────────────────────────────────────────────
        AddCoins("cA",  new[] { 90f, 130f, 170f }, y: 415f);          // Zone A: 3
        AddItem<SpeedBoostPowerUp>("sboost", 555f, 258f);

        AddCoins("cBf", new[] { 665f, 705f }, y: 415f);               // Zone B: 2+3
        AddCoins("cBp", new[] { 1015f, 1065f, 1110f }, y: 258f);
        AddItem<DoubleJumpPowerUp>("djump", 790f, 333f);

        AddCoins("cCf", new[] { 1230f, 1270f }, y: 415f);             // Zone C: 2+3
        AddCoins("cCp", new[] { 1570f, 1620f, 1670f }, y: 323f);
        AddItem<SlowTimePowerUp>("slow", 1935f, 243f);

        AddCoins("cDf", new[] { 1890f, 1930f }, y: 415f);             // Zone D: 2

        // ── Goal Flag ─────────────────────────────────────────────────────────
        goal          = base.AddGameObject<GoalFlag>("goal");
        goal.Position = new Vector2(2340, 285);
        goal.Player   = player;

        // ── Progression ───────────────────────────────────────────────────────
        RegisterPlayerForProgression(player);

        // call base setup last (creates pause panel, timer UI on top)
        base.Setup();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        // trigger level complete once goal animation finishes
        if (!_isLevelCompleted && player != null && player.IsGoalAnimationComplete)
            CompleteLevel();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddBlock(string name, int x, int y, int w, int h, Color color)
    {
        var go      = base.AddGameObject<GameObject>(name);
        go.Position = new Vector2(x, y);
        go.Scale    = new Vector2(w, h);
        var sr      = go.AddComponent<SpriteRenderer>();
        sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        sr.Tint       = color;
        sr.LayerDepth = 0.1f;
    }

    private void AddCoins(string prefix, float[] xs, float y)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            var c = base.AddGameObject<Coin>($"{prefix}_{i}");
            c.Position = new Vector2(xs[i], y);
            c.SetPlayer(player);
        }
    }

    private void AddItem<T>(string name, float x, float y) where T : PowerUp, new()
    {
        var item = base.AddGameObject<T>(name);
        item.Position = new Vector2(x, y);
        item.SetPlayer(player);
    }
}
