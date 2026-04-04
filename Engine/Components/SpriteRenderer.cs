using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.Components;

public class SpriteRenderer : Component
{
    // X rotation: tilts the sprite top/bottom (scales Y axis by cos(rotX), flips past 90°)
    // Y rotation: tilts the sprite left/right  (scales X axis by cos(rotY), flips past 90°)
    // Z rotation: standard 2D rotation angle

    public Texture2D Texture
    {
        get { return _texture; }
        set
        {
            _texture = value;
            Origin = new Vector2(
                _texture.Bounds.Center.X,
                _texture.Bounds.Center.Y
            );
        }
    }
    private Texture2D _texture;

    public Color Tint { get; set; } = Color.White;
    public Vector2 Origin { get; set; } = Vector2.Zero;
    public float LayerDepth { get; set; } = 0;
    /// <summary>เลื่อน sprite ในพิกเซลโดยไม่กระทบ Position/Collider (ใช้แก้ offset ของ sprite art)</summary>
    public Vector2 DrawOffset { get; set; } = Vector2.Zero;

    public Rectangle? SourceRectangle { get; set; } = null;

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Texture == null) return;

        Vector3 rotation = base.GameObject.Rotation;

        float cosX = (float)Math.Cos(rotation.X);
        float cosY = (float)Math.Cos(rotation.Y);

        Vector2 scale = base.GameObject.Scale * new Vector2(Math.Abs(cosY), Math.Abs(cosX));

        SpriteEffects effects = SpriteEffects.None;
        if (cosY < 0) effects |= SpriteEffects.FlipHorizontally;
        if (cosX < 0) effects |= SpriteEffects.FlipVertically;

        spriteBatch.Draw(Texture, base.GameObject.Position + DrawOffset, SourceRectangle, Tint,
            rotation.Z, Origin, scale, effects, LayerDepth);
    }
}