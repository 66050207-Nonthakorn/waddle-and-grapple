using System.Collections.Generic;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GamePlayer = ComputerGameFinal.Game.Player;
using MonoGameGum;

namespace ComputerGameFinal.Game.Example;

class MainScene : Scene
{
    GamePlayer player;
    GameObject cameraObject;

    public override void Setup()
    {
        // Camera
        cameraObject = base.AddGameObject<GameObject>("camera");
        var camera = cameraObject.AddComponent<Camera2D>();
        camera.SetViewport(new Viewport(
            0,
            0,
            ScreenManager.Instance.nativeWidth,
            ScreenManager.Instance.nativeHeight
        ));
        camera.Zoom = 1f;
        camera.SmoothFollow = false;
        base.Camera = camera;

        // Player
        player = base.AddGameObject<GamePlayer>("player");
        var startSpawn = new Vector2(140, 380);
        player.Position = startSpawn;

        // ── Floor: 16 tiles × 150px = 2400px ─────────────────────────────────
        var floorColors = new[] { new Color(60, 80, 120), new Color(40, 55, 90) };
        for (int i = 0; i < 16; i++)
        {
            var tile      = base.AddGameObject<GameObject>($"floor_{i}");
            tile.Position = new Vector2(i * 150, 450);
            tile.Scale    = new Vector2(150, 150);
            var sr        = tile.AddComponent<SpriteRenderer>();
            sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint       = floorColors[i % 2];
            sr.LayerDepth = 0.1f;
        }

        // ── Platforms ─────────────────────────────────────────────────────────
        // (name, x, y, w, h)
        var platformDefs = new (string n, int x, int y, int w, int h)[]
        {
            ("platform_350",  350, 300, 200, 30),  // zone 1: กลาง
            ("platform_600",  600, 180, 150, 30),  // zone 1: สูง
            ("platform_1070", 1070, 360, 180, 20), // zone 4: ceiling spike
            ("platform_1820", 1820, 320, 170, 24), // zone 7: mini-map area
            ("platform_2010", 2010, 260, 150, 24), // zone 7: mini-map area
            ("platform_2200", 2200, 340, 180, 24), // zone 7: mini-map area
        };

        foreach (var (n, x, y, w, h) in platformDefs)
        {
            var p     = base.AddGameObject<GameObject>(n);
            p.Position = new Vector2(x, y);
            p.Scale    = new Vector2(w, h);
            var sr    = p.AddComponent<SpriteRenderer>();
            sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint       = new Color(80, 120, 80);
            sr.LayerDepth = 0.1f;
        }

        // ── Solids ────────────────────────────────────────────────────────────
        var solids = new List<Microsoft.Xna.Framework.Rectangle>
        {
            new(0,    450, 2400, 150),  // พื้นเต็มความยาว scene
            new(350,  300,  200,  30),  // platform กลาง
            new(600,  180,  150,  30),  // platform สูง
            new(1070, 360,  180,  20),  // platform ceiling spike zone
            new(1820, 320,  170,  24),  // mini-map platform A
            new(2010, 260,  150,  24),  // mini-map platform B
            new(2200, 340,  180,  24),  // mini-map platform C
        };
        player.SetSolids(solids);
        player.SetSpawnPoint(startSpawn); // level-start fallback

        // ── Sections / Checkpoints ────────────────────────────────────────────
        CheckpointManager.Instance.Reset();
        CheckpointManager.Instance.RegisterSections(new[]
        {
            new Section { Id = 0, LeftBound =    0, RightBound =  900, LeftSpawnPoint = startSpawn, RightSpawnPoint = new Vector2(880, 380) },
            new Section { Id = 1, LeftBound =  900, RightBound = 1400, LeftSpawnPoint = new Vector2(920, 380), RightSpawnPoint = new Vector2(1360, 380) },
            new Section { Id = 2, LeftBound = 1400, RightBound = 1900, LeftSpawnPoint = new Vector2(1460, 380), RightSpawnPoint = new Vector2(1860, 380) },
            new Section { Id = 3, LeftBound = 1900, RightBound = 2400, LeftSpawnPoint = new Vector2(1960, 380), RightSpawnPoint = new Vector2(2320, 380) },
        });

        var spawnMarker = base.AddGameObject<GameObject>("spawn_marker_start");
        spawnMarker.Position = new Vector2(startSpawn.X, startSpawn.Y - 32f);
        spawnMarker.Scale    = new Vector2(10, 64);
        var spawnMarkerSr    = spawnMarker.AddComponent<SpriteRenderer>();
        spawnMarkerSr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        spawnMarkerSr.Tint       = new Color(60, 180, 120);
        spawnMarkerSr.LayerDepth = 0.4f;

        // Visual markers at section boundaries (blue poles)
        foreach (var (name, x) in new[] { ("cp_marker_1", 900f), ("cp_marker_2", 1400f), ("cp_marker_3", 1900f) })
        {
            var marker      = base.AddGameObject<GameObject>(name);
            marker.Position = new Vector2(x, 380);
            marker.Scale    = new Vector2(12, 70);
            var sr          = marker.AddComponent<SpriteRenderer>();
            sr.Texture      = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint         = new Color(80, 80, 200);
            sr.LayerDepth   = 0.4f;
        }

        camera.FollowTarget = player;

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 1 — Saw Traps  (x = 0–900, original area)
        // ═════════════════════════════════════════════════════════════════════

        // Saw บนพื้น: เดินซ้าย-ขวา x=320–520 (เว้นพื้นที่ปลอดภัยให้จุด spawn)
        var saw1 = base.AddGameObject<SawTrap>("saw1");
        saw1.Position       = new Vector2(320, 420);
        saw1.MoveRange      = 200f;
        saw1.MoveSpeed      = 100f;
        saw1.MoveHorizontal = true;
        saw1.Player         = player;

        // Saw บน platform กลาง: x=360–510
        var saw2 = base.AddGameObject<SawTrap>("saw2");
        saw2.Position       = new Vector2(360, 270);
        saw2.MoveRange      = 150f;
        saw2.MoveSpeed      = 120f;
        saw2.MoveHorizontal = true;
        saw2.Player         = player;

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 2 — Laser Traps  (x = 0–900, original area)
        // ═════════════════════════════════════════════════════════════════════

        // Laser ค้างตลอด — แนวนอน x=450–650
        var laser1 = base.AddGameObject<LaserTrap>("laser_always");
        laser1.Position     = new Vector2(450, 406);
        laser1.BeamLength   = 200f;
        laser1.IsHorizontal = true;
        laser1.AlwaysOn     = true;
        laser1.Player       = player;

        // Laser เปิด-ปิด (2s/1.5s) — แนวตั้ง x=320, y=300–450
        var laser2 = base.AddGameObject<LaserTrap>("laser_timed");
        laser2.Position     = new Vector2(320, 300);
        laser2.BeamLength   = 150f;
        laser2.IsHorizontal = false;
        laser2.AlwaysOn     = false;
        laser2.OnDuration   = 2f;
        laser2.OffDuration  = 1.5f;
        laser2.Player       = player;

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 3 — Floor Spikes  (x = 920–1060)
        // ผุดขึ้นจากพื้นสลับ phase ให้ต้องวิ่งหลบ
        // ═════════════════════════════════════════════════════════════════════

        int[]   floorSpikeX   = { 920, 960, 1000, 1040 };
        float[] floorPhase    = { 0f, 0.5f, 1.0f, 1.5f };
        for (int i = 0; i < 4; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_floor_{i}");
            s.Position    = new Vector2(floorSpikeX[i], 450);
            s.Origin      = SpikeOrigin.Floor;
            s.SpikeLength = 45f;
            s.PhaseOffset = floorPhase[i];
            s.Player      = player;
        }

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 4 — Ceiling Spikes  (x = 1070–1250, ห้อยลงใต้ platform)
        // ═════════════════════════════════════════════════════════════════════

        // platform_1070 อยู่ที่ y=360, ความสูง 20 → bottom = y=380
        int[]   ceilSpikeX  = { 1085, 1130, 1175 };
        float[] ceilPhase   = { 0f, 0.6f, 1.2f };
        for (int i = 0; i < 3; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_ceil_{i}");
            s.Position    = new Vector2(ceilSpikeX[i], 380); // BaseY = platform bottom
            s.Origin      = SpikeOrigin.Ceiling;
            s.SpikeLength = 45f;
            s.PhaseOffset = ceilPhase[i];
            s.Player      = player;
        }

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 5 — Wall Spikes  (x = 1540–1680, ช่องทางแคบ)
        // หนามสลับสองข้าง ผู้เล่นต้องรอจังหวะผ่าน
        // ═════════════════════════════════════════════════════════════════════

        // ซ้าย: ยื่นขวาจาก x=1540 → ถึง x=1595 เมื่อ extend เต็ม
        var spikeWallL = base.AddGameObject<SpikeTrap>("spike_wall_left");
        spikeWallL.Position    = new Vector2(1540, 405);
        spikeWallL.Origin      = SpikeOrigin.LeftWall;
        spikeWallL.SpikeLength = 55f;
        spikeWallL.PhaseOffset = 0f;
        spikeWallL.Player      = player;

        // ขวา: ยื่นซ้ายจาก x=1680 → ถึง x=1625 เมื่อ extend เต็ม (gap 30px)
        // phase ต่างกัน 0.75s → สลับกันเพื่อให้มีหน้าต่างผ่าน
        var spikeWallR = base.AddGameObject<SpikeTrap>("spike_wall_right");
        spikeWallR.Position    = new Vector2(1680, 405);
        spikeWallR.Origin      = SpikeOrigin.RightWall;
        spikeWallR.SpikeLength = 55f;
        spikeWallR.PhaseOffset = 0.75f;
        spikeWallR.Player      = player;

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 6 — Final Floor Spikes  (x = 1740–1780)
        // ═════════════════════════════════════════════════════════════════════

        var spikeFinalA = base.AddGameObject<SpikeTrap>("spike_final_a");
        spikeFinalA.Position    = new Vector2(1740, 450);
        spikeFinalA.Origin      = SpikeOrigin.Floor;
        spikeFinalA.SpikeLength = 45f;
        spikeFinalA.PhaseOffset = 0f;
        spikeFinalA.Player      = player;

        var spikeFinalB = base.AddGameObject<SpikeTrap>("spike_final_b");
        spikeFinalB.Position    = new Vector2(1775, 450);
        spikeFinalB.Origin      = SpikeOrigin.Floor;
        spikeFinalB.SpikeLength = 45f;
        spikeFinalB.PhaseOffset = 0.7f;
        spikeFinalB.Player      = player;

        // ═════════════════════════════════════════════════════════════════════
        // ZONE 7 — Mini Demo Map  (x = 1820–2380)
        // โซนย่อยเพิ่มพื้นที่ demo ให้มีทั้งพื้น, platform และกับดักผสม
        // ═════════════════════════════════════════════════════════════════════

        var saw3 = base.AddGameObject<SawTrap>("saw_mini_1");
        saw3.Position       = new Vector2(2030, 230);
        saw3.MoveRange      = 120f;
        saw3.MoveSpeed      = 110f;
        saw3.MoveHorizontal = true;
        saw3.Player         = player;

        var laser3 = base.AddGameObject<LaserTrap>("laser_mini_gate");
        laser3.Position     = new Vector2(2120, 420);
        laser3.BeamLength   = 160f;
        laser3.IsHorizontal = true;
        laser3.AlwaysOn     = false;
        laser3.OnDuration   = 1.4f;
        laser3.OffDuration  = 1.0f;
        laser3.Player       = player;

        int[]   miniFloorSpikeX = { 2060, 2100, 2140 };
        float[] miniFloorPhase  = { 0f, 0.45f, 0.9f };
        for (int i = 0; i < miniFloorSpikeX.Length; i++)
        {
            var s = base.AddGameObject<SpikeTrap>($"spike_mini_floor_{i}");
            s.Position    = new Vector2(miniFloorSpikeX[i], 450);
            s.Origin      = SpikeOrigin.Floor;
            s.SpikeLength = 40f;
            s.PhaseOffset = miniFloorPhase[i];
            s.Player      = player;
        }
    }
}
