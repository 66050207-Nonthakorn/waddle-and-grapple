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

    /// <summary>เรียกเมื่อ overlay fade in เสร็จ — ให้ level เรียก CompleteLevel() ตรงนี้</summary>
    public Action OnComplete { get; set; }

    private GoalFlagRenderer _renderer;

    public override void Initialize()
    {
        _renderer = AddComponent<GoalFlagRenderer>();
    }

    public override void Update(GameTime gameTime)
    {
        _renderer?.Tick(gameTime, Player, Position, OnComplete);
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

    private bool _reached       = false;
    private bool _completeFired = false;

    private static readonly Color PoleColor = new(160, 130, 80);
    private static readonly Color FlagColor = new(220, 40,  40);

    public override void Initialize()
    {
        _pixel       = ResourceManager.Instance.GetTexture("pixel");
        _triangleTex = CreateTriangleTexture(FlagW, FlagH);
    }

    // เรียกจาก GoalFlag.Update() ทุก frame
    public void Tick(GameTime gameTime, Player player, Vector2 pos, Action onComplete)
    {
        if (_reached)
        {
            // รอ goal animation จบแล้วเรียก CompleteLevel() ทันที
            if (!_completeFired && player != null && player.IsGoalAnimationComplete)
            {
                _completeFired = true;
                onComplete?.Invoke();
            }
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
