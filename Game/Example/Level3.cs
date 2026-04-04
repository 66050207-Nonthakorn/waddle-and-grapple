using System;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;

namespace WaddleAndGrapple.Game.Example;

class Level3 : BaseLevel
{
    Player player;
    GameObject cameraObject;
    
    GameObject tilemapObject;

    public override void Setup()
    {
        // set the value for progression
        LevelIndex = 3;
        SetTotalFish(16);

        // Create tilemap first
        tilemapObject = base.AddGameObject<GameObject>("tilemap");
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemap.Tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        tilemap.SourceTileSize = 75;
        tilemap.DestinationTileSize = 150;
        tilemap.GameObject.Scale = new Vector2(1f, 1f);
        tilemap.MapData = new int[,]
        {
            { 3, 3, 3, 3, 3, 3 },
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

    protected override void CompleteLevel()
    {
        _isLevelCompleted = true;

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.CompleteLevel(
                LevelIndex,
                TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()),
                _collectedFishCount,
                _totalFishInLevel,
                GetLatestCheckpoint());
        }
        
        SceneManager.Instance.LoadScene("Level3OutroCutscene");
    }
}
