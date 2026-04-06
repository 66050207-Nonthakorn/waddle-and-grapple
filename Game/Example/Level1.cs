using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WaddleAndGrapple.Game;
using GamePlayer = WaddleAndGrapple.Game.Player;

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


    public override void Setup()
    {
        LevelIndex = 1;
        SetTotalFish(0);

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
        bg.AddLayer("Parallax/Level1/level1-sky-overflow",   scrollFactor: 0.00f, layerDepth: 0.00f);
        bg.AddLayer("Parallax/Level1/level1-below-overflow", scrollFactor: 0.00f, layerDepth: 0.01f);
        bg.AddLayer("Parallax/Level1/level1-background",     scrollFactor: 0.05f, layerDepth: 0.02f);
        bg.AddLayer("Parallax/Level1/level1-farground",      scrollFactor: 0.15f, layerDepth: 0.03f);
        bg.AddLayer("Parallax/Level1/level1-midground",      scrollFactor: 0.30f, layerDepth: 0.04f);
        bg.AddLayer("Parallax/Level1/level1-nearground",     scrollFactor: 0.50f, layerDepth: 0.06f);

        // ── Player ────────────────────────────────────────────────────────────
        player = base.AddGameObject<GamePlayer>("player");
        var startSpawn = new Vector2(300, 390);
        player.Position = startSpawn;
        player.SetSpawnPoint(startSpawn);
        RegisterPlayerForProgression(player);

        // ══════════════════════════════════════════════════════════════════════
        // SOLIDS — ใช้ TileCollider จาก tiles แทน hardcode
        // ══════════════════════════════════════════════════════════════════════
        // var solids = new List<Microsoft.Xna.Framework.Rectangle>
        // {
        //     // ── พื้นหลัก (ช่องว่าง Grapple Zone: x=2680–3320) ─────────────────
        //     new(   0, 450, 2680, 150),   // Section 0 + 1 + ต้น 2 (x=0–2679)
        //     new(3320, 450, 1480, 150),   // ปลาย 2 + Section 3   (x=3320–4799)
        //
        //     // ── Section 0 ─────────────────────────────────────────────────────
        //     // low_ceil: bottom y=405 → blocks standing (top=390) แต่ให้ก้มผ่าน (top=420)
        //     new( 350, 385, 130, 20),     // low_ceil (x=350–479, bottom y=405) ← CROUCH HERE
        //     new( 530, 370, 260, 20),     // plat_a1 (x=530–789)  ← หลัง tunnel
        //     new( 830, 295, 250, 20),     // plat_a2 (x=830–1079) ← Ledge Grab / SpeedBoost
        //
        //     // ── Section 1 ─────────────────────────────────────────────────────
        //     new(1240, 370, 260, 20),     // plat_b1 (x=1240–1499) ← escape route จากช้าง
        //     new(1700, 295, 250, 20),     // plat_b2 (x=1700–1949)
        //     new(2050, 235, 250, 20),     // plat_b3 (x=2050–2299) ← DoubleJump reward
        //
        //     // ── Section 2 — Grapple Platforms ────────────────────────────────
        //     // y=305, y=265 < 324 (max jump peak) → ต้องใช้ pickaxe ข้าม
        //     new(2800, 305, 200, 20),     // plat_c1 (x=2800–2999) ← hook target 1
        //     new(3080, 265, 180, 20),     // plat_c2 (x=3080–3259) ← hook target 2
        //
        //     // ── Section 3 ─────────────────────────────────────────────────────
        //     new(3700, 370, 280, 20),     // plat_d1 (x=3700–3979) ← SlowTime
        //     new(4300, 345, 230, 20),     // plat_d2 (x=4300–4529) ← เหนือ traps
        //     new(4580, 260, 180, 20),     // plat_d3 (x=4580–4759) ← Goal
        // };
        // player.SetSolids(solids);
        // enemy1.SetSolids(solids);

        // ══════════════════════════════════════════════════════════════════════
        // TILE MAP — โหลดจาก Level1.tmj ผ่าน GameMapLoader (tile 16×16)
        // ══════════════════════════════════════════════════════════════════════
        var tileset          = ResourceManager.Instance.GetTexture("Tiles/LevelTileSet");
        var solidTileIndices = LoadSolidTileIndicesFromTileset("Assets/Tiled/LevelTileSet.tsx");

        var mapLoader = new GameMapLoader(this, "Assets/Tiled/Level1.tmj", player);
        var mapResult = mapLoader.Load(tileset, baseLayer: 0.5f, solidTileIndices: solidTileIndices);

        if (mapResult.Tilemaps.Count > 0)
        {
            var tileCollider = mapResult.Tilemaps[0].GetComponent<TileCollider>();
            if (tileCollider != null)
                player.SetSolids(tileCollider.GetSolidRects());
        }

        var tiledMap = mapResult.Map;
        player.SetWorldBounds(
            left: 0f,
            right: tiledMap.Width * tiledMap.TileWidth,
            fallDeathY: (tiledMap.Height * tiledMap.TileHeight) + 160f);

        // ══════════════════════════════════════════════════════════════════════
        // CHECKPOINTS / SECTIONS — อ่านจาก Room layer ใน Tiled
        // ══════════════════════════════════════════════════════════════════════
        CheckpointManager.Instance.Reset();
        var roomLayer = tiledMap.ObjectLayers.Find(l => l.Name == "Room");
        var rooms     = roomLayer.Objects.OrderBy(r => r.X).ToList();
        var sections  = new List<Section>();
        for (int i = 0; i < rooms.Count; i++)
        {
            var r    = rooms[i];
            int left = (int)r.X;
            int right = (int)(r.X + r.Width);
            sections.Add(new Section
            {
                Id              = i,
                LeftBound       = left,
                RightBound      = right,
                LeftSpawnPoint  = i == 0 ? startSpawn : new Vector2(left + 20, 450),
                RightSpawnPoint = new Vector2(right - 20, 450),
            });
        }
        CheckpointManager.Instance.RegisterSections(sections.ToArray());
        CheckpointManager.Instance.UpdateSection(player.Position.X);

        var checkpointAreas = new List<CheckpointData>();
        for (int i = 0; i < sections.Count; i++)
        {
            var section = sections[i];
            var room    = rooms[i];
            int left    = (int)room.X;
            int top     = (int)room.Y;
            int width   = (int)room.Width;
            int height  = (int)room.Height;
            int triggerWidth = Math.Min(32, width);

            checkpointAreas.Add(new CheckpointData(
                new Rectangle(left, top, triggerWidth, height),
                section.LeftSpawnPoint));

            checkpointAreas.Add(new CheckpointData(
                new Rectangle(Math.Max(left, left + width - triggerWidth), top, triggerWidth, height),
                section.RightSpawnPoint));
        }
        player.SetCheckpoints(checkpointAreas);

        camera.FollowTarget = player;

        base.Setup(); // สร้าง PausedPanel + TimerUI (ต้องเป็นบรรทัดสุดท้าย)
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns 0-based tile indices matching MapLoader's MapData (rawGid - 1).
    // TSX tile id is already 0-based for firstGid=1, so we use it directly.
    private static int[] LoadSolidTileIndicesFromTileset(string tsxPath)
    {
        var doc = XDocument.Load(tsxPath);
        XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        return doc
            .Descendants(ns + "tile")
            .Where(tile => tile.Element(ns + "objectgroup") != null)
            .Select(tile => (int?)tile.Attribute("id"))
            .Where(id => id.HasValue)
            .Select(id => id.Value)
            .Distinct()
            .ToArray();
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
