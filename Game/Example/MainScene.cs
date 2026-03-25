using System.Collections.Generic;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GamePlayer = ComputerGameFinal.Game.Player;

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
        camera.SetViewport(new Viewport(0, 0, 800, 600));
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
            tile.Position = new Vector2(i * 150, 450); // top-left ของแต่ละช่อง
            tile.Scale    = new Vector2(150, 150);
            var sr        = tile.AddComponent<SpriteRenderer>();
            sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
            sr.Tint       = colors[i % 2];
            sr.LayerDepth = 0.1f;
        }

        // Platforms (x, y, width, height) — y คือ top ของ platform
        var platforms = new (int x, int y, int w, int h)[]
        {
            (350, 300, 200, 30),  // platform กลาง
            (600, 180, 150, 30),  // platform สูง
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
            new(0,   450, 900, 150),  // พื้น
            new(350, 300, 200,  30),  // platform กลาง
            new(600, 180, 150,  30),  // platform สูง
        };
        player.SetSolids(solids);

        camera.FollowTarget = player;
    }
}
