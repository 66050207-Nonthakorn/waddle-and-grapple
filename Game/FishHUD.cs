using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game;

/// <summary>
/// แสดงจำนวนปลาที่เก็บได้ มุมบนซ้ายของหน้าจอ
/// Attach กับ Player → วาดใน world-space แต่ตำแหน่ง convert จาก screen-space
/// เพื่อให้ติดมุมจอแม้ camera เลื่อน
/// </summary>
public class FishHUD : Component
{
    private Player     _player;
    private Texture2D  _fishSheet;
    private SpriteFont _font;

    // ระยะห่างจากมุมบนซ้าย (screen pixels)
    private const float PadLeft = 16f;
    private const float PadTop  = 14f;

    // icon: ใช้ frame แรกของ Fish spritesheet (32×32) scale ลงเป็น IconSize
    private const int IconSize = 36;
    private static readonly Rectangle FishFrame = new(0, 0, 32, 32);

    private static readonly Color TextColor   = Color.White;
    private static readonly Color ShadowColor = new(0, 0, 0, 180);

    public override void Initialize()
    {
        _player    = (Player)GameObject;
        _fishSheet = ResourceManager.Instance.GetTexture("Collectibles/Fish");
        _font      = ResourceManager.Instance.GetFont("Fonts/36Font");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_font == null || _fishSheet == null) return;

        var camera = SceneManager.Instance.CurrentScene?.Camera;
        if (camera == null) return;

        float depth = 0.99f;
        float scale = (float)IconSize / FishFrame.Width;

        // ── Fish icon ──────────────────────────────────────────────────────────
        Vector2 iconWorld = camera.ScreenToWorld(new Vector2(PadLeft, PadTop));
        spriteBatch.Draw(_fishSheet,
            iconWorld, FishFrame,
            Color.White, 0f, Vector2.Zero, scale,
            SpriteEffects.None, depth);

        // ── "x N" ทางขวาของ icon ──────────────────────────────────────────────
        string  text     = $"x {_player.FishCount}";
        float   textOffY = (IconSize - _font.LineSpacing) / 2f;
        Vector2 textWorld = camera.ScreenToWorld(new Vector2(PadLeft + IconSize + 4f, PadTop + textOffY));

        // shadow
        spriteBatch.DrawString(_font, text, textWorld + new Vector2(1, 1),
            ShadowColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth - 0.001f);
        // ข้อความหลัก
        spriteBatch.DrawString(_font, text, textWorld,
            TextColor, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
    }
}
