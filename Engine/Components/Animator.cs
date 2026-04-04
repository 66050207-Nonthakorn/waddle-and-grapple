using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Engine.Components;

public class Animator : Component
{
    private readonly Dictionary<string, Animation> _animations = [];
    private Animation _currentAnimation;
    private string _currentName;

    public string CurrentAnimationName => _currentName;
    public int CurrentLoopCount => _currentAnimation?.LoopCount ?? 0;

    public void AddAnimation(string name, Animation animation)
    {
        _animations[name] = animation;
    }

    public void Play(string name)
    {
        if (_currentName == name) return;
        if (_animations.TryGetValue(name, out var animation))
        {
            _currentAnimation = animation;
            _currentAnimation.Reset();
            _currentName = name;
        }
    }

    /// <summary>เปลี่ยนไป animation ที่ระบุแล้วข้ามไปที่ frame สุดท้ายทันที
    /// ใช้เมื่อต้องการแสดง pose สุดท้ายโดยไม่เล่น transition ซ้ำ</summary>
    public void PlayAtEnd(string name)
    {
        bool alreadyPlaying = _currentName == name;
        Play(name); // Reset ถ้าเป็น animation ใหม่
        if (!alreadyPlaying && _currentAnimation != null)
            _currentAnimation.SkipToLastFrame();
    }

    public override void Initialize()
    {
        if (!GameObject.HasComponent<SpriteRenderer>())
        {
            GameObject.AddComponent<SpriteRenderer>();
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_currentAnimation == null) return;

        _currentAnimation.UpdateAnimation(gameTime);

        // Sync the SpriteRenderer on the same GameObject
        var renderer = GameObject.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.Texture          = _currentAnimation.Sheet;
            renderer.SourceRectangle  = _currentAnimation.CurrentSourceRect;

            // Re-centre the origin based on the frame size
            var r = _currentAnimation.CurrentSourceRect;
            renderer.Origin = new Vector2(r.Width / 2f, r.Height / 2f);
        }
    }
}