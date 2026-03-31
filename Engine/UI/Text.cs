using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ComputerGameFinal.Engine.Components;

namespace ComputerGameFinal.Engine.UI;

public class Text : Component
{
    public SpriteFont Font { get; set; }
    public string Content { get; set; } = string.Empty;
    public Color Color { get; set; } = Color.White;
    public Vector2 Offset { get; set; } = Vector2.Zero;
    public Vector2 Origin { get; set; } = Vector2.Zero;
    public float LayerDepth { get; set; } = 1f;

    public Vector2 MeasureText()
    {
        if (Font == null) return Vector2.Zero;
        return Font.MeasureString(Content) * base.GameObject.Scale;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Font == null || string.IsNullOrEmpty(Content)) return;

        spriteBatch.DrawString(
            Font,
            Content,
            base.GameObject.Position + Offset,
            Color,
            base.GameObject.Rotation.Z,
            Origin,
            base.GameObject.Scale,
            SpriteEffects.None,
            LayerDepth
        );
    }
}