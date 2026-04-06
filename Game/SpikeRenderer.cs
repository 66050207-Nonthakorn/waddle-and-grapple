using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Renders a spike using tile-based drawing.
/// Sprite layout (48x16): tiles are arranged horizontally side-by-side.
///   tile 0 (x=0)  = left edge
///   tile 1 (x=16) = middle part
///   tile 2 (x=32) = right edge
///
/// Spikes extend outwards along the normal based on ExtensionRatio.
///
/// Position = base contact point (where spike meets the surface).
/// RotationAngle: 0=up, PI/2=right, PI=down, -PI/2=left.
/// </summary>
public class SpikeRenderer : Component
{
    public  const int TileSize   = 16;   // game-world display size per tile (px)
    private const int SpriteTile = 16;   // sprite sheet pixel size per tile
    private const float LayerDepth = 0.6f;

    private Texture2D _pixel;
    private Texture2D _sheet;
    private string    _loadedTextureName;
    private SpikeTrap _spike;

    public override void Initialize()
    {
        _pixel = ResourceManager.Instance.GetTexture("pixel");
        _spike = GameObject as SpikeTrap;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_spike == null) return;

        // Lazy-reload texture if SpriteTextureName changed after Initialize
        if (_loadedTextureName != _spike.SpriteTextureName)
        {
            _loadedTextureName = _spike.SpriteTextureName;
            _sheet = string.IsNullOrEmpty(_loadedTextureName)
                ? null
                : ResourceManager.Instance.GetTexture(_loadedTextureName);
        }

        float ext = _spike.ExtensionRatio;
        if (ext <= 0f) return;

        int n = _spike.SpikeTiles;
        if (n <= 0) return;

        bool hasSheet = _sheet != null
                        && _sheet.Width  >= SpriteTile * 3
                        && _sheet.Height >= SpriteTile;

        if (!hasSheet)
        {
            DrawFallback(spriteBatch, n, ext);
            return;
        }

        int drawHeight = (int)Math.Round(SpriteTile * ext);
        if (drawHeight <= 0) return;

        // ครอปความสูงจากด้านบน (y=0) เพื่อให้ปลายโผล่ขึ้นมาก่อนตาม ExtensionRatio
        var leftSrc  = new Rectangle(0,              0, SpriteTile, drawHeight);
        var midSrc   = new Rectangle(SpriteTile,     0, SpriteTile, drawHeight);
        var rightSrc = new Rectangle(SpriteTile * 2, 0, SpriteTile, drawHeight);

        float rot  = _spike.RotationAngle;
        // Direction ขนานไปกับแนวนอนของพื้น
        var rightDir = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
        Color tint = _spike.SpriteTint;

        // จุด Base ของแต่ละ Tile จัดเรียงขนานไปกับพื้น
        Vector2 BasePos(int i) => _spike.Position + rightDir * (i * TileSize);

        if (n == 1)
        {
            DrawTile(spriteBatch, midSrc, BasePos(0), rot, tint);
        }
        else if (n == 2)
        {
            DrawTile(spriteBatch, leftSrc,  BasePos(0), rot, tint);
            DrawTile(spriteBatch, rightSrc, BasePos(1), rot, tint);
        }
        else
        {
            DrawTile(spriteBatch, leftSrc, BasePos(0), rot, tint);
            for (int i = 1; i < n - 1; i++)
                DrawTile(spriteBatch, midSrc, BasePos(i), rot, tint);
            DrawTile(spriteBatch, rightSrc, BasePos(n - 1), rot, tint);
        }
    }

    private void DrawTile(SpriteBatch sb, Rectangle src, Vector2 basePos,
                          float rotation, Color tint)
    {
        // ตั้งจุดอ้างอิงตอนวาดให้อยู่ตรงกลางด้านล่างของส่วนที่ครอปมา
        var   srcOrigin = new Vector2(src.Width * 0.5f, src.Height);
        float scale     = (float)TileSize / SpriteTile;
        sb.Draw(_sheet, basePos, src, tint, rotation, srcOrigin,
                scale, SpriteEffects.None, LayerDepth);
    }

    private void DrawFallback(SpriteBatch sb, int n, float ext)
    {
        if (_pixel == null) return;
        float rot = _spike.RotationAngle;
        var rightDir = new Vector2(MathF.Cos(rot), MathF.Sin(rot));
        var upDir    = new Vector2(MathF.Sin(rot), -MathF.Cos(rot));

        float drawHeight = TileSize * ext;

        for (int i = 0; i < n; i++)
        {
            var basePos = _spike.Position + rightDir * (i * TileSize);
            var center  = basePos + upDir * (drawHeight * 0.5f);

            sb.Draw(_pixel, center, null, Color.Green, rot,
                    new Vector2(0.5f, 0.5f), new Vector2(TileSize, drawHeight),
                    SpriteEffects.None, LayerDepth);
        }
    }
}
