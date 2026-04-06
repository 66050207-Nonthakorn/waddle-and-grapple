using System.Collections.Generic;
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

    /// <summary>ชื่อ texture ใน ResourceManager (spritesheet 8 frames, 16×16 px/frame)</summary>
    protected abstract string SpriteName { get; }

    /// <summary>ระยะเวลา effect รวม (วินาที)</summary>
    protected float Duration;
    public float TotalDuration  => Duration;
    public float RemainingTime  { get; private set; }

    // ── Collectible pickup ────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize(); // ตั้ง collider

        Scale = new Vector2(2f, 2f);

        var sheet  = ResourceManager.Instance.GetTexture(SpriteName);
        const int FrameSize = 16;
        const int FrameCount = 8;
        var frames = new List<Microsoft.Xna.Framework.Rectangle>();
        for (int i = 0; i < FrameCount; i++)
            frames.Add(new Microsoft.Xna.Framework.Rectangle(i * FrameSize, 0, FrameSize, FrameSize));

        var anim     = new Animation(sheet, frames, frameDuration: 0.1f, isLooping: true);
        var animator = AddComponent<Animator>();
        animator.AddAnimation("idle", anim);
        animator.Play("idle");

        // SpriteRenderer is created by Animator.Initialize — set layer depth
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.LayerDepth = 0.5f;
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

    public virtual void UpdateEffect(Player player, float dt)
    {
        if (!IsActive) return;
        if (Duration <= 0f) return;

        RemainingTime -= dt;
        if (RemainingTime <= 0f)
            Deactivate(player);
    }

    /// <summary>อัตราส่วนเกจที่แสดงบนหัว (0–1). ค่าปกติคำนวณจาก RemainingTime/TotalDuration</summary>
    public virtual float GaugeRatio => Duration > 0f
        ? System.Math.Clamp(RemainingTime / Duration, 0f, 1f)
        : (IsActive ? 1f : 0f);

    public void Deactivate(Player player)
    {
        if (!IsActive) return;
        IsActive = false;
        OnDeactivate(player);
    }

    protected abstract void OnActivate(Player player);
    protected abstract void OnDeactivate(Player player);
}
