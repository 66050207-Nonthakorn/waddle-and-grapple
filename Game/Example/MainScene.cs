using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using System.Collections.Generic;
using GamePlayer = WaddleAndGrapple.Game.Player;

namespace WaddleAndGrapple.Game.Example;

class MainScene : Scene
{
    GamePlayer player;
    GameObject cameraObject;
    GameObject tilemapObject;

    public override void Setup()
    {
        // Create tilemap first
        tilemapObject = base.AddGameObject<GameObject>("tilemap");
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.Tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        tilemap.SourceTileSize = 75;
        tilemap.DestinationTileSize = 75;
        tilemap.Layer = 0.5f;
        tilemap.MapData = new int[,]
        {
            { 1, 1, 1, 1, -1, -1 },
            { 5, 4, 3, 2, 1, 0 },
            { 1, 1, 1, 1, 1, 1 },
            { 1, 2, 3, 4, 5, 6 }
        };

        tilemap.GameObject.Scale = new Vector2(1f, 1f);

        var tileCollider = tilemapObject.AddComponent<TileCollider>();
        tileCollider.SetSolid(0, 1, 2, 3, 4, 5);

        // Create camera
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

        // Player (Game.Player — ตัวเต็มพร้อม physics)
        player = base.AddGameObject<GamePlayer>("player");
        player.Position = new Vector2(200, 380);

        // วาดพื้นเป็นแถบสีสลับ 6 ช่อง ช่องละ 150×150 px
        var colors = new[] { new Color(60, 80, 120), new Color(40, 55, 90) };
        for (int i = 0; i < 6; i++)
        {
            var tile = base.AddGameObject<GameObject>($"floor_{i}");
            tile.Position = new Vector2(i * 150, 450);
            tile.Scale    = new Vector2(150, 150);
            var sr        = tile.AddComponent<SpriteRenderer>();
            sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint       = colors[i % 2];
            sr.LayerDepth = 0.1f;
        }

        // Platforms (x, y, width, height)
        var platforms = new (int x, int y, int w, int h)[]
        {
            (350, 300, 200, 30),
            (600, 180, 150, 30),
        };

        var platformColors = new[] { new Color(80, 120, 80), new Color(60, 100, 60) };
        foreach (var (x, y, w, h) in platforms)
        {
            var p  = base.AddGameObject<GameObject>($"platform_{x}");
            p.Position = new Vector2(x, y);
            p.Scale    = new Vector2(w, h);
            var sr     = p.AddComponent<SpriteRenderer>();
            sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint       = platformColors[0];
            sr.LayerDepth = 0.1f;
        }

        // Solid rects สำหรับ collision
        var solids = new List<Microsoft.Xna.Framework.Rectangle>
        {
            new(0,   450, 900, 150),
            new(350, 300, 200,  30),
            new(600, 180, 150,  30),
        };
        player.SetSolids(solids);

        camera.FollowTarget = player;
    }
}
