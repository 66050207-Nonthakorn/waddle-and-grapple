using System;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game.Example;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Game;

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
        _pixel = ResourceManager.Instance.GetTexture("pixel");
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
                DrawDot(spriteBatch, _pickaxe.PickaxePosition, Color.Blue, 8f, LayerHook);
                break;

            case IcePickaxe.PickaxeStateKind.Hooked:
                DrawLine(spriteBatch, _player.Position, _pickaxe.HookPosition, Color.Black, 2f, LayerRope);
                DrawDot(spriteBatch, _pickaxe.HookPosition, Color.Red, 10f, LayerHook);
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
