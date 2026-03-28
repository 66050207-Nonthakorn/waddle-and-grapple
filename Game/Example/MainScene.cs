using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace WaddleAndGrapple.Game.Example;

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
        // camera.SmoothFollow = true;
        // camera.FollowSpeed = 2f;

        base.Camera = camera;
        
        player = base.AddGameObject<Player>("player");
        player.Position = new Vector2(100, 100);
        player.Scale = new Vector2(0.75f, 0.75f);

        camera.FollowTarget = player;
    }
}
