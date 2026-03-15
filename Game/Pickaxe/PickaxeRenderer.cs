using System;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
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
                DrawChargeIndicator(spriteBatch);
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

    /// <summary>
    /// กล่องสีเหลืองรอบ Player ขยายจาก 8px → 30px ตาม ChargeLevel
    /// </summary>
    private void DrawChargeIndicator(SpriteBatch sb)
    {
        float size      = 8f + _pickaxe.ChargeLevel * 22f;
        var   center    = _player.Position;
        var   color     = Color.Orange;
        float thickness = 2f;

        // top
        DrawLine(sb, new Vector2(center.X - size, center.Y - size),
                     new Vector2(center.X + size, center.Y - size), color, thickness, LayerCharge);
        // bottom
        DrawLine(sb, new Vector2(center.X - size, center.Y + size),
                     new Vector2(center.X + size, center.Y + size), color, thickness, LayerCharge);
        // left
        DrawLine(sb, new Vector2(center.X - size, center.Y - size),
                     new Vector2(center.X - size, center.Y + size), color, thickness, LayerCharge);
        // right
        DrawLine(sb, new Vector2(center.X + size, center.Y - size),
                     new Vector2(center.X + size, center.Y + size), color, thickness, LayerCharge);
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
