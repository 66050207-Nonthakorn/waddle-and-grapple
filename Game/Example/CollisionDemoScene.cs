using System;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Physics;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ComputerGameFinal.Game.Example;

/// <summary>
/// Minimal scene that visually and textually confirms every collision callback fires.
///
/// Layout (screen coords, origin = top-left):
///   blue  box  [Player]     → starts at x=100, walk right with D
///   gray  box  [Wall]       → solid, x=420  — triggers OnCollisionEnter/Stay/Exit
///   green box  [TriggerZone]→ trigger, x=640 — triggers OnTriggerEnter/Stay/Exit
///
/// Watch the console:
///   [Collision] ENTER ← 'Wall'
///   [Collision] EXIT  ← 'Wall'
///   [Trigger]   ENTER ← 'TriggerZone'
///   [Trigger]   EXIT  ← 'TriggerZone'
///
/// Player tint: Blue=normal · Red=collision · Yellow=trigger
/// </summary>
public class CollisionDemoScene : Scene
{
    public override void Setup()
    {
        AddGameObject<DemoPlayer>("player").Position       = new Vector2(100, 250);
        AddGameObject<DemoWall>("wall").Position           = new Vector2(420, 250);
        AddGameObject<DemoTriggerZone>("trigger").Position = new Vector2(640, 250);
        AddGameObject<DemoCircle>("circle").Position       = new Vector2(400, 450);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Player — blue box, moves with WASD
// ─────────────────────────────────────────────────────────────────────────────
file class DemoPlayer : GameObject
{
    private const float Speed = 250f;
    private SpriteRenderer _renderer;
    private bool _inCollision;
    private bool _inTrigger;

    public override void Initialize()
    {
        Tag = "Player";

        _renderer            = AddComponent<SpriteRenderer>();
        _renderer.Texture    = ResourceManager.Instance.GetTexture("pixel");
        _renderer.Tint       = Color.CornflowerBlue;
        _renderer.LayerDepth = 0.5f;
        Scale = new Vector2(64, 64);

        var col    = AddComponent<BoxCollider>();
        col.Bounds  = new Rectangle(0, 0, 64, 64);
    
        var rb = AddComponent<Rigidbody2D>();
        rb.GravityScale = 0.0f;
    }

    public override void Update(GameTime gameTime)
    {
        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // WASD movement with rigidbody (physics-based)
        var dir = Vector2.Zero;
        if (InputManager.Instance.IsKeyDown(Keys.A)) dir.X -= 1;
        if (InputManager.Instance.IsKeyDown(Keys.D)) dir.X += 1;
        if (InputManager.Instance.IsKeyDown(Keys.W)) dir.Y -= 1;
        if (InputManager.Instance.IsKeyDown(Keys.S)) dir.Y += 1;
        var rb = GetComponent<Rigidbody2D>();
        rb.Velocity = dir * Speed;

        _renderer.Tint = _inCollision ? Color.Red
                       : _inTrigger   ? Color.Yellow
                                      : Color.CornflowerBlue;
    }

    // ── Solid collision callbacks ────────────────────────────────────────────
    public override void OnCollisionEnter2D(Collider col)
    {
        _inCollision = true;
        Console.WriteLine($"[Collision] ENTER  ← '{col.GameObject.Tag}'");
    }

    public override void OnCollisionStay2D(Collider col)
    {
        _inCollision = true;
        Console.WriteLine($"[Collision] STAY   ← '{col.GameObject.Tag}'");
    }

    public override void OnCollisionExit2D(Collider col)
    {
        _inCollision = false;
        Console.WriteLine($"[Collision] EXIT   ← '{col.GameObject.Tag}'");
    }

    // ── Trigger callbacks ────────────────────────────────────────────────────
    public override void OnTriggerEnter2D(Collider other)
    {
        _inTrigger = true;
        Console.WriteLine($"[Trigger]   ENTER  ← '{other.GameObject.Tag}'");
    }

    public override void OnTriggerStay2D(Collider other)
    {
        _inTrigger = true;
        Console.WriteLine($"[Trigger]   STAY   ← '{other.GameObject.Tag}'");
    }

    public override void OnTriggerExit2D(Collider other)
    {
        _inTrigger = false;
        Console.WriteLine($"[Trigger]   EXIT   ← '{other.GameObject.Tag}'");
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Wall — gray solid box
// ─────────────────────────────────────────────────────────────────────────────
file class DemoWall : GameObject
{
    public override void Initialize()
    {
        Tag = "Wall";

        var r        = AddComponent<SpriteRenderer>();
        r.Texture    = ResourceManager.Instance.GetTexture("pixel");
        r.Tint       = Color.DarkGray;
        r.LayerDepth = 0.3f;
        Scale = new Vector2(96, 128);

        var col    = AddComponent<BoxCollider>();
        col.Bounds  = new Rectangle(0, 0, 96, 128);
        col.IsTrigger = false;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TriggerZone — semi-transparent green trigger box
// ─────────────────────────────────────────────────────────────────────────────
file class DemoTriggerZone : GameObject
{
    public override void Initialize()
    {
        Tag = "TriggerZone";

        var r        = AddComponent<SpriteRenderer>();
        r.Texture    = ResourceManager.Instance.GetTexture("pixel");
        r.Tint       = new Color(0, 200, 0, 120);   // semi-transparent green
        r.LayerDepth = 0.2f;
        Scale = new Vector2(96, 128);

        var col       = AddComponent<BoxCollider>();
        col.Bounds     = new Rectangle(0, 0, 96, 128);
        col.IsTrigger = true;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Circle — orange circle, moves with arrow keys, tests CircleCollider
// ─────────────────────────────────────────────────────────────────────────────
file class DemoCircle : GameObject
{
    private const float Speed = 250f;
    private const int   Radius = 40;
    private SpriteRenderer _renderer;
    private bool _inCollision;
    private bool _inTrigger;

    public override void Initialize()
    {
        Tag = "Circle";

        _renderer            = AddComponent<SpriteRenderer>();
        _renderer.Texture    = ResourceManager.Instance.GetTexture("circle");
        _renderer.Tint       = Color.Orange;
        _renderer.LayerDepth = 0.5f;
        // Scale to diameter so the white pixel covers the circle's bounding square
        Scale = new Vector2(Radius * 2, Radius * 2) / ResourceManager.Instance.GetTexture("circle").Bounds.Size.ToVector2();

        var col    = AddComponent<CircleCollider>();
        col.Radius = Radius;

        var rb     = AddComponent<Rigidbody2D>();
        rb.GravityScale = 0.0f; // Disable gravity for top-down movement
    }

    public override void Update(GameTime gameTime)
    {
        float dt  = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var   dir = Vector2.Zero;
        if (InputManager.Instance.IsKeyDown(Keys.Left))  dir.X -= 1;
        if (InputManager.Instance.IsKeyDown(Keys.Right)) dir.X += 1;
        if (InputManager.Instance.IsKeyDown(Keys.Up))    dir.Y -= 1;
        if (InputManager.Instance.IsKeyDown(Keys.Down))  dir.Y += 1;

        Position += dir * Speed * dt;

        _renderer.Tint = _inCollision ? Color.Red
                       : _inTrigger   ? Color.Yellow
                                      : Color.Orange;
    }

    public override void OnCollisionEnter2D(Collider col)
    {
        _inCollision = true;
        Console.WriteLine($"[Circle Collision] ENTER  ← '{col.GameObject.Tag}'");
    }

    public override void OnCollisionStay2D(Collider col) {
        _inCollision = true;
        Console.WriteLine($"[Circle Collision] STAY   ← '{col.GameObject.Tag}'");
    }

    public override void OnCollisionExit2D(Collider col)
    {
        _inCollision = false;
        Console.WriteLine($"[Circle Collision] EXIT   ← '{col.GameObject.Tag}'");
    }

    public override void OnTriggerEnter2D(Collider other)
    {
        _inTrigger = true;
        Console.WriteLine($"[Circle Trigger]   ENTER  ← '{other.GameObject.Tag}'");
    }

    public override void OnTriggerStay2D(Collider other) {
        _inTrigger = true;
        Console.WriteLine($"[Circle Trigger]   STAY   ← '{other.GameObject.Tag}'");
    }

    public override void OnTriggerExit2D(Collider other)
    {
        _inTrigger = false;
        Console.WriteLine($"[Circle Trigger]   EXIT   ← '{other.GameObject.Tag}'");
    }
}
