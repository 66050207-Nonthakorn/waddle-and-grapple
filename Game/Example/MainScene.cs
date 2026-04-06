using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using GamePlayer = WaddleAndGrapple.Game.Player;
using GameEnemy = WaddleAndGrapple.Game.Enemy;

namespace WaddleAndGrapple.Game.Example;

/// <summary>
/// Test Map v2 — 2400px wide, 4 zones, checkpoint ทุก 600px
///
///  Zone A (0–600):    Tutorial — platform ใหญ่ chain 2 ชั้น, saw ช้า
///                     Items: 3 coins บนพื้น, SpeedBoost บน plat_a2
///
///  Zone B (600–1200): Timing  — timed laser ขวางทาง, floor spike
///                     Items: 2 coins บนพื้น, 3 coins บน plat_b2, DoubleJump บน plat_b1
///
///  Zone C (1200–1800): Grapple — ช่องว่าง 220px, hook wall, ceiling spike
///                     Items: 2 coins ก่อนช่อง, 3 coins บน plat_c1, SlowTime บน plat_c2
///
///  Zone D (1800–2400): Gauntlet — wall spike pair, fast saw, always-on laser
///                     Items: 2 coins บนพื้นก่อน saw
///
/// กระโดดสูงสุด: ~126px (JumpForce 550 / Gravity 1200)
/// ยืนบนพื้น (floor y=450): player center y=420
/// </summary>
class MainScene : Scene
{
    GamePlayer player;
    GameObject cameraObject;
    GameEnemy enemy;

    static readonly Color ColFloorA   = new(50,  70, 110);
    static readonly Color ColFloorB   = new(35,  50,  85);
    static readonly Color ColPlatform = new(70, 110,  70);
    static readonly Color ColWall     = new(90,  70,  50);
    static readonly Color ColMarker   = new(60, 180, 120);
    static readonly Color ColCheckpt  = new(80,  80, 200);

    public override void Setup()
    {
        // ── Camera ────────────────────────────────────────────────────────────
        cameraObject = base.AddGameObject<GameObject>("camera");
        var camera   = cameraObject.AddComponent<Camera2D>();
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

        // ── Enemy ────────────────────────────────────────────────────────────
        enemy = base.AddGameObject<GameEnemy>("enemy");
        enemy.Position = new Vector2(600, 460);
        enemy.SetPlayer(player);

        // ══════════════════════════════════════════════════════════════════════
        // SOLIDS
        // ══════════════════════════════════════════════════════════════════════
        var solids = new List<Microsoft.Xna.Framework.Rectangle>
        {
            // ── พื้น (ช่องว่าง Zone C: x=1300–1520) ─────────────────────────
            new(   0, 450, 1300, 150),   // Zone A + B + ต้น C
            new(1520, 450,  880, 150),   // ปลาย C + Zone D

            // ── Zone A ────────────────────────────────────────────────────────
            // plat_a1: platform ต่ำ กระโดดจากพื้นง่าย (rise 50px)
            new( 150, 370, 300, 20),
            // plat_a2: สูงขึ้นอีกชั้น chain จาก a1 (rise 70px จาก center=340)
            new( 440, 295, 260, 20),

            // ── Zone B ────────────────────────────────────────────────────────
            new( 640, 370, 300, 20),     // plat_b1
            new( 990, 295, 270, 20),     // plat_b2 (chain จาก b1, rise 70px)

            // ── Zone C ────────────────────────────────────────────────────────
            new(1320, 258,  20, 197),    // hook_wall — grapple ข้ามช่องว่าง
            new(1540, 360, 300, 20),     // plat_c1 — ลงจอดหลังข้ามช่อง
            new(1810, 280, 250, 20),     // plat_c2 — chain จาก c1 (rise 80px)

            // ── Zone D ────────────────────────────────────────────────────────
            new(1865, 360, 300, 20),     // plat_d1
            new(2195, 285, 265, 20),     // plat_d2 — chain จาก d1 (rise 75px)
        };
        player.SetSolids(solids);
        enemy.SetSolids(solids);

        // ══════════════════════════════════════════════════════════════════════
        // VISUALS — พื้น (tile 150px)
        // ══════════════════════════════════════════════════════════════════════
        for (int i = 0; i < 9; i++)   // x = 0–1299
            AddBlock($"fl_{i}", i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);
        for (int i = 0; i < 6; i++)   // x = 1520–2399
            AddBlock($"fl_{10+i}", 1520 + i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // ── Platform visuals ─────────────────────────────────────────────────
        AddBlock("plat_a1",   150, 370, 300, 20, ColPlatform);
        AddBlock("plat_a2",   440, 295, 260, 20, ColPlatform);
        AddBlock("plat_b1",   640, 370, 300, 20, ColPlatform);
        AddBlock("plat_b2",   990, 295, 270, 20, ColPlatform);
        AddBlock("hook_wall",1320, 258,  20, 197, ColWall);
        AddBlock("plat_c1",  1540, 360, 300, 20, ColPlatform);
        AddBlock("plat_c2",  1810, 280, 250, 20, ColPlatform);
        AddBlock("plat_d1",  1865, 360, 300, 20, ColPlatform);
        AddBlock("plat_d2",  2195, 285, 265, 20, ColPlatform);

        // ══════════════════════════════════════════════════════════════════════
        // CHECKPOINTS / SECTIONS
        // ══════════════════════════════════════════════════════════════════════
        CheckpointManager.Instance.Reset();
        CheckpointManager.Instance.RegisterSections(new[]
        {
            new Section { Id=0, LeftBound=   0, RightBound= 600,
                LeftSpawnPoint = startSpawn,
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

        // ══════════════════════════════════════════════════════════════════════
        // TRAPS
        // ══════════════════════════════════════════════════════════════════════

        // ── Zone A: saw ช้า บนพื้น ────────────────────────────────────────────
        var sawA = base.AddGameObject<SawTrap>("saw_a");
        sawA.Position          = new Vector2(310, 420);
        sawA.MoveRange         = 220f;
        sawA.MoveSpeed         = 70f;
        sawA.MoveHorizontal    = true;
        sawA.Size              = SawSize.Large;
        sawA.SpriteTextureName = "Traps/Saw/LargeSaw";
        sawA.SpriteTint        = Color.White;
        sawA.Placement         = SawPlacement.FloorMounted;
        sawA.Player            = player;

        // ── Zone B: timed laser ────────────────────────────────────────────────
        var laserB = base.AddGameObject<LaserTrap>("laser_b");
        laserB.Position           = new Vector2(755, 426);
        laserB.BeamLength         = 220f;
        laserB.IsHorizontal       = true;
        laserB.Style              = LaserStyle.WallMounted;
        laserB.AlwaysOn           = false;
        laserB.OnDuration         = 1.5f;
        laserB.OffDuration        = 2.0f;
        laserB.Player             = player;

        // ── Zone B: floor spike สองอัน stagger ────────────────────────────────
        foreach (var (nx, ph) in new[] { (895, 0f), (940, 0.7f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_b_{nx}");
            s.Position          = new Vector2(nx, 450);
            s.RotationAngle     = 0f;   // floor (spikes point up)
            s.SpikeTiles        = 3;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // ── Zone C: pit spike ในช่องว่าง ─────────────────────────────────────
        foreach (var (nx, ph) in new[] { (1330,0f),(1375,0.5f),(1420,1.0f),(1465,1.5f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_c_{nx}");
            s.Position          = new Vector2(nx, 600);
            s.RotationAngle     = 0f;
            s.SpikeTiles        = 3;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // ── Zone C: ceiling spike ใต้ plat_c2 ────────────────────────────────
        foreach (var (nx, ph) in new[] { (1840, 0f), (1885, 0.8f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_cc_{nx}");
            s.Position          = new Vector2(nx, 280);
            s.RotationAngle     = MathF.PI;   // ceiling (spikes point down)
            s.SpikeTiles        = 3;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // ── Zone D: wall spike pair ───────────────────────────────────────────
        var swL = base.AddGameObject<SpikeTrap>("spk_wL");
        swL.Position          = new Vector2(1948, 428);
        swL.RotationAngle     = MathF.PI / 2f;   // left wall (spikes point right)
        swL.SpikeTiles        = 3;
        swL.PhaseOffset       = 0f;
        swL.SpriteTextureName = "Traps/Spike/Spike";
        swL.SpriteTint        = Color.White;
        swL.Player            = player;

        var swR = base.AddGameObject<SpikeTrap>("spk_wR");
        swR.Position          = new Vector2(1995, 428);
        swR.RotationAngle     = -MathF.PI / 2f;  // right wall (spikes point left)
        swR.SpikeTiles        = 3;
        swR.PhaseOffset       = 0.9f;
        swR.SpriteTextureName = "Traps/Spike/Spike";
        swR.SpriteTint        = Color.White;
        swR.Player            = player;

        // ── Zone D: fast saw ──────────────────────────────────────────────────
        var sawD = base.AddGameObject<SawTrap>("saw_d");
        sawD.Position          = new Vector2(2075, 420);
        sawD.MoveRange         = 160f;
        sawD.MoveSpeed         = 165f;
        sawD.MoveHorizontal    = true;
        sawD.Size              = SawSize.Large;
        sawD.SpriteTextureName = "Traps/Saw/LargeSaw";
        sawD.SpriteTint        = Color.White;
        sawD.Placement         = SawPlacement.FloorMounted;
        sawD.Player            = player;

        // ── Zone D: always-on laser ───────────────────────────────────────────
        var laserD = base.AddGameObject<LaserTrap>("laser_d");
        laserD.Position     = new Vector2(2165, 426);
        laserD.BeamLength   = 110f;
        laserD.IsHorizontal = true;
        laserD.Style        = LaserStyle.WallMounted;
        laserD.AlwaysOn     = true;
        laserD.Player       = player;

        // ── Zone D: floor spike ก่อน plat_d2 ────────────────────────────────
        foreach (var (nx, ph) in new[] { (2275, 0f), (2315, 0.5f) })
        {
            var s = base.AddGameObject<SpikeTrap>($"spk_d_{nx}");
            s.Position          = new Vector2(nx, 450);
            s.RotationAngle     = 0f;
            s.SpikeTiles        = 3;
            s.PhaseOffset       = ph;
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // ══════════════════════════════════════════════════════════════════════
        // ITEMS — Coins & Power-Ups
        //
        // Fish  (ปลา)     : เพิ่ม FishCount — secondary score
        // SpeedBoost (M)  : MoveSpeed ×1.5 เป็น 10 วิ
        // DoubleJump (M)  : กระโดดได้อีกครั้งในอากาศ (one-time)
        // SlowTime   (M)  : ชะลอ world ทั้งหมด 8 วิ (timer ยังเดินปกติ)
        // ══════════════════════════════════════════════════════════════════════

        // Zone A — 3 coins บนพื้น (ก่อน saw), SpeedBoost บน plat_a2
        AddFish("cA", new[] { 90f, 130f, 170f }, y: 415f);
        AddItem<SpeedBoostPowerUp>("sboost", 555f, 258f);   // plat_a2 center

        // Zone B — 2 coins บนพื้นก่อน laser, 3 coins บน plat_b2, DoubleJump บน plat_b1
        AddFish("cBf", new[] { 665f, 705f }, y: 415f);
        AddFish("cBp", new[] { 1015f, 1065f, 1110f }, y: 258f);
        AddItem<DoubleJumpPowerUp>("djump", 790f, 333f);    // plat_b1 center

        // Zone C — 2 coins ก่อนช่อง, 3 coins บน plat_c1, SlowTime บน plat_c2
        AddFish("cCf", new[] { 1230f, 1270f }, y: 415f);
        AddFish("cCp", new[] { 1570f, 1620f, 1670f }, y: 323f);
        AddItem<SlowTimePowerUp>("slow", 1935f, 243f);      // plat_c2 center

        // Zone D — 2 coins บนพื้นก่อน wall spike
        AddFish("cDf", new[] { 1890f, 1930f }, y: 415f);

        // ── Goal Flag ── ปลายด่าน ──────────────────────────────────────────
        var goal = base.AddGameObject<GoalFlag>("goal");
        goal.Position = new Vector2(2340, 285);  // บน plat_d2 (x=2195,w=265,y=285)
        goal.Player   = player;
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

    private void AddFish(string prefix, float[] xs, float y)
    {
        for (int i = 0; i < xs.Length; i++)
        {
            var c = base.AddGameObject<Fish>($"{prefix}_{i}");
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
