using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game;

/// <summary>
/// ธงปลายด่าน — เสาสูง + สามเหลี่ยมแดง
/// เมื่อ player แตะ trigger zone → fade in congratulations overlay
///
/// วิธีใช้:
///   var goal = scene.AddGameObject&lt;GoalFlag&gt;("goal");
///   goal.Position = new Vector2(2380, 450);
///   goal.Player   = player;
/// </summary>
public class GoalFlag : GameObject
{
    public Player Player { get; set; }

    private GoalFlagRenderer _renderer;

    public override void Initialize()
    {
        _renderer = AddComponent<GoalFlagRenderer>();
    }

    public override void Update(GameTime gameTime)
    {
        _renderer?.Tick(gameTime, Player, Position);
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class GoalFlagRenderer : Component
{
    // ขนาด
    private const int TriggerW = 60;
    private const int TriggerH = 120;
    private const int PoleW    = 8;
    private const int PoleH    = 110;
    private const int FlagW    = 44;
    private const int FlagH    = 36;

    private Texture2D  _pixel;
    private Texture2D  _triangleTex;
    private SpriteFont _font;

    private bool  _reached    = false;
    private float _overlayAlpha = 0f;

    private static readonly Color PoleColor = new(160, 130, 80);
    private static readonly Color FlagColor = new(220, 40,  40);
    private static readonly Color WinColor  = new(255, 220, 60);

    public override void Initialize()
    {
        _pixel        = ResourceManager.Instance.GetTexture("pixel");
        _font         = ResourceManager.Instance.GetFont("Fonts/File");
        _triangleTex  = CreateTriangleTexture(FlagW, FlagH);
    }

    // เรียกจาก GoalFlag.Update() ทุก frame
    public void Tick(GameTime gameTime, Player player, Vector2 pos)
    {
        if (_reached)
        {
            // รอ player เล่น goal animation ครบ 3 รอบ แล้วค่อย freeze + fade in overlay
            if (!WorldTime.IsFrozen && player != null && player.IsGoalAnimationComplete)
                WorldTime.Freeze();

            if (WorldTime.IsFrozen)
                _overlayAlpha = Math.Min(1f, _overlayAlpha + (float)gameTime.ElapsedGameTime.TotalSeconds * 2f);
            return;
        }

        if (player == null) return;

        var trigger = new Rectangle(
            (int)(pos.X - TriggerW / 2f),
            (int)(pos.Y - TriggerH),
            TriggerW, TriggerH);

        if (trigger.Intersects(player.ColliderBounds))
        {
            _reached = true;
            player.TriggerGoalReached();
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Vector2 pos = GameObject.Position;

        // ── เสา ──────────────────────────────────────────────────────────────
        spriteBatch.Draw(_pixel,
            new Rectangle((int)pos.X - PoleW / 2, (int)pos.Y - PoleH, PoleW, PoleH),
            null, PoleColor, 0f, Vector2.Zero, SpriteEffects.None, 0.12f);

        // ── สามเหลี่ยม (ชี้ขวา ปลายเสาด้านบน) ───────────────────────────────
        spriteBatch.Draw(_triangleTex,
            new Vector2(pos.X + PoleW / 2f, pos.Y - PoleH),
            null, FlagColor, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0.13f);

        // ── Overlay ───────────────────────────────────────────────────────────
        if (!_reached || _overlayAlpha <= 0f || _font == null) return;

        var camera = SceneManager.Instance.CurrentScene?.Camera;
        if (camera == null) return;

        var player = (GameObject as GoalFlag)?.Player;

        int sw = ScreenManager.Instance.nativeWidth;
        int sh = ScreenManager.Instance.nativeHeight;

        Vector2 topLeft = camera.ScreenToWorld(Vector2.Zero);
        Vector2 center  = camera.ScreenToWorld(new Vector2(sw / 2f, sh / 2f));

        byte a   = (byte)(255 * _overlayAlpha);
        Color bg = new Color(0, 0, 0) * (0.7f * _overlayAlpha);

        // พื้นหลัง
        spriteBatch.Draw(_pixel,
            new Rectangle((int)topLeft.X, (int)topLeft.Y, sw, sh),
            null, bg, 0f, Vector2.Zero, SpriteEffects.None, 0.97f);

        // "GOAL!" ตัวใหญ่
        DrawText(spriteBatch, "GOAL!", center,
            WinColor * _overlayAlpha,
            scale: 3f, offsetY: -110f, depth: 0.986f);

        // ข้อความรอง
        DrawText(spriteBatch, "Congratulations! You cleared the stage!",
            center, Color.White * _overlayAlpha,
            scale: 1f, offsetY: 30f, depth: 0.986f);

        if (player != null)
            DrawText(spriteBatch, $"Coins collected: {player.CoinCount}",
                center, new Color(255, 220, 100) * _overlayAlpha,
                scale: 1f, offsetY: 80f, depth: 0.986f);
    }

    private void DrawText(SpriteBatch sb, string text, Vector2 center,
        Color color, float scale, float offsetY, float depth)
    {
        Vector2 size = _font.MeasureString(text) * scale;
        Vector2 pos  = center + new Vector2(-size.X / 2f, offsetY);

        // shadow
        sb.DrawString(_font, text, pos + new Vector2(2, 2),
            Color.Black * (color.A / 255f), 0f, Vector2.Zero, scale,
            SpriteEffects.None, depth - 0.001f);
        // main
        sb.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale,
            SpriteEffects.None, depth);
    }

    private static Texture2D CreateTriangleTexture(int w, int h)
    {
        var gd   = ScreenManager.Instance._graphics.GraphicsDevice;
        var tex  = new Texture2D(gd, w, h);
        var data = new Color[w * h];

        for (int py = 0; py < h; py++)
        {
            float dy   = Math.Abs(py - h / 2f) / (h / 2f);
            int   maxX = (int)((1f - dy) * w);
            for (int px = 0; px < w; px++)
                data[py * w + px] = px < maxX ? Color.White : Color.Transparent;
        }

        tex.SetData(data);
        return tex;
    }
}
