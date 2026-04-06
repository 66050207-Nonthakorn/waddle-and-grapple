using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System;
using System.Collections.Generic;
using GamePlayer = WaddleAndGrapple.Game.Player;
using GameEnemy  = WaddleAndGrapple.Game.Enemy;

namespace WaddleAndGrapple.Game.Example;

/// <summary>
/// Level 1 — Redesigned
///
///  Section 0 (0–1200):    Safe Zone — เดิน กระโดด Ledge Grab ไม่มีอันตราย
///  Section 1 (1200–2400): First Enemy — ช้างตัวแรก + platform สูงสามชั้น + DoubleJump
///  Section 2 (2400–3600): IcePickaxe Test — หลุม 640px บังคับใช้ Grapple ข้าม
///  Section 3 (3600–4800): Traps + Goal — หนาม + เลื่อยเล็ก + SlowTime ก่อนถึงธง
///
/// กระโดดสูงสุด: ~126px (JumpForce 550 / Gravity 1200)
/// ยืนบนพื้น (floor y=450): player center y=420, feet y=450
///
/// Platform reachability (feet start y=450, peak feet y=324):
///   floor → a1/b1/d1 (y=370, diff=80px) ✓
///   a1/b1 → a2/b2    (y=295, diff=75px) ✓
///   b2    → b3        (y=235, diff=60px) ✓
///   floor → d2        (y=345, diff=105px) ✓  ← ต้องกระโดดเต็มที่
///   d2    → d3        (y=260, diff=85px) ✓
///   c1/c2 ต้องใช้ pickaxe เพราะสูงกว่า peak (y < 324)
/// </summary>
class Level1 : BaseLevel
{
    GamePlayer player;
    GameObject cameraObject;

    static readonly Color ColFloorA   = new(50,  70, 110);
    static readonly Color ColFloorB   = new(35,  50,  85);
    static readonly Color ColPlatform = new(70, 110,  70);
    static readonly Color ColLowCeil  = new(100, 75,  55);  // low ceiling tunnel
    static readonly Color ColMarker   = new(60, 180, 120);
    static readonly Color ColCheckpt  = new(80,  80, 200);

    public override void Setup()
    {
        LevelIndex = 1;
        SetTotalFish(38);

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
        var startSpawn = new Vector2(80, 390);
        player.Position = startSpawn;
        player.SetSpawnPoint(startSpawn);
        RegisterPlayerForProgression(player);

        // ── Enemy — Section 1 เท่านั้น ────────────────────────────────────────
        // วางที่ x=1620 patrol ±150px (x=1470–1770)
        // b1 จบที่ x=1500 → player ขึ้น b1 ได้ก่อนเจอช้าง
        var enemy1 = base.AddGameObject<GameEnemy>("enemy_1");
        enemy1.Position     = new Vector2(1620, 420);
        enemy1.PatrolRadius = 150f;
        enemy1.SceneKey     = "enemy_1";
        enemy1.SetPlayer(player);
        player.SetEnemies([ enemy1 ]);

        // ══════════════════════════════════════════════════════════════════════
        // SOLIDS
        // ══════════════════════════════════════════════════════════════════════
        var solids = new List<Microsoft.Xna.Framework.Rectangle>
        {
            // ── พื้นหลัก (ช่องว่าง Grapple Zone: x=2680–3320) ─────────────────
            new(   0, 450, 2680, 150),   // Section 0 + 1 + ต้น 2 (x=0–2679)
            new(3320, 450, 1480, 150),   // ปลาย 2 + Section 3   (x=3320–4799)

            // ── Section 0 ─────────────────────────────────────────────────────
            // low_ceil: bottom y=405 → blocks standing (top=390) แต่ให้ก้มผ่าน (top=420)
            new( 350, 385, 130, 20),     // low_ceil (x=350–479, bottom y=405) ← CROUCH HERE
            new( 530, 370, 260, 20),     // plat_a1 (x=530–789)  ← หลัง tunnel
            new( 830, 295, 250, 20),     // plat_a2 (x=830–1079) ← Ledge Grab / SpeedBoost

            // ── Section 1 ─────────────────────────────────────────────────────
            new(1240, 370, 260, 20),     // plat_b1 (x=1240–1499) ← escape route จากช้าง
            new(1700, 295, 250, 20),     // plat_b2 (x=1700–1949)
            new(2050, 235, 250, 20),     // plat_b3 (x=2050–2299) ← DoubleJump reward

            // ── Section 2 — Grapple Platforms ────────────────────────────────
            // y=305, y=265 < 324 (max jump peak) → ต้องใช้ pickaxe ข้าม
            new(2800, 305, 200, 20),     // plat_c1 (x=2800–2999) ← hook target 1
            new(3080, 265, 180, 20),     // plat_c2 (x=3080–3259) ← hook target 2

            // ── Section 3 ─────────────────────────────────────────────────────
            new(3700, 370, 280, 20),     // plat_d1 (x=3700–3979) ← SlowTime
            new(4300, 345, 230, 20),     // plat_d2 (x=4300–4529) ← เหนือ traps
            new(4580, 260, 180, 20),     // plat_d3 (x=4580–4759) ← Goal
        };
        player.SetSolids(solids);
        enemy1.SetSolids(solids);

        // ══════════════════════════════════════════════════════════════════════
        // VISUALS — floor tiles 150px
        // ══════════════════════════════════════════════════════════════════════

        // พื้นก่อนหลุม (x=0–2699, 18 blocks)
        for (int i = 0; i < 18; i++)
            AddBlock($"fl_{i}", i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // พื้นหลังหลุม (x=3300–4799, 10 blocks)
        for (int i = 0; i < 10; i++)
            AddBlock($"fl2_{i}", 3300 + i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // Platforms
        AddBlock("low_ceil", 350, 385, 130, 20, ColLowCeil);
        AddBlock("plat_a1",  530, 370, 260, 20, ColPlatform);
        AddBlock("plat_a2",  830, 295, 250, 20, ColPlatform);
        AddBlock("plat_b1", 1240, 370, 260, 20, ColPlatform);
        AddBlock("plat_b2", 1700, 295, 250, 20, ColPlatform);
        AddBlock("plat_b3", 2050, 235, 250, 20, ColPlatform);
        AddBlock("plat_c1", 2800, 305, 200, 20, ColPlatform);
        AddBlock("plat_c2", 3080, 265, 180, 20, ColPlatform);
        AddBlock("plat_d1", 3700, 370, 280, 20, ColPlatform);
        AddBlock("plat_d2", 4300, 345, 230, 20, ColPlatform);
        AddBlock("plat_d3", 4580, 260, 180, 20, ColPlatform);

        // ══════════════════════════════════════════════════════════════════════
        // CHECKPOINTS / SECTIONS
        // ══════════════════════════════════════════════════════════════════════
        CheckpointManager.Instance.Reset();
        CheckpointManager.Instance.RegisterSections(new[]
        {
            new Section { Id=0, LeftBound=    0, RightBound= 1200,
                LeftSpawnPoint  = startSpawn,
                RightSpawnPoint = new Vector2(1160, 390) },
            new Section { Id=1, LeftBound= 1200, RightBound= 2400,
                LeftSpawnPoint  = new Vector2(1230, 390),
                RightSpawnPoint = new Vector2(2360, 390) },
            new Section { Id=2, LeftBound= 2400, RightBound= 3600,
                LeftSpawnPoint  = new Vector2(2430, 390),
                RightSpawnPoint = new Vector2(3560, 390) },
            new Section { Id=3, LeftBound= 3600, RightBound= 4800,
                LeftSpawnPoint  = new Vector2(3630, 390),
                RightSpawnPoint = new Vector2(4760, 390) },
        });

        AddBlock("spawn_marker", (int)startSpawn.X, (int)startSpawn.Y - 40, 8, 50, ColMarker);
        foreach (var (nm, x) in new[] { ("cp1",1200), ("cp2",2400), ("cp3",3600) })
            AddBlock(nm, x, 375, 10, 75, ColCheckpt);

        camera.FollowTarget = player;

        // ══════════════════════════════════════════════════════════════════════
        // TRAPS — Section 3 เท่านั้น (สอน trap หลัง player มี SlowTime แล้ว)
        // ══════════════════════════════════════════════════════════════════════

        // หนามพื้น 3 อัน phase ต่างกัน — player เดินผ่านได้ด้วย SlowTime
        float[] spikeXs     = { 4010f, 4055f, 4100f };
        float[] spikePhases = { 0f, 0.6f, 1.2f };
        for (int i = 0; i < spikeXs.Length; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_d{i}");
            s.Position          = new Vector2(spikeXs[i], 450);
            s.RotationAngle     = 0f;
            s.SpikeTiles        = 2;
            s.PhaseOffset       = spikePhases[i];
            s.SpriteTextureName = "Traps/Spike/Spike";
            s.SpriteTint        = Color.White;
            s.Player            = player;
        }

        // เลื่อยเล็กวิ่งช้าๆ บนพื้น ก่อนถึง d2
        // ช้า (65 px/s) เพื่อให้ SlowTime เห็นผลชัดเจน
        var saw = base.AddGameObject<SawTrap>("saw_d");
        saw.Position          = new Vector2(4200, 450);
        saw.MoveRange         = 110f;
        saw.MoveSpeed         = 65f;
        saw.MoveHorizontal    = true;
        saw.Size              = SawSize.Small;
        saw.SpriteTextureName = "Traps/Saw/SmallSaw";
        saw.SpriteTint        = Color.White;
        saw.Placement         = SawPlacement.FloorMounted;
        saw.Player            = player;

        // ══════════════════════════════════════════════════════════════════════
        // ITEMS & COINS
        // ══════════════════════════════════════════════════════════════════════

        // ── Section 0: อุโมงค์ก้ม → a1 → a2 (SpeedBoost) ──────────────────────
        // เหรียญ trail พา player ตรงเข้าอุโมงค์ (x=350–479)
        // เหรียญในอุโมงค์: ดึงดูดให้ก้ม/slide เข้าไปเก็บ
        AddFish("cA0",  new[] { 110f, 165f, 240f, 315f }, y: 415f); // floor trail → อุโมงค์
        AddFish("cAtun",new[] { 415f               },     y: 430f); // IN tunnel (reward)
        AddFish("cA1",  new[] { 600f, 665f, 730f   },     y: 333f); // บน a1 (x=530-789, y=370-37)
        AddFish("cA2",  new[] { 880f, 950f         },     y: 258f); // บน a2 (x=830-1079, y=295-37)
        AddItem<SpeedBoostPowerUp>("speed_a", 1010f, 258f);           // ปลาย a2
        AddFish("cA3",  new[] { 1065f, 1120f       },     y: 415f); // floor exit

        // ── Section 1: ช้างตัวแรก ───────────────────────────────────────────
        // b1 อยู่ก่อนช้าง (x=1500 < enemy x=1620) → ขึ้นหลบได้ทันที
        // b3 สูงสุด: DoubleJump เป็น reward สำหรับคนที่ขึ้นไปถึง
        AddFish("cB0", new[] { 1225f, 1265f },          y: 415f); // floor entry
        AddFish("cB1", new[] { 1310f, 1375f, 1440f },   y: 333f); // บน b1 (y=370-37)
        AddFish("cB2", new[] { 1760f, 1830f },          y: 258f); // บน b2 (y=295-37)
        AddItem<DoubleJumpPowerUp>("djump_b", 2175f, 198f);         // บน b3 (y=235-37)
        AddFish("cB3", new[] { 2330f, 2380f },          y: 415f); // floor exit

        // ── Section 2: Grapple ──────────────────────────────────────────────
        // cArc: เหรียญลอยในอากาศรูปโค้งนำไปหา c1
        //   - x=2660 อยู่บนพื้น ก่อนหลุม (เก็บขณะเดิน)
        //   - x=2720, 2790 ลอยเหนือหลุม — เก็บได้ขณะ swing เป็น visual hint
        // c1/c2: เก็บได้ขณะลงจอดบน platform
        AddFish("cC0",  new[] { 2450f, 2530f, 2610f }, y: 415f);  // floor approach
        AddFish("cArc", new[] { 2660f, 2720f, 2790f }, y: 400f);  // arc hint (midair)
        AddFish("cC1",  new[] { 2840f, 2910f },        y: 268f);  // บน c1 (y=305-37)
        AddFish("cC2",  new[] { 3110f, 3180f },        y: 228f);  // บน c2 (y=265-37)
        AddFish("cC3",  new[] { 3360f, 3420f },        y: 415f);  // floor landing

        // ── Section 3: Traps + Goal ─────────────────────────────────────────
        // SlowTime อยู่บน d1 ก่อนถึง trap → player ใช้ SlowTime ผ่านหนาม+เลื่อย
        // สองเส้นทาง:
        //   A) Floor path: ใช้ SlowTime เดินผ่าน → กระโดดขึ้น d2 จากพื้น
        //   B) Air path: มี DoubleJump → กระโดดจาก d1 ข้าม trap ไป d2 โดยตรง
        AddFish("cD0", new[] { 3640f, 3685f },     y: 415f);      // floor entry
        AddFish("cD1", new[] { 3750f, 3820f },     y: 333f);      // บน d1 (y=370-37)
        AddItem<SlowTimePowerUp>("slow_d", 3900f, 333f);            // ปลาย d1
        AddFish("cD2", new[] { 4340f, 4410f },     y: 308f);      // บน d2 (y=345-37)

        // ── Goal Flag ─────────────────────────────────────────────────────────
        // d3 top y=260 → goal y=260-37=223
        var goal = base.AddGameObject<GoalFlag>("goal");
        goal.Position   = new Vector2(4670, 223);  // บน d3
        goal.Player     = player;
        goal.OnComplete = CompleteLevel;

        base.Setup(); // สร้าง PausedPanel + TimerUI (ต้องเป็นบรรทัดสุดท้าย)
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

    protected override void CompleteLevel()
    {
        _isLevelCompleted = true;

        ProgressionManager.Instance.CompleteLevel(
            LevelIndex,
            TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()),
            player?.FishCount ?? 0,
            _totalFishInLevel,
            GetLatestCheckpoint());

        Console.WriteLine($"Level {LevelIndex} completed!");
        SceneManager.Instance.LoadScene("levelcomplete");
    }
}
