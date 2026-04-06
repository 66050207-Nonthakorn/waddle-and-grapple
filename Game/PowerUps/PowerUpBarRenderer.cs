using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game;

/// <summary>
/// วาด timer bar เหนือหัว player ทุก effect ที่ active อยู่
/// Attach ใน Player.Initialize() → AddComponent&lt;PowerUpBarRenderer&gt;()
/// </summary>
public class PowerUpBarRenderer : Component
{
    private Player      _player;
    private Texture2D   _pixel;

    private const int   BarW       = 48;   // ความกว้างเต็ม (px)
    private const int   BarH       = 6;    // ความสูง bar
    private const int   BarGap     = 10;   // ระยะห่างระหว่าง bar แต่ละอัน
    private const int   OffsetY    = 48;   // สูงจาก center player ขึ้นไป
    private const float LayerDepth = 0.95f;

    private static readonly Color BgColor = new(20, 20, 20, 200);

    public override void Initialize()
    {
        _player = (Player)GameObject;
        _pixel  = ResourceManager.Instance.GetTexture("pixel");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var effects = _player.ActiveEffects;
        int drawn   = 0;

        foreach (var fx in effects)
        {
            if (!fx.IsActive) continue;

            float ratio  = fx.GaugeRatio;
            float barY   = _player.Position.Y - OffsetY - drawn * BarGap;
            float barX   = _player.Position.X - BarW / 2f;

            // พื้นหลังสีเข้ม
            spriteBatch.Draw(_pixel,
                new Rectangle((int)barX, (int)barY, BarW, BarH),
                null, BgColor, 0f, Vector2.Zero, SpriteEffects.None, LayerDepth);

            // foreground — เหลือเวลาเท่าไร
            int filled = (int)(BarW * ratio);
            if (filled > 0)
                spriteBatch.Draw(_pixel,
                    new Rectangle((int)barX, (int)barY, filled, BarH),
                    null, fx.ItemColor, 0f, Vector2.Zero, SpriteEffects.None, LayerDepth);

            drawn++;
        }
    }
}
