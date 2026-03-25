using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Engine.Components;

public class Animation(
    Texture2D sheet,
    List<Rectangle> frames,
    float frameDuration,
    bool isLooping = true
) {
    public Texture2D Sheet { get; } = sheet;
    public Rectangle CurrentSourceRect => frames[currentFrame];

    public float FrameDuration { get; set; } = frameDuration;
    public bool IsLooping { get; set; } = isLooping;
    public bool IsFinished { get; private set; }

    private readonly List<Rectangle> frames = frames;
    private float timer;
    private int currentFrame;

    public void UpdateAnimation(GameTime gameTime)
    {
        if (IsFinished) return;

        timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (timer >= FrameDuration)
        {
            timer -= FrameDuration;

            int next = currentFrame + 1;
            if (next >= frames.Count)
            {
                if (IsLooping)
                    currentFrame = 0;
                else
                    IsFinished = true;
            }
            else
            {
                currentFrame = next;
            }
        }
    }

    public void Reset()
    {
        currentFrame = 0;
        timer = 0f;
        IsFinished = false;
    }
}