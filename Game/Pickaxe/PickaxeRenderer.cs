using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Game.Example;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

/// <summary>
/// วาด debug visuals ของ IcePickaxe:
///   Charging → กล่องสีเหลืองขยายตาม ChargeLevel
///   Flying   → จุดสีฟ้าที่ตำแหน่ง projectile
///   Hooked   → เส้นเชือก + จุดสีแดงที่ hook point
///
/// ใช้ 1×1 pixel texture จาก ResourceManager ("pixel")
/// Phase 9: เปลี่ยนเป็น sprite จริง
/// </summary>
public class PickaxeRenderer : Component
{
    private Player     _player;
    private IcePickaxe _pickaxe;
    private Texture2D  _pixel;
    private Texture2D  _pickaxeTex;
    private Texture2D  _ropeTex;

    private const float LayerRope    = 0.80f;
    private const float LayerHook    = 0.81f;
    private const float LayerCharge  = 0.82f;

    public void Setup(Player player, IcePickaxe pickaxe)
    {
        _player  = player;
        _pickaxe = pickaxe;
    }

    public override void Initialize()
    {
        _pixel      = ResourceManager.Instance.GetTexture("pixel");
        _pickaxeTex = ResourceManager.Instance.GetTexture("Player/Pickaxe");
        _ropeTex    = ResourceManager.Instance.GetTexture("Player/Rope");
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_pixel == null || _player == null || _pickaxe == null) return;

        switch (_pickaxe.CurrentState)
        {
            case IcePickaxe.PickaxeStateKind.Charging:
                DrawChargeBar(spriteBatch, Color.Orange);
                break;

            case IcePickaxe.PickaxeStateKind.Flying:
                DrawRopeSegments(spriteBatch, _pickaxe.PickaxePosition);
                DrawPickaxeSprite(spriteBatch, _pickaxe.PickaxePosition, _pickaxe.FlyAngle);
                break;

            case IcePickaxe.PickaxeStateKind.Hooked:
            case IcePickaxe.PickaxeStateKind.Launching:
                DrawRopeSegments(spriteBatch, _pickaxe.HookPosition);
                DrawPickaxeSprite(spriteBatch, _pickaxe.HookPosition, _pickaxe.FlyAngle);
                break;

            case IcePickaxe.PickaxeStateKind.Recalling:
                DrawRopeSegments(spriteBatch, _pickaxe.PickaxePosition);
                DrawPickaxeSprite(spriteBatch, _pickaxe.PickaxePosition, _pickaxe.FlyAngle);
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // ── Charge Bar Constants ──────────────────────────────────────────────────
    private const float BarWidth     = 60f;
    private const float BarHeight    = 8f;
    private const float BarAboveHead = 80f;  // px เหนือ player center

    /// <summary>
    /// แถบชาร์จเหนือหัว Player — เต็มตาม ChargeLevel (0→1)
    /// สีขึ้นอยู่กับโหมด: Orange = ขว้าง, Cyan = พุ่งจาก rope
    /// </summary>
    private void DrawChargeBar(SpriteBatch sb, Color fillColor)
    {
        Vector2 center = _player.Position;
        float   barY   = center.Y - BarAboveHead;
        float   barX   = center.X - BarWidth / 2f;

        // background (เทา)
        DrawRect(sb, new Vector2(barX, barY), BarWidth, BarHeight,
                 new Color(40, 40, 40, 200), LayerCharge);

        // fill
        float fillW = BarWidth * _pickaxe.ChargeLevel;
        if (fillW > 0f)
            DrawRect(sb, new Vector2(barX, barY), fillW, BarHeight, fillColor, LayerCharge + 0.001f);

        // border
        float t = 1.5f;
        DrawLine(sb, new Vector2(barX,           barY),
                     new Vector2(barX + BarWidth, barY),            Color.White, t, LayerCharge + 0.002f);
        DrawLine(sb, new Vector2(barX,           barY + BarHeight),
                     new Vector2(barX + BarWidth, barY + BarHeight), Color.White, t, LayerCharge + 0.002f);
        DrawLine(sb, new Vector2(barX,           barY),
                     new Vector2(barX,           barY + BarHeight), Color.White, t, LayerCharge + 0.002f);
        DrawLine(sb, new Vector2(barX + BarWidth, barY),
                     new Vector2(barX + BarWidth, barY + BarHeight), Color.White, t, LayerCharge + 0.002f);
    }

    /// <summary>วาดเชือกผ่านทุก bend point จาก player ไปถึงปลาย (pickaxe/hook)</summary>
    private void DrawRopeSegments(SpriteBatch sb, Vector2 endPoint)
    {
        Vector2 from = _player.Position;
        for (int i = 0; i < _pickaxe.BendCount; i++)
        {
            Vector2 bend = _pickaxe.GetBendPoint(i);
            DrawRope(sb, from, bend);
            from = bend;
        }
        DrawRope(sb, from, endPoint);
    }

    private void DrawRope(SpriteBatch sb, Vector2 from, Vector2 to)
    {
        var tex = _ropeTex ?? _pixel;
        Vector2 edge  = to - from;
        float   len   = edge.Length();
        float   angle = (float)Math.Atan2(edge.Y, edge.X);

        // tile rope texture ตามความยาวเชือก
        float tileW    = tex.Width;
        float scaleY   = tex.Height > 0 ? 1f : 1f; // ความสูง 1:1
        int   tileCount = (int)Math.Ceiling(len / tileW);

        for (int i = 0; i < tileCount; i++)
        {
            float offset    = i * tileW;
            float remaining = Math.Min(tileW, len - offset);
            Vector2 pos     = from + Vector2.Normalize(edge) * offset;

            var srcRect = new Rectangle(0, 0, (int)remaining, tex.Height);
            sb.Draw(tex, pos, srcRect, Color.White,
                    angle,
                    new Vector2(0f, tex.Height / 2f),
                    Vector2.One,
                    SpriteEffects.None,
                    LayerRope);
        }
    }

    private void DrawPickaxeSprite(SpriteBatch sb, Vector2 position, float angle)
    {
        var tex = _pickaxeTex ?? _pixel;
        var origin = new Vector2(tex.Width / 2f, tex.Height / 2f);
        sb.Draw(tex, position, null, Color.White, angle, origin,
                2f, SpriteEffects.None, LayerHook);
    }

    private void DrawRect(SpriteBatch sb, Vector2 topLeft, float w, float h,
                          Color color, float layerDepth)
    {
        sb.Draw(_pixel, topLeft, null, color,
                0f, Vector2.Zero,
                new Vector2(w, h),
                SpriteEffects.None,
                layerDepth);
    }

    /// <summary>
    /// วาดเส้นตรงจาก <paramref name="from"/> ถึง <paramref name="to"/>
    /// โดยยืด 1×1 pixel ด้วย scale
    /// </summary>
    private void DrawLine(SpriteBatch sb, Vector2 from, Vector2 to,
                          Color color, float thickness, float layerDepth)
    {
        Vector2 edge  = to - from;
        float   angle = (float)Math.Atan2(edge.Y, edge.X);
        float   len   = edge.Length();

        sb.Draw(_pixel, from, null, color,
                angle,
                new Vector2(0f, 0.5f),            // origin: ซ้ายสุด, กึ่งกลาง Y
                new Vector2(len, thickness),
                SpriteEffects.None,
                layerDepth);
    }

    /// <summary>
    /// วาดจุด (สี่เหลี่ยมจัตุรัส) ที่ตำแหน่ง <paramref name="center"/>
    /// </summary>
    private void DrawDot(SpriteBatch sb, Vector2 center,
                         Color color, float size, float layerDepth)
    {
        sb.Draw(_pixel, center, null, color,
                0f,
                new Vector2(0.5f, 0.5f),          // origin: กลาง
                new Vector2(size, size),
                SpriteEffects.None,
                layerDepth);
    }
}
