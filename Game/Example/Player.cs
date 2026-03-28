using System;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace WaddleAndGrapple.Game.Example;

public class Player : GameObject
{
    private const float MoveSpeed = 300f;

    private Animator _animator;

    private Rigidbody2D _rigidbody;

    private SpriteRenderer _spriteRenderer;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();

        AnimationFactory factory = new AnimationFactory(
            ResourceManager.Instance.GetTexture("elephant-animation"),
            rows: 8,
            columns: 8
        );
        
        Animation idle = factory.CreateFromRow(row: 0, totalFrames: 1, frameDuration: .05f);
        Animation walk = factory.CreateFromRow(row: 2, totalFrames: 8, frameDuration: .05f);

        Scale = new Vector2(1.5f, 1.5f);

        _rigidbody = AddComponent<Rigidbody2D>();
        _rigidbody.GravityScale = 0f;

        var boxCollider = AddComponent<BoxCollider>();
        // Frame size 190×270 at scale 0.4 → ~76×108px; center it around Position
        boxCollider.Bounds = new Rectangle(-38, -54, 76, 108);
        
        _animator = AddComponent<Animator>();
        _animator.AddAnimation("idle", idle);
        _animator.AddAnimation("walk", walk);
        _animator.Play("idle");
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector2 speed = Vector2.Zero;

        if (InputManager.Instance.IsKeyDown(Keys.A))
        {
            speed.X = -MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.D))
        {
            speed.X = MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.W))
        {
            speed.Y = -MoveSpeed;
        }
        if (InputManager.Instance.IsKeyDown(Keys.S))
        {
            speed.Y = MoveSpeed;
        }

        if (speed != Vector2.Zero)
        {
            _animator.Play("walk");

            if (speed.X < 0)
            {;
                base.Rotation = QuaternionUtils.Euler(0, 180, 0); 
            }
            else
            {
                base.Rotation = QuaternionUtils.Euler(0, 0, 0);
            }
        }
        else
        {
            _animator.Play("idle");
        }
        
        _rigidbody.Velocity = speed;
    }
}