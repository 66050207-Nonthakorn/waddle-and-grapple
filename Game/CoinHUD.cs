using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game;

/// <summary>
/// แสดงจำนวน coin มุมบนขวาของหน้าจอ
/// Attach กับ Player → วาดใน world-space แต่ตำแหน่ง convert จาก screen-space
/// เพื่อให้ติดมุมจอแม้ camera เลื่อน
/// </summary>
public class CoinHUD : Component
{
    private Player     _player;
    private Texture2D  _pixel;
    private SpriteFont _font;

    // ระยะห่างจากมุมบนขวา (screen pixels)
    private const float PadRight = 16f;
    private const float PadTop   = 14f;

    // ขนาด icon coin
    private const int IconSize = 14;

    private static readonly Color IconColor = Color.Gold;
    private static readonly Color TextColor = Color.White;
    private static readonly Color ShadowColor = new(0, 0, 0, 180);

    public override void Initialize()
    {
        _player = (Player)GameObject;
        _pixel  = ResourceManager.Instance.GetTexture("pixel");
        _font   = ResourceManager.Instance.GetFont("Fonts/File");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_font == null) return;

        var camera  = SceneManager.Instance.CurrentScene?.Camera;
        if (camera == null) return;

        float screenW = ScreenManager.Instance.nativeWidth;

        string text   = $"x {_player.CoinCount}";
        Vector2 textSize = _font.MeasureString(text);

        // คำนวณตำแหน่ง screen-space แล้ว convert → world-space
        float screenX = screenW - PadRight - textSize.X - IconSize - 6f;
        float screenY = PadTop;

        Vector2 worldPos = camera.ScreenToWorld(new Vector2(screenX, screenY));

        float depth = 0.99f;

        // shadow ข้อความ
        DrawAt(spriteBatch, text, worldPos + new Vector2(1, 1), ShadowColor, depth - 0.001f);
        // ข้อความหลัก
        DrawAt(spriteBatch, text, worldPos, TextColor, depth);

        // icon สี่เหลี่ยมสีทอง (coin placeholder)
        Vector2 iconWorld = camera.ScreenToWorld(
            new Vector2(screenW - PadRight - textSize.X - IconSize - 4f, screenY + 2f));
        spriteBatch.Draw(_pixel,
            new Rectangle((int)iconWorld.X, (int)iconWorld.Y, IconSize, IconSize),
            null, IconColor, 0f, Vector2.Zero, SpriteEffects.None, depth);
    }

    private void DrawAt(SpriteBatch sb, string text, Vector2 pos, Color color, float depth)
    {
        sb.DrawString(_font, text, pos, color,
            0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }
}
