using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Tile;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Game.Example;

class MainScene : Scene
{
    Player player;
    GameObject cameraObject;
    
    GameObject tilemapObject;

    public override void Setup()
    {
        // Create tilemap first
        tilemapObject = base.AddGameObject<GameObject>("tilemap");
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.Tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        tilemap.SourceTileSize = 75;
        tilemap.DestinationTileSize = 150;
        tilemap.GameObject.Scale = new Vector2(1f, 1f);
        tilemap.MapData = new int[,]
        {
            { 5, 4, 3, 2, 1, 0 },
        };

        // Create camera
        cameraObject = base.AddGameObject<GameObject>("camera");
        var camera = cameraObject.AddComponent<Camera2D>();
        camera.SetViewport(new Viewport(0, 0, 800, 600));
        camera.Zoom = 1f;
        camera.SmoothFollow = false;
        // camera.FollowSpeed = 2f;

        base.Camera = camera;
        
        player = base.AddGameObject<Player>("player");
        player.Position = new Vector2(100, 100);
        player.Scale = new Vector2(0.75f, 0.75f);

        camera.FollowTarget = player;
    }
}
