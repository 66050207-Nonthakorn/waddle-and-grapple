using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game.Example;

public class GameMapLoaderDemo : Scene
{
    Game.Player player;

    public override void Setup()
    {
        var tileset = ResourceManager.Instance.GetTexture("Tiles/tileset");

        // Load map, build tile layers, and spawn all registered objects in one call
        var loader = new GameMapLoader(this, "Content/testtest.tmj", player);
        loader.Load(tileset, baseLayer: 0.5f);

        // Create player GameObject
        player = base.AddGameObject<Game.Player>("player");
        player.Position = new Vector2(100, 100);

        var rb = player.AddComponent<Rigidbody2D>();
        rb.GravityScale = 0f;
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