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

class Level1 : BaseLevel
{
    GamePlayer player;
    GameObject cameraObject;


    public override void Setup()
    {
        LevelIndex = 1;
        SetTotalFish(6);

        AudioManager.Instance.PlaySong("Song/Level1");

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
        // TILE MAP — โหลดจาก Level1.tmj ผ่าน GameMapLoader (tile 16×16)
        // ══════════════════════════════════════════════════════════════════════
        var tileset          = ResourceManager.Instance.GetTexture("Tiles/LevelTileSet");
        var solidTileIndices = LoadSolidTileIndicesFromTileset("Assets/Tiled/LevelTileSet.tsx");

        var mapLoader = new GameMapLoader(this, "Assets/Tiled/Level1.tmj", player);
        var mapResult = mapLoader.Load(tileset, baseLayer: 0.5f, solidTileIndices: solidTileIndices);

        foreach (var goal in mapResult.GetSpawned<GoalFlag>())
        {
            goal.OnComplete = CompleteLevel;
        }

        if (mapResult.Tilemaps.Count > 0)
        {
            var tileCollider = mapResult.Tilemaps[0].GetComponent<TileCollider>();
            if (tileCollider != null)
            {
                var solids = tileCollider.GetSolidRects();
                player.SetSolids(solids);
                foreach (var enemy in mapResult.GetSpawned<Enemy>())
                    enemy.SetSolids(solids);
            }
        }

        var enemies = mapResult.GetSpawned<Enemy>().ToList();
        foreach (var trap in mapResult.GetSpawned<Trap>())
            trap.Enemies = enemies;

        var tiledMap = mapResult.Map;
        int mapTileWidth = tiledMap.TileLayers.Count > 0
            ? tiledMap.TileLayers[0].MapData.GetLength(1)
            : tiledMap.Width;
        int mapTileHeight = tiledMap.TileLayers.Count > 0
            ? tiledMap.TileLayers[0].MapData.GetLength(0)
            : tiledMap.Height;
        // player.SetWorldBounds(
        //     left: 0f,
        //     right: tiledMap.Width * tiledMap.TileWidth,
        //     fallDeathY: (tiledMap.Height * tiledMap.TileHeight) + 1f);

        // Set map dimensions for camera bounds clamping
        MapWidth = mapTileWidth * tiledMap.TileWidth;
        MapHeight = mapTileHeight * tiledMap.TileHeight;

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
                TopBound        = (int)r.Y,
                BottomBound     = (int)(r.Y + r.Height),
                LeftSpawnPoint  = i == 0 ? startSpawn : new Vector2(left + 20, 450),
                RightSpawnPoint = new Vector2(right - 20, 450),
            });
        }
        CheckpointManager.Instance.RegisterSections(sections.ToArray());
        CheckpointManager.Instance.UpdateSection(player.Position.X, player.Position.Y);

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
