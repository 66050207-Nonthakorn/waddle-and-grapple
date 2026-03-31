using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace ComputerGameFinal.Engine.UI;

public class TextButton : GameObject
{
    public Button button;
    public Text text;

    public TextButton()
    {
        button = AddComponent<Button>();
        text = AddComponent<Text>();
    }

    // Property accessors for easy customization
    public Vector2 Size
    {
        get => button.Size;
        set => button.Size = value;
    }

    public Color FillColor
    {
        get => button.FillColor;
        set => button.FillColor = value;
    }

    public bool IsShowFill
    {
        get => button.IsShowFill;
        set => button.IsShowFill = value;
    }

    public Color OutlineColor
    {
        get => button.OutlineColor;
        set => button.OutlineColor = value;
    }

    public int OutlineThickness
    {
        get => button.OutlineThickness;
        set => button.OutlineThickness = value;
    }

    public bool IsShowOutline
    {
        get => button.IsShowOutline;
        set => button.IsShowOutline = value;
    }

    public Color TextColor
    {
        get => text.Color;
        set => text.Color = value;
    }

    public string Text
    {
        get => text.Content;
        set
        {
            text.Content = value;
            // Re-center text when content changes
            var textSize = text.MeasureText();
            text.Origin = textSize / 2;
        }
    }

    public SpriteFont font
    {
        get => text.Font;
        set => text.Font = value;
    }

    public System.Action OnClick
    {
        get => button.OnClick;
        set => button.OnClick = value;
    }

    public Vector2 TextOffset
    {
        get => text.Offset;
        set => text.Offset = value;
    }
}
