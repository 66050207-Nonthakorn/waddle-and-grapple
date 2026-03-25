using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game.Example;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

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

    /// <summary>ระยะเวลา effect (วินาที); 0 = one-time</summary>
    protected float Duration;

    private float _remainingTime;

    // ── Collectible pickup ────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize(); // ตั้ง collider

        // pixel texture ขนาด 1×1 → ยืดด้วย Scale ให้เป็น 32×32 px
        // TODO (Phase 9): ใส่ sprite จริง
        Scale      = new Vector2(32, 32);
        var sr     = AddComponent<SpriteRenderer>();
        sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        sr.Tint       = Color.Magenta;
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
        IsActive       = true;
        _remainingTime = Duration;
        OnActivate(player);
    }

    public void UpdateEffect(Player player, float dt)
    {
        if (!IsActive) return;
        if (Duration <= 0f) return; // one-time use — deactivate จากภายนอก

        _remainingTime -= dt;
        if (_remainingTime <= 0f)
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
