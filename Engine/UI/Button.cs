using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;

namespace ComputerGameFinal.Engine.UI;

public class Button : Component
{
    Texture2D _dummyTexture;
    public Vector2 Size { get; set; } = new Vector2(200, 50);
    public Action OnClick { get; set; }

    public Color FillColor { get; set; } = Color.DarkGray;
    public Color OutlineColor { get; set; } = Color.Red;
    public int OutlineThickness { get; set; } = 1;
    public bool IsShowOutline { get; set; } = false;
    public bool IsShowFill { get; set; } = true;

    public override void Update(GameTime gameTime)
    {
        var mousePosition = InputManager.Instance.GetMousePosition();
        var buttonRectangle = new Rectangle((int)base.GameObject.Position.X, (int)base.GameObject.Position.Y, (int)Size.X, (int)Size.Y);

        if (buttonRectangle.Contains(mousePosition))
        {
            if (InputManager.Instance.IsMouseButtonDown(0))
            {            
                OnClick?.Invoke();
            }
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_dummyTexture == null)
        {
            _dummyTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _dummyTexture.SetData(new[] { Color.White });
        }

        if (IsShowFill)
        {
            spriteBatch.Draw(
                _dummyTexture,
                new Rectangle((int)GameObject.Position.X, (int)GameObject.Position.Y, (int)Size.X, (int)Size.Y),
                FillColor
            );
        }

        if (!IsShowOutline) return;

        // Top outline
        spriteBatch.Draw(
            _dummyTexture,
            new Rectangle(
                (int)GameObject.Position.X - OutlineThickness,
                (int)GameObject.Position.Y - OutlineThickness,
                (int)Size.X + OutlineThickness * 2,
                OutlineThickness
            ),
            OutlineColor
        );

        // Bottom outline
        spriteBatch.Draw(
            _dummyTexture,
            new Rectangle(
                (int)GameObject.Position.X - OutlineThickness,
                (int)(GameObject.Position.Y + Size.Y),
                (int)Size.X + OutlineThickness * 2,
                OutlineThickness
            ),
            OutlineColor
        );

        // Left outline
        spriteBatch.Draw(
            _dummyTexture,
            new Rectangle(
                (int)GameObject.Position.X - OutlineThickness,
                (int)GameObject.Position.Y - OutlineThickness,
                OutlineThickness,
                (int)Size.Y + OutlineThickness * 2
            ),
            OutlineColor
        );

        // Right outline
        spriteBatch.Draw(
            _dummyTexture,
            new Rectangle(
                (int)(GameObject.Position.X + Size.X),
                (int)GameObject.Position.Y - OutlineThickness,
                OutlineThickness,
                (int)Size.Y + OutlineThickness * 2
            ),
            OutlineColor
        );
    }
}