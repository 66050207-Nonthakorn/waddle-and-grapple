using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Game.Example;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Base class สำหรับ PowerUp ทุกตัว
///
/// ทำงาน 2 บทบาท:
///   1. Collectible — วางอยู่ใน world, Player วิ่งผ่านเพื่อเก็บ → OnCollect → ApplyEffect
///   2. Active effect — Player เก็บแล้ว effect ยัง run อยู่ผ่าน UpdateEffect()
///
/// Duration:
///   > 0  → effect หมดเมื่อครบเวลา (SpeedBoost, SlowTime)
///   = 0  → one-time use ไม่มีเวลาหมด (DoubleJump)
/// </summary>
public abstract class PowerUp : Collectible
{
    public bool IsActive { get; private set; }

    /// <summary>สีของ item บน map และ HUD bar</summary>
    public virtual Color ItemColor => Color.Magenta;

    /// <summary>ระยะเวลา effect รวม (วินาที)</summary>
    protected float Duration;
    public float TotalDuration  => Duration;
    public float RemainingTime  { get; private set; }

    // ── Collectible pickup ────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize(); // ตั้ง collider

        // pixel texture ขนาด 1×1 → ยืดด้วย Scale ให้เป็น 32×32 px
        // TODO (Phase 9): ใส่ sprite จริง
        Scale      = new Vector2(32, 32);
        var sr     = AddComponent<SpriteRenderer>();
        sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        sr.Tint       = ItemColor;
        sr.LayerDepth = 0.5f;
    }

    /// <summary>เมื่อเก็บ → ส่งต่อ effect ให้ Player จัดการ</summary>
    public override void OnCollect(Player player)
    {
        player.ApplyEffect(this);
    }

    // ── Effect lifecycle (เรียกโดย Player._activeEffects) ────────────────────

    public void Activate(Player player)
    {
        IsActive      = true;
        RemainingTime = Duration;
        OnActivate(player);
    }

    public void UpdateEffect(Player player, float dt)
    {
        if (!IsActive) return;
        if (Duration <= 0f) return;

        RemainingTime -= dt;
        if (RemainingTime <= 0f)
            Deactivate(player);
    }

    public void Deactivate(Player player)
    {
        if (!IsActive) return;
        IsActive = false;
        OnDeactivate(player);
    }

    protected abstract void OnActivate(Player player);
    protected abstract void OnDeactivate(Player player);
}
