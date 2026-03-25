using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Engine.Components;

public class AnimationFactory(Texture2D sheet, int rows, int columns)
{
    private readonly int frameWidth = sheet.Width / columns;
    private readonly int frameHeight = sheet.Height / rows;

    public Animation CreateFromRow(int row, int totalFrames, float frameDuration, int spaceX = 0, int spaceY = 0, bool isLooping = true)
    {
        var frames = new List<Rectangle>();
        for (int x = 0; x < totalFrames; x++)
        {
            int c = x % columns;
            int r = row + x / columns;

            frames.Add(new Rectangle(
                c * (frameWidth + spaceX),
                r * (frameHeight + spaceY),
                frameWidth,
                frameHeight
            ));
        }

        return new Animation(sheet, frames, frameDuration, isLooping);
    }

    public Animation CreateFromCell(int row, int col, int totalFrames, float frameDuration, int spaceX = 0, int spaceY = 0, bool isLooping = true)
    {
        var frames = new List<Rectangle>();
        for (int i = 0; i < totalFrames; i++)
        {
            int x = (col + i) % columns;
            int y = row + (col + i) / columns;

            frames.Add(new Rectangle(
                x * (frameWidth + spaceX),
                y * (frameHeight + spaceY),
                frameWidth,
                frameHeight
            ));
        }

        return new Animation(sheet, frames, frameDuration, isLooping);
    }
}