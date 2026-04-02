using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System.Collections.Generic;
using GamePlayer = WaddleAndGrapple.Game.Player;

namespace WaddleAndGrapple.Game.Example;

/// <summary>
/// Test Map — 2400px, 4 zones, checkpoint ทุก 600px
///
///  Zone A (0–600):    Tutorial — กระโดด + saw ช้า
///  Zone B (600–1200): Timing   — platform chain + laser + floor spike
///  Zone C (1200–1800): Grapple  — ช่องว่าง 180px + hook wall + spike
///  Zone D (1800–2400): Gauntlet — saw เร็ว + wall spike + laser + spike
/// </summary>
class MainScene : Scene
{
    GamePlayer player;
    GameObject cameraObject;

    // ── Palette ──────────────────────────────────────────────────────────────
    static readonly Color ColFloorA    = new(50,  70, 110);
    static readonly Color ColFloorB    = new(35,  50,  85);
    static readonly Color ColPlatform  = new(70, 110,  70);
    static readonly Color ColWall      = new(90,  70,  50);
    static readonly Color ColMarker    = new(60, 180, 120);
    static readonly Color ColCheckpt   = new(80,  80, 200);

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
        // layer เดียวตอนนี้ — เพิ่มได้โดยเรียก bg.AddLayer(...) เพิ่ม
        bg.AddLayer("background", scrollFactor: 0.3f, layerDepth: 0.00f);

        // ── Player ────────────────────────────────────────────────────────────
        player = base.AddGameObject<GamePlayer>("player");
        var startSpawn = new Vector2(80, 380);
        player.Position = startSpawn;
        player.SetSpawnPoint(startSpawn);

        // ══════════════════════════════════════════════════════════════════════
        // SOLIDS
        // ══════════════════════════════════════════════════════════════════════
        var solids = new List<Microsoft.Xna.Framework.Rectangle>
        {
            // ── พื้น (มีช่องว่าง Zone C: x=1320–1500) ──────────────────────
            new(   0, 450, 1320, 150),   // พื้น Zone A–B + ต้น C
            new(1500, 450,  900, 150),   // พื้น Zone C ปลาย – Zone D

            // ── Zone A: platforms ──────────────────────────────────────────
            new( 200, 340, 160, 20),     // platform ต่ำ  (กระโดดขึ้นได้)
            new( 420, 260, 140, 20),     // platform สูง  (chain jump)

            // ── Zone B: platforms ──────────────────────────────────────────
            new( 640, 360, 160, 20),     // B-1 (เริ่ม zone)
            new( 860, 300, 150, 20),     // B-2 (กลาง)
            new(1060, 370, 140, 20),     // B-3 (ลงมา)

            // ── Zone C: hook wall + platforms ─────────────────────────────
            new(1340, 280,  20, 170),    // กำแพงสำหรับ grapple ข้ามช่อง
            new(1520, 350, 170, 20),     // C-1 (หลังช่อง)
            new(1680, 270, 140, 20),     // C-2 (สูงขึ้น)

            // ── Zone D: platforms ──────────────────────────────────────────
            new(1840, 340, 150, 20),     // D-1
            new(2020, 270, 140, 20),     // D-2
            new(2220, 350, 160, 20),     // D-3 (เกือบจบ)
        };
        player.SetSolids(solids);

        // ══════════════════════════════════════════════════════════════════════
        // VISUALS — พื้น
        // ══════════════════════════════════════════════════════════════════════
        // Zone A–B + ต้น C (tile 0–8, ข้ามช่วง gap)
        for (int i = 0; i < 9; i++)
            AddBlock($"floor_{i}", i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // Zone C ปลาย–D (tile 10–15, เริ่ม x=1500)
        for (int i = 0; i < 6; i++)
            AddBlock($"floor_{10+i}", 1500 + i * 150, 450, 150, 150,
                     i % 2 == 0 ? ColFloorA : ColFloorB);

        // ── Platforms ──────────────────────────────────────────────────────
        AddBlock("plat_a1",   200, 340, 160, 20, ColPlatform);
        AddBlock("plat_a2",   420, 260, 140, 20, ColPlatform);
        AddBlock("plat_b1",   640, 360, 160, 20, ColPlatform);
        AddBlock("plat_b2",   860, 300, 150, 20, ColPlatform);
        AddBlock("plat_b3",  1060, 370, 140, 20, ColPlatform);
        AddBlock("hook_wall",1340, 280,  20, 170, ColWall);
        AddBlock("plat_c1",  1520, 350, 170, 20, ColPlatform);
        AddBlock("plat_c2",  1680, 270, 140, 20, ColPlatform);
        AddBlock("plat_d1",  1840, 340, 150, 20, ColPlatform);
        AddBlock("plat_d2",  2020, 270, 140, 20, ColPlatform);
        AddBlock("plat_d3",  2220, 350, 160, 20, ColPlatform);

        // ══════════════════════════════════════════════════════════════════════
        // CHECKPOINTS / SECTIONS
        // ══════════════════════════════════════════════════════════════════════
        CheckpointManager.Instance.Reset();
        CheckpointManager.Instance.RegisterSections(new[]
        {
            new Section { Id=0, LeftBound=   0, RightBound= 600,
                LeftSpawnPoint=startSpawn, RightSpawnPoint=new Vector2(560,380) },
            new Section { Id=1, LeftBound= 600, RightBound=1200,
                LeftSpawnPoint=new Vector2(620,380), RightSpawnPoint=new Vector2(1160,380) },
            new Section { Id=2, LeftBound=1200, RightBound=1800,
                LeftSpawnPoint=new Vector2(1220,380), RightSpawnPoint=new Vector2(1760,380) },
            new Section { Id=3, LeftBound=1800, RightBound=2400,
                LeftSpawnPoint=new Vector2(1820,380), RightSpawnPoint=new Vector2(2360,380) },
        });

        // spawn marker
        AddBlock("spawn_marker", (int)startSpawn.X, (int)startSpawn.Y - 40, 8, 50, ColMarker);

        // checkpoint markers (เสาสีน้ำเงิน)
        foreach (var (name, x) in new[] {
            ("cp1", 600), ("cp2", 1200), ("cp3", 1800)
        })
            AddBlock(name, x, 370, 10, 80, ColCheckpt);

        camera.FollowTarget = player;

        // ══════════════════════════════════════════════════════════════════════
        // TRAPS
        // ══════════════════════════════════════════════════════════════════════

        // ── Zone A: saw ช้า บนพื้น ────────────────────────────────────────
        var sawA = base.AddGameObject<SawTrap>("saw_a");
        sawA.Position       = new Vector2(310, 420);
        sawA.MoveRange      = 160f;
        sawA.MoveSpeed      = 75f;
        sawA.MoveHorizontal = true;
        sawA.Player         = player;

        // ── Zone B: laser timed ขวางทางเดิน (ต้องรอจังหวะ) ──────────────
        var laserB = base.AddGameObject<LaserTrap>("laser_b");
        laserB.Position     = new Vector2(700, 406);
        laserB.BeamLength   = 220f;
        laserB.IsHorizontal = true;
        laserB.AlwaysOn     = false;
        laserB.OnDuration   = 1.5f;
        laserB.OffDuration  = 1.5f;
        laserB.Player       = player;

        // ── Zone B: floor spike (2 อัน, stagger) ─────────────────────────
        int[] spikeBx    = { 820, 860 };
        float[] spikeBph = { 0f, 0.6f };
        for (int i = 0; i < 2; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_b_{i}");
            s.Position    = new Vector2(spikeBx[i], 450);
            s.Origin      = SpikeOrigin.Floor;
            s.SpikeLength = 45f;
            s.PhaseOffset = spikeBph[i];
            s.Player      = player;
        }

        // ── Zone C: floor spike ในช่องว่าง (ตกลงมา = ตาย) ───────────────
        int[] spikeCx    = { 1340, 1380, 1420, 1460 };
        float[] spikeCph = { 0f, 0.4f, 0.8f, 1.2f };
        for (int i = 0; i < 4; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_c_{i}");
            s.Position    = new Vector2(spikeCx[i], 600);  // พื้นหุบเหว
            s.Origin      = SpikeOrigin.Floor;
            s.SpikeLength = 50f;
            s.PhaseOffset = spikeCph[i];
            s.Player      = player;
        }

        // ── Zone C: ceiling spike ใต้ platform C-2 (ห้อยลงมา) ───────────
        int[] spikeCeilX  = { 1700, 1740 };
        float[] spikeCeilP = { 0f, 0.7f };
        for (int i = 0; i < 2; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_ceil_{i}");
            s.Position    = new Vector2(spikeCeilX[i], 270);  // ใต้ plat_c2
            s.Origin      = SpikeOrigin.Ceiling;
            s.SpikeLength = 45f;
            s.PhaseOffset = spikeCeilP[i];
            s.Player      = player;
        }

        // ── Zone D: saw เร็ว บน platform D-1 ────────────────────────────
        var sawD = base.AddGameObject<SawTrap>("saw_d");
        sawD.Position       = new Vector2(1880, 310);
        sawD.MoveRange      = 120f;
        sawD.MoveSpeed      = 170f;
        sawD.MoveHorizontal = true;
        sawD.Player         = player;

        // ── Zone D: wall spike สลับซ้ายขวา (ช่องแคบ) ────────────────────
        var spikeWL = base.AddGameObject<SpikeTrap>("spike_wl");
        spikeWL.Position    = new Vector2(1960, 410);
        spikeWL.Origin      = SpikeOrigin.LeftWall;
        spikeWL.SpikeLength = 55f;
        spikeWL.PhaseOffset = 0f;
        spikeWL.Player      = player;

        var spikeWR = base.AddGameObject<SpikeTrap>("spike_wr");
        spikeWR.Position    = new Vector2(2060, 410);
        spikeWR.Origin      = SpikeOrigin.RightWall;
        spikeWR.SpikeLength = 55f;
        spikeWR.PhaseOffset = 0.8f;
        spikeWR.Player      = player;

        // ── Zone D: laser ค้างตลอด กระโดดข้าม ───────────────────────────
        var laserD = base.AddGameObject<LaserTrap>("laser_d");
        laserD.Position     = new Vector2(2150, 406);
        laserD.BeamLength   = 140f;
        laserD.IsHorizontal = true;
        laserD.AlwaysOn     = true;
        laserD.Player       = player;

        // ── Zone D: floor spike ก่อน platform สุดท้าย ────────────────────
        int[] spikeDx    = { 2230, 2270 };
        float[] spikeDph = { 0f, 0.5f };
        for (int i = 0; i < 2; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_d_{i}");
            s.Position    = new Vector2(spikeDx[i], 450);
            s.Origin      = SpikeOrigin.Floor;
            s.SpikeLength = 45f;
            s.PhaseOffset = spikeDph[i];
            s.Player      = player;
        }
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private void AddBlock(string name, int x, int y, int w, int h, Color color)
    {
        var go     = base.AddGameObject<GameObject>(name);
        go.Position = new Vector2(x, y);
        go.Scale    = new Vector2(w, h);
        var sr      = go.AddComponent<SpriteRenderer>();
        sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        sr.Tint       = color;
        sr.LayerDepth = 0.1f;
    }
}
