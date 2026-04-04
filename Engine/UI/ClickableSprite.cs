using WaddleAndGrapple.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class ClickableSprite : Button
{
    public Texture2D Texture { get; set; }
    public Color Tint { get; set; } = Color.White;
    public bool UseTextureSizeForHitbox { get; set; } = true;

    public ClickableSprite()
    {

    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture != null)
        {
            if (UseTextureSizeForHitbox)
            {
                Size = new Vector2(Texture.Width, Texture.Height);
            }

            spriteBatch.Draw(
                Texture,
                new Rectangle((int)GameObject.Position.X, (int)GameObject.Position.Y, (int)Size.X, (int)Size.Y),
                Tint
            );
        }
    }
}