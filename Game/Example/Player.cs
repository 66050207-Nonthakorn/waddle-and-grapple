using System;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Physics;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Engine.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ComputerGameFinal.Game.Example;

public class Player : GameObject
{
    private const float MoveSpeed = 300f;

    private SpriteRenderer _spriteRenderer;
    private Animator _animator;

    public override void Initialize()
    {
        _spriteRenderer = AddComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;
        // _spriteRenderer.Texture = ResourceManager.Instance.GetTexture("bird");

        AnimationFactory factory = new AnimationFactory(
            ResourceManager.Instance.GetTexture("mario_walk"), 1, 3
        );
        
        Animation idle = factory.CreateFromCell(row: 0, col: 2, totalFrames: 1, frameDuration: 0.1f);
        Animation walk = factory.CreateFromRow(row: 0, totalFrames: 3, frameDuration: 0.1f);

        Scale = new Vector2(.4f, .4f);
        
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

        Position += speed * dt;
    }
}