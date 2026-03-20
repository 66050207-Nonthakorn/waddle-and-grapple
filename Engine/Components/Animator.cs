using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Engine.Components;

public class Animator : Component
{
    private readonly Dictionary<string, Animation> _animations = [];
    private Animation _currentAnimation;
    private string _currentName;

    public string CurrentAnimationName => _currentName;

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