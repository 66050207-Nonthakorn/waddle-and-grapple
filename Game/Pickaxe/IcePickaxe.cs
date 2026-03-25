using System;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Game.Example;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ComputerGameFinal.Game;

/// <summary>
/// Ice Pickaxe — อาวุธและเครื่องมือเดินทางหลักของ Agen-T
///
/// Flow:
///   Right Click (ค้าง) → Charging → Right Click (ปล่อย) → Flying
///   → ชน HookPoint → Hooked → Swing / Climb
///   → E หรือ Space หรือ Auto → Recall → Idle
/// </summary>
public class IcePickaxe
{
    // ── Constants ─────────────────────────────────────────────────────────────
    private const float MaxChargeSec   = 1.5f;  // วินาทีชาร์จเต็ม
    private const float MaxRange       = 400f;  // ระยะขว้างสูงสุด (px)
    private const float MinThrowRange  = 60f;   // ระยะขั้นต่ำแม้ไม่ชาร์จ
    private const float FlySpeed       = 900f;  // ความเร็ว projectile (px/s)
    private const float ClimbSpeed     = 150f;  // ความเร็วไต่เชือก (px/s)
    private const float MinRopeLength  = 20f;   // ความยาวเชือกขั้นต่ำ (px)
    private const float AutoRecallSec  = 3f;    // auto-recall หลังกี่วินาที
    private const float LaunchSpeed    = 900f;  // ความเร็วพุ่งจาก rope

    // ── Owner ─────────────────────────────────────────────────────────────────
    private readonly Player _owner;

    // ── Public State ──────────────────────────────────────────────────────────
    public bool  IsDeployed  { get; private set; }
    public bool  IsHooked    { get; private set; }
    public float ChargeLevel { get; private set; } // 0.0 – 1.0

    // ── Public Position (อ่านได้จาก PickaxeRenderer) ─────────────────────────
    public Vector2 PickaxePosition => _position;
    public Vector2 HookPosition    => _hookPosition;
    public PickaxeStateKind CurrentState => _state switch
    {
        PickaxeState.Charging => PickaxeStateKind.Charging,
        PickaxeState.Flying   => PickaxeStateKind.Flying,
        PickaxeState.Hooked   => PickaxeStateKind.Hooked,
        _                     => PickaxeStateKind.Idle,
    };

    public enum PickaxeStateKind { Idle, Charging, Flying, Hooked }

    // ── Internal State Machine ────────────────────────────────────────────────
    private enum PickaxeState { Idle, Charging, Flying, Hooked }
    private PickaxeState _state = PickaxeState.Idle;

    // Projectile (ขณะบิน)
    private Vector2 _position;        // ตำแหน่งของ pickaxe ในโลก
    private Vector2 _flyVelocity;     // velocity ของ projectile
    private float   _flownDistance;   // ระยะที่บินไปแล้ว
    private float   _maxFlyDistance;  // ระยะสูงสุดตาม charge

    // Hook (ขณะ hooked)
    private Vector2 _hookPosition;    // จุดที่ hook อยู่บน map
    private float   _ropeLength;      // ความยาวเชือกปัจจุบัน (ไต่ได้)
    private float   _autoRecallTimer;

    // TODO (Member 3): private IEnemy _hookedEnemy;

    // ─────────────────────────────────────────────────────────────────────────

    public IcePickaxe(Player owner)
    {
        _owner = owner;
    }

    public void Initialize()
    {
        // TODO (Phase 9): โหลด sprite สำหรับ pickaxe, rope, charge indicator
    }

    // ══════════════════════════════════════════════════════════════════════════
    // เรียกจาก Player.Update() ก่อน ApplyGravity
    // จัดการ input + flight update + climb
    // ══════════════════════════════════════════════════════════════════════════
    public void HandleInput(float dt)
    {
        switch (_state)
        {
            case PickaxeState.Idle:
            case PickaxeState.Charging:
                HandleChargeAndThrow(dt);
                break;

            case PickaxeState.Flying:
                UpdateFlight(dt);
                if (InputManager.Instance.IsKeyPressed(Keys.E))
                    Recall();
                break;

            case PickaxeState.Hooked:
                HandleHookedInput(dt);
                _autoRecallTimer -= dt;
                if (_autoRecallTimer <= 0f) Recall();
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // เรียกจาก Player.Update() หลัง MoveAndCollide
    // บังคับ rope constraint: ถ้า Player ห่างจาก hookPoint เกิน ropeLength → ดึงกลับ
    // ══════════════════════════════════════════════════════════════════════════
    public void ApplyConstraint()
    {
        if (_state != PickaxeState.Hooked) return;

        Vector2 toPlayer = _owner.Position - _hookPosition;
        float   dist     = toPlayer.Length();

        if (dist < 0.001f || dist <= _ropeLength) return;

        // Player อยู่เกินความยาวเชือก → ดึงกลับบนวงกลม
        toPlayer.Normalize();
        _owner.Position = _hookPosition + toPlayer * _ropeLength;

        // ตัด radial velocity ออก (เก็บแต่ tangential ไว้เพื่อ momentum)
        float radialDot = _owner.VelocityX * toPlayer.X + _owner.VelocityY * toPlayer.Y;
        if (radialDot > 0f) // กำลังออกจาก hook → ตัดทิ้ง
        {
            _owner.VelocityX -= toPlayer.X * radialDot;
            _owner.VelocityY -= toPlayer.Y * radialDot;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Public API — เรียกจาก Player และ Level/Enemies
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// เรียกเมื่อ projectile ชน HookPoint บน map (จาก Member 4)
    /// </summary>
    public void HookToPoint(Vector2 point)
    {
        _hookPosition    = point;
        _position        = point;
        _flyVelocity     = Vector2.Zero;
        _ropeLength      = Vector2.Distance(_owner.Position, point);
        _autoRecallTimer = AutoRecallSec;
        IsHooked         = true;
        _state           = PickaxeState.Hooked;
    }

    /// <summary>
    /// ปล่อย pickaxe กลับมือ — รีเซ็ตทุกสถานะ
    /// Player.VelocityX/Y ไม่ถูกแตะ เพื่อให้ momentum ยังคงอยู่
    /// </summary>
    public void Recall()
    {
        _state      = PickaxeState.Idle;
        IsDeployed  = false;
        IsHooked    = false;
        ChargeLevel = 0f;
        _flyVelocity      = Vector2.Zero;
        // TODO (Phase 9): เล่น recall animation / sound
    }

    // ── Draw ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// วาด rope และ pickaxe — เรียกจาก Player.DrawComponents override
    /// TODO (Phase 9): ใส่ sprite จริงและ rope texture
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        // TODO (Phase 9): วาด rope จาก _owner.Position → _hookPosition / _position
        // TODO (Phase 9): วาด pickaxe sprite ที่ _position
        // TODO (Phase 9): วาด charge arc indicator รอบ Player ตาม ChargeLevel
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Private helpers
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Right Click ค้าง → ชาร์จ
    /// Right Click ปล่อย → ขว้าง
    /// </summary>
    private void HandleChargeAndThrow(float dt)
    {
        bool rightHeld     = InputManager.Instance.IsMouseButtonDown(1);
        bool rightReleased = InputManager.Instance.IsMouseButtonReleased(1);

        if (rightHeld)
        {
            _state      = PickaxeState.Charging;
            ChargeLevel = Math.Min(ChargeLevel + dt / MaxChargeSec, 1f);
        }

        if (rightReleased && _state == PickaxeState.Charging)
            Throw();
    }

    private void Throw()
    {
        // ถ้ามี pickaxe อยู่แล้ว → recall ก่อน (1 pickaxe เท่านั้น)
        if (IsDeployed) { Recall(); return; }

        // คำนวณทิศทาง Player → Mouse
        // GetMousePosition() คืน System.Numerics.Vector2 → แปลงเป็น XNA Vector2
        var rawMouse = InputManager.Instance.GetMousePosition();
        var mousePos = new Vector2(rawMouse.X, rawMouse.Y);
        var dir      = mousePos - _owner.Position;

        if (dir == Vector2.Zero)
        {
            ChargeLevel = 0f;
            _state      = PickaxeState.Idle;
            return;
        }

        dir.Normalize();

        _position       = _owner.Position;
        _flyVelocity    = dir * FlySpeed;
        _flownDistance  = 0f;
        _maxFlyDistance = Math.Max(ChargeLevel * MaxRange, MinThrowRange);
        ChargeLevel     = 0f;
        IsDeployed      = true;
        _state          = PickaxeState.Flying;
    }

    private void UpdateFlight(float dt)
    {
        Vector2 step   = _flyVelocity * dt;
        _position     += step;
        _flownDistance += step.Length();

        // TODO (Phase 4 + Member 4): ตรวจ collision กับ GameObject ที่มี tag "HookPoint"
        //   foreach hookPoint in level.HookPoints:
        //       if Vector2.Distance(_position, hookPoint.Position) < 8f:
        //           HookToPoint(hookPoint.Position); return;

        // TODO (Phase 4 + Member 3): ตรวจ collision กับ Enemy
        //   ถ้าชน stunned enemy → enemy.TakeDamage(); Recall(); return;
        //   ถ้าชน normal enemy  → Recall(); return;

        // หมดระยะ → hook ณ ตำแหน่งปัจจุบัน
        // TODO (Member 4): เปลี่ยนเป็น collision กับ HookPoint จริง แล้วค่อย Recall() ถ้าไม่โดน
        if (_flownDistance >= _maxFlyDistance)
            HookToPoint(_position);
    }

    private void HandleHookedInput(float dt)
    {
        // ── ไต่เชือก: W/↑ = ขึ้น, S/↓ = ลง ────────────────────────────────
        bool climbUp   = InputManager.Instance.IsKeyDown(Keys.W)
                      || InputManager.Instance.IsKeyDown(Keys.Up);
        bool climbDown = InputManager.Instance.IsKeyDown(Keys.S)
                      || InputManager.Instance.IsKeyDown(Keys.Down);

        if (climbUp)   _ropeLength -= ClimbSpeed * dt;
        if (climbDown) _ropeLength += ClimbSpeed * dt;

        _ropeLength = Math.Max(_ropeLength, MinRopeLength);

        // ── พุ่งทันที: คลิกขวาครั้งเดียว = พุ่งไปทิศเม้าส์แล้ว recall ───────
        if (InputManager.Instance.IsMouseButtonPressed(1))
        {
            LaunchFromRope();
            return;
        }

        // E = recall ด้วยมือ
        // Space = ปล่อยตัวด้วย momentum (จัดการใน Player.HandleJump)
    }

    /// <summary>
    /// พุ่งตรงจาก rope ไปทิศ Player → Mouse ด้วยความเร็วคงที่ MaxLaunchSpeed
    /// </summary>
    private void LaunchFromRope()
    {
        var rawMouse = InputManager.Instance.GetMousePosition();
        var mousePos = new Vector2(rawMouse.X, rawMouse.Y);
        var dir      = mousePos - _owner.Position;

        if (dir != Vector2.Zero)
        {
            dir.Normalize();
            _owner.VelocityX = dir.X * LaunchSpeed;
            _owner.VelocityY = dir.Y * LaunchSpeed;
        }

        Recall();
        _owner.ChangeState(_owner.VelocityY <= 0f ? PlayerState.Jumping : PlayerState.Falling);
    }
}
