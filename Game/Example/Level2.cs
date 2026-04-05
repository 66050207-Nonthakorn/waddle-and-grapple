using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using GamePlayer = WaddleAndGrapple.Game.Player;

namespace WaddleAndGrapple.Game.Example;

class Level2 : BaseLevel
{
    GamePlayer player;
    GameObject cameraObject;
    
    GameObject tilemapObject;

    public override void Setup()
    {
        // set the value for progression
        LevelIndex = 2;
        SetTotalFish(12);

        // Create tilemap first
        tilemapObject = base.AddGameObject<GameObject>("tilemap");
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.Tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        tilemap.SourceTileSize = 75;
        tilemap.DestinationTileSize = 150;
        tilemap.GameObject.Scale = new Vector2(1f, 1f);
        tilemap.MapData = new int[,]
        {
            { 2, 2, 2, 2, 2, 2 },
        };

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
        // camera.FollowSpeed = 2f;

        base.Camera = camera;
        
        player = base.AddGameObject<GamePlayer>("player");
        player.Position = new Vector2(100, 100);
        player.Scale = new Vector2(0.75f, 0.75f);
        player.SetSolids(tileCollider.GetSolidRects());
        RegisterPlayerForProgression(player);

        camera.FollowTarget = player;

        // call base setup last to ensure create UI objects are on top of everything else
        base.Setup();
    }
}
