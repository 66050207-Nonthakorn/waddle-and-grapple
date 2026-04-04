using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Tile;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace ComputerGameFinal.Game.Example;

class Level1 : BaseLevel
{
    Player player;
    GameObject cameraObject;
    
    GameObject tilemapObject;

    public override void Setup()
    {
        // set the value for progression
        LevelIndex = 1;
        SetTotalFish(10);

        // Create tilemap first
        tilemapObject = base.AddGameObject<GameObject>("tilemap");
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.Tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        tilemap.SourceTileSize = 75;
        tilemap.DestinationTileSize = 150;
        tilemap.GameObject.Scale = new Vector2(1f, 1f);
        tilemap.MapData = new int[,]
        {
            { 1, 1, 1, 1, 1, 1 },
        };

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
        // camera.FollowSpeed = 2f;

        base.Camera = camera;
        
        player = base.AddGameObject<Player>("player");
        player.Position = new Vector2(100, 100);
        player.Scale = new Vector2(0.75f, 0.75f);
        RegisterPlayerForProgression(player);

        camera.FollowTarget = player;

        // call base setup last to ensure create UI objects are on top of everything else
        base.Setup();
    }
}
