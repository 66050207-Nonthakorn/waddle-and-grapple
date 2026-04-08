using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.Components;

/// <summary>
/// Spritesheet animation — frames laid out horizontally in a single texture.
/// frameCount is auto-calculated from texture.Width / frameWidth.
/// </summary>
public class SpriteAnimation
{
    public Texture2D Texture       { get; }
    public float     FrameDuration { get; }

    private readonly int _frameWidth;
    private readonly int _frameHeight;
    private readonly int _frameCount;

    private float _timer;
    private int   _frame;

    public SpriteAnimation(Texture2D spritesheet, int frameWidth, float frameDuration)
    {
        Texture       = spritesheet;
        FrameDuration = frameDuration;
        _frameWidth   = frameWidth;
        _frameHeight  = spritesheet.Height;
        _frameCount   = spritesheet.Width / frameWidth;
    }

    public Rectangle SourceRect =>
        new Rectangle(_frame * _frameWidth, 0, _frameWidth, _frameHeight);

    public void Reset()
    {
        _frame = 0;
        _timer = 0f;
    }

    public void Update(float dt)
    {
        _timer += dt;
        if (_timer >= FrameDuration)
        {
            _timer -= FrameDuration;
            _frame  = (_frame + 1) % _frameCount;
        }
    }
}
