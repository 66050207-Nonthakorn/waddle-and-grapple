using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Components.Tile;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Example;

public class TileLoaderDemoScene : Scene
{
    GameObject player;

    public override void Setup()
    {
        // Load and parse the JSON map file
        var map = TiledMapLoader.Load("Content/gametiles.tmj");

        // Create tilemap GameObjects with collision
        var tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");
        
        TiledMapLoader.CreateTilemapObjects(
            scene: this,
            map: map,
            tileset: tileset,
            baseLayer: 0.5f,
            solidTileIndices: [0, 2, 3, 4, 5]
        );

        // Create player GameObject
        player = base.AddGameObject<GameObject>("player");
        player.Position = new Vector2(100, 100);
        player.AddComponent<SpriteRenderer>().Texture = ResourceManager.Instance.GetTexture("bird");

        var rb = player.AddComponent<Rigidbody2D>();
        rb.GravityScale = 1f;
        rb.Drag = 0.5f;

        var collider = player.AddComponent<BoxCollider>();
        collider.Bounds = new Rectangle(0, 0, 32, 32);

        var camera = base.AddGameObject<GameObject>("camera");
        camera.AddComponent<Camera2D>().FollowTarget = player;

        base.Camera = camera.GetComponent<Camera2D>();   
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        var rb = player.GetComponent<Rigidbody2D>();
        float speed = 300f;
        float jumpForce = -500f;

        if (InputManager.Instance.IsKeyDown(Keys.Left))
            rb.Velocity = new Vector2(-speed, rb.Velocity.Y);
        else if (InputManager.Instance.IsKeyDown(Keys.Right))
            rb.Velocity = new Vector2(speed, rb.Velocity.Y);
        else
            rb.Velocity = new Vector2(0, rb.Velocity.Y);

        if (InputManager.Instance.IsKeyPressed(Keys.Space))
            rb.Velocity = new Vector2(rb.Velocity.X, jumpForce);
    }
}