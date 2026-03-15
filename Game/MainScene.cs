using System.Collections.Generic;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Tile;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Game;

class MainScene : Scene
{
    Player     player;
    GameObject cameraObject;
    GameObject tilemapObject;

    public override void Setup()
    {
        tilemapObject = base.AddGameObject<GameObject>("tilemap");

        cameraObject = base.AddGameObject<GameObject>("camera");
        var camera = cameraObject.AddComponent<Camera2D>();
        camera.SetViewport(new Viewport(0, 0, 800, 600));
        camera.Zoom = 1f;
        camera.SmoothFollow = false;
        base.Camera = camera;

        player = base.AddGameObject<Player>("player");
        player.Position = new Vector2(100, 100);
        player.Scale    = new Vector2(0.1f, 0.1f);
        player.SetSpawnPoint(new Vector2(100, 100));
        camera.FollowTarget = player;

        SetupTilemap();

        AddGameObject<Coin>("coin_1");
        AddGameObject<Coin>("coin_2");
        AddGameObject<Coin>("coin_3");
        AddGameObject<DoubleJumpPowerUp>("powerup_dj");
        AddGameObject<SpeedBoostPowerUp>("powerup_speed");

        SetupCollectible("coin_1",        new Vector2(150, 388));
        SetupCollectible("coin_2",        new Vector2(220, 388));
        SetupCollectible("coin_3",        new Vector2(290, 388));
        SetupCollectible("powerup_dj",    new Vector2(420, 384));
        SetupCollectible("powerup_speed", new Vector2(520, 384));

        player.SetCheckpoints(new List<CheckpointData>
        {
            new(new Rectangle(625, 355, 50, 45), new Vector2(650, 100))
        });
    }

    private void SetupTilemap()
    {
        var tileTexture = ResourceManager.Instance.GetTexture("Tiles/block");

        if (tileTexture != null)
        {
            new TilemapBuilder(tilemapObject)
                .WithTileset(tileTexture, 64, 64, solidTiles: [0])
                .AddLayer("ground", 25, 19, CreateLevelData(), layerDepth: 0.1f)
                .Build();

            tilemapObject.Position = Vector2.Zero;
        }
    }

    private void SetupCollectible(string name, Vector2 position)
    {
        var c = (Collectible)GameObjects[name];
        c.Position = position;
        c.SetPlayer(player);
    }

    private static int[] CreateLevelData()
    {
        const int width  = 25;
        const int height = 19;

        int[] data = TilemapBuilder.CreateGroundLayer(width, height, groundTileId: 0, groundHeight: 3);

        TilemapBuilder.FillRect(data, width, height, tileId: 0, startX: 5,  startY: 12, rectWidth: 5, rectHeight: 1);
        TilemapBuilder.FillRect(data, width, height, tileId: 0, startX: 15, startY: 8,  rectWidth: 4, rectHeight: 1);
        TilemapBuilder.FillRect(data, width, height, tileId: 0, startX: 10, startY: 10, rectWidth: 3, rectHeight: 1);

        return data;
    }
}
