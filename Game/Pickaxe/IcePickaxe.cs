using System;
using System.Collections.Generic;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace WaddleAndGrapple.Game;

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
    private const float MaxChargeSec   = 0.4f;  // วินาทีชาร์จเต็ม
    private const float MaxRange       = 400f;  // ระยะขว้างสูงสุด (px)
    private const float MinThrowRange  = 60f;   // ระยะขั้นต่ำแม้ไม่ชาร์จ
    private const float FlySpeed       = 900f;  // ความเร็ว projectile (px/s)
    private const float ClimbSpeed     = 150f;  // ความเร็วไต่เชือก (px/s)
    private const float MinRopeLength  = 20f;   // ความยาวเชือกขั้นต่ำ (px)
    public  const float MaxRopeLength      = 700f;   // ระยะสูงสุดขณะบิน (px) — เกินนี้ hook ไม่ติด
    private const float MaxSwingRopeLength = 700f * 1.5f; // ความยาวเชือกสูงสุดขณะแกว่ง (px) — เกินนี้ auto-recall
    private const float FlyGravity     = 1200f; // แรงโน้มถ่วงขณะ pickaxe บิน (px/s²)
    private const float RecallSpeed    = 900f;  // ความเร็วดึงเชือกกลับ (px/s)

    // ── Owner ─────────────────────────────────────────────────────────────────
    private readonly Player _owner;

    // ── Public State ──────────────────────────────────────────────────────────
    public bool  IsDeployed  { get; private set; }
    public bool  IsHooked    { get; private set; }
    public float ChargeLevel { get; private set; } // 0.0 – 1.0

    // ── Public Position (อ่านได้จาก PickaxeRenderer) ─────────────────────────
    public Vector2 PickaxePosition => _position;
    public Vector2 HookPosition    => _hookPosition;
    public float   FlyAngle        => (float)Math.Atan2(_flyVelocity.Y, _flyVelocity.X);
    public PickaxeStateKind CurrentState => _state switch
    {
        PickaxeState.Charging  => PickaxeStateKind.Charging,
        PickaxeState.Flying    => PickaxeStateKind.Flying,
        PickaxeState.Hooked    => PickaxeStateKind.Hooked,
        PickaxeState.Recalling => PickaxeStateKind.Recalling,
        PickaxeState.Launching => PickaxeStateKind.Launching,
        _                      => PickaxeStateKind.Idle,
    };

    // สำหรับ Player ตรวจสถานะ launch
    public bool IsLaunching       => _state == PickaxeState.Launching;
    public bool IsLaunchComplete  { get; private set; }
    /// <summary>waypoint ปัจจุบันที่ player ควรเคลื่อนไปหาตอน Launching</summary>
    public Vector2 LaunchTarget   => _bendPoints.Count > 0 ? _bendPoints[0].Position : _hookPosition;

    /// <summary>ความยาวเชือกทั้งหมด = segment ปัจจุบัน + ทุก bend segment</summary>
    private float TotalRopeLength
    {
        get
        {
            float total = _ropeLength;
            foreach (var bp in _bendPoints) total += bp.SegmentLength;
            return total;
        }
    }

    public enum PickaxeStateKind { Idle, Charging, Flying, Hooked, Recalling, Launching }

    // ── Internal State Machine ────────────────────────────────────────────────
    private enum PickaxeState { Idle, Charging, Flying, Hooked, Recalling, Launching }
    private PickaxeState _state = PickaxeState.Idle;

    // Projectile (ขณะบิน)
    private Vector2 _position;        // ตำแหน่งของ pickaxe ในโลก
    private Vector2 _flyVelocity;     // velocity ของ projectile (ใช้ทั้ง Flying และ Recalling)
    private float   _flownDistance;   // ระยะแนวนอนที่บินไปแล้ว
    private float   _maxFlyDistance;  // ระยะสูงสุดตาม charge

    // Hook (ขณะ hooked)
    private Vector2 _hookPosition;    // จุดที่ hook อยู่บน map
    private float   _ropeLength;      // ความยาวเชือกปัจจุบัน (จาก anchor ปัจจุบัน → player)

    // Rope Wrapping — bend points เมื่อเชือกพาดผ่านขอบวัตถุ
    // ลำดับ: index 0 = ใกล้ player สุด, index [^1] = ใกล้ hook สุด
    private struct BendPoint
    {
        public Vector2   Position;
        public Rectangle Solid;
        public float     SegmentLength; // distance ไปยัง bend ถัดไปหรือ hook (restore เมื่อ unwrap)
    }
    private readonly List<BendPoint> _bendPoints = [];

    // สำหรับ PickaxeRenderer วาดผ่าน bend points
    public int     BendCount          => _bendPoints.Count;
    public Vector2 GetBendPoint(int i) => _bendPoints[i].Position;

    /// <summary>ตั้งเป็น true เมื่อ Player ใช้คลิกขวาเพื่อ launch → ป้องกันชาร์จซ้ำในเฟรมถัดไป</summary>
    public bool SuppressCharge { get; set; }

    private List<Enemy> _enemies = [];
    public void SetEnemies(List<Enemy> enemies) => _enemies = enemies;

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
                if (InputManager.Instance.IsMouseButtonPressed(1))
                    StartRecall();
                break;

            case PickaxeState.Hooked:
                HandleHookedInput(dt);
                break;

            case PickaxeState.Launching:
                CheckLaunchProgress();
                break;

            case PickaxeState.Recalling:
                UpdateRecall(dt);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // เรียกจาก Player.Update() หลัง MoveAndCollide
    // บังคับ rope constraint: ถ้า Player ห่างจาก hookPoint เกิน ropeLength → ดึงกลับ
    // ══════════════════════════════════════════════════════════════════════════
    public void ApplyConstraint()
    {
        if (_state != PickaxeState.Hooked) return; // Launching ข้าม — player เคลื่อนเองตาม waypoint

        // อัปเดต wrap/unwrap ขณะ player แกว่ง
        UpdateRopeWrap();

        // anchor ปัจจุบัน = bend[0] (ใกล้ player) หรือ hook ถ้าไม่มี bend
        Vector2 anchor   = _bendPoints.Count > 0 ? _bendPoints[0].Position : _hookPosition;
        Vector2 toPlayer = _owner.Position - anchor;
        float   dist     = toPlayer.Length();

        if (dist < 0.001f || dist <= _ropeLength) return;

        // Player อยู่เกินความยาวเชือก → ดึงกลับบนวงกลม
        toPlayer.Normalize();
        Vector2 proposed = anchor + toPlayer * _ropeLength;

        // ตรวจว่าตำแหน่งใหม่จะชน solid ไหม — ถ้าใช่ให้หยุดเชือกไว้
        var curBounds = _owner.ColliderBounds;
        int halfW = curBounds.Width  / 2;
        int halfH = curBounds.Height / 2;
        var newBounds = new Microsoft.Xna.Framework.Rectangle(
            (int)(proposed.X - halfW),
            (int)(proposed.Y - halfH),
            curBounds.Width,
            curBounds.Height
        );
        foreach (var solid in _owner.Solids)
        {
            if (newBounds.Intersects(solid))
            {
                _ropeLength = dist;
                if (TotalRopeLength > MaxSwingRopeLength)
                    StartRecall();
                return;
            }
        }

        _owner.Position = proposed;

        // ตัด radial velocity ออก (เก็บแต่ tangential ไว้เพื่อ momentum)
        float radialDot = _owner.VelocityX * toPlayer.X + _owner.VelocityY * toPlayer.Y;
        if (radialDot > 0f)
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
        _hookPosition = point;
        _position     = point;
        _flyVelocity  = Vector2.Zero;

        // คำนวณ SegmentLength สำหรับ bend points ที่เก็บมาตอน flight
        // ลำดับ: bend[0] ใกล้ player, bend[^1] ใกล้ hook
        for (int i = 0; i < _bendPoints.Count; i++)
        {
            Vector2 nextPos = i + 1 < _bendPoints.Count ? _bendPoints[i + 1].Position : point;
            var bp = _bendPoints[i];
            bp.SegmentLength = Vector2.Distance(bp.Position, nextPos);
            _bendPoints[i]   = bp;
        }

        // rope length = distance จาก effective anchor (bend[0] หรือ hook) ไปถึง player
        // ใช้ bend ที่ CheckFlightWrap สะสมไว้ตอนบิน — ไม่คำนวณซ้ำเพราะจะทับ bend ที่ถูกต้อง
        Vector2 firstAnchor = _bendPoints.Count > 0 ? _bendPoints[0].Position : point;
        _ropeLength = Vector2.Distance(firstAnchor, _owner.Position);

        // เชือกยาวเกิน MaxRopeLength → ไม่ hook, ดึงกลับทันที
        if (_ropeLength > MaxRopeLength) { StartRecall(); return; }

        AudioManager.Instance.PlaySound("SFX/PickaxeHit");
        
        IsHooked    = true;
        _state      = PickaxeState.Hooked;
    }

    /// <summary>
    /// ปล่อย pickaxe กลับมือ — รีเซ็ตทุกสถานะ
    /// Player.VelocityX/Y ไม่ถูกแตะ เพื่อให้ momentum ยังคงอยู่
    /// </summary>
    public void Recall()
    {
        _state           = PickaxeState.Idle;
        IsDeployed       = false;
        IsHooked         = false;
        IsLaunchComplete = false;
        ChargeLevel      = 0f;
        _flyVelocity     = Vector2.Zero;
        _bendPoints.Clear();
        // TODO (Phase 9): เล่น recall animation / sound
    }

    /// <summary>
    /// เริ่มดึงตัวเองไปตามเชือก — เรียกจาก Player เมื่อคลิกขวาขณะ Hooked
    /// </summary>
    public void StartLaunch()
    {
        IsLaunchComplete = false;
        IsHooked         = false;
        _state           = PickaxeState.Launching;
        AudioManager.Instance.PlaySound("SFX/PickaxeRope");
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

    // ── Rope Wrapping Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// ตรวจ rope segment (last bend หรือ player → pickaxe) ขณะบิน
    /// ถ้าผ่าน solid → เพิ่ม bend point ที่ corner ที่ใกล้ที่สุด
    /// </summary>
    private void CheckFlightWrap()
    {
        Vector2 ropeFrom    = _bendPoints.Count > 0 ? _bendPoints[^1].Position : _owner.Position;
        Vector2 lastBendPos = _bendPoints.Count > 0 ? _bendPoints[^1].Position : new Vector2(float.MinValue, float.MinValue);

        float     bestDist   = float.MaxValue;
        Vector2   bestCorner = Vector2.Zero;
        Rectangle bestSolid  = default;

        const float PickaxeRadius = 6f;
        foreach (var solid in _owner.Solids)
        {
            // ข้าม solid ที่ pickaxe กำลังอยู่ข้างใน (กำลัง hook ใน frame นี้ — ไม่ต้อง wrap)
            if (_position.X + PickaxeRadius > solid.Left && _position.X - PickaxeRadius < solid.Right
             && _position.Y + PickaxeRadius > solid.Top  && _position.Y - PickaxeRadius < solid.Bottom)
                continue;

            if (SegmentCrossesRect(ropeFrom, _position, solid, out Vector2 corner)
                && Vector2.DistanceSquared(corner, lastBendPos) > 4f   // ไม่เพิ่ม corner เดิมซ้ำ
                && Vector2.DistanceSquared(corner, ropeFrom)    > 100f
                && Vector2.DistanceSquared(corner, _position)   > 100f)
            {
                float d = Vector2.DistanceSquared(ropeFrom, corner);
                if (d < bestDist) { bestDist = d; bestCorner = corner; bestSolid = solid; }
            }
        }

        if (bestSolid != default)
        {
            _bendPoints.Add(new BendPoint { Position = bestCorner, Solid = bestSolid, SegmentLength = 0f });
        }
    }

    /// <summary>
    /// ขณะ hooked: เพิ่ม bend ถ้าเชือก player→anchor ผ่าน solid
    /// และลบ bend ถ้า player แกว่งกลับจนไม่ต้องการแล้ว (unwrap)
    /// </summary>
    private void UpdateRopeWrap()
    {
        // --- Unwrap: ลบ bend[0] ถ้า player มองเห็น anchor ถัดไปได้โดยตรง ---
        // ใช้ SegmentCrossesRectInterior (ตรวจ 4 ขอบ, strict t) เพื่อรองรับ
        // กรณี player อยู่ใต้ platform และ hook อยู่บน top surface
        // (SegmentCrossesRect เดิมไม่ตรวจขอบล่าง → ลบ bend ผิด)
        bool didUnwrap = false;
        while (_bendPoints.Count > 0)
        {
            var   first      = _bendPoints[0];
            Vector2 nextAnchor = _bendPoints.Count >= 2 ? _bendPoints[1].Position : _hookPosition;

            if (!SegmentCrossesRectInterior(_owner.Position, nextAnchor, first.Solid))
            {
                _ropeLength += first.SegmentLength; // คืน rope ที่ถูกใช้ใน segment นี้
                _bendPoints.RemoveAt(0);
                didUnwrap = true;
            }
            else break;
        }

        // ถ้าเพิ่ง unwrap ไปในเฟรมนี้แล้ว → ข้าม wrap check
        // ป้องกัน SegmentCrossesRect กับ SegmentCrossesRectInterior ให้ผลต่างกันแล้ว wrap/unwrap กลับซ้ำในเฟรมเดียว
        if (didUnwrap) return;

        // --- Wrap: ตรวจ player → anchor ปัจจุบัน ผ่าน solid ใหม่ไหม ---
        // เลือก solid ที่ใกล้ player ที่สุด (corner ใกล้สุด) เพื่อให้ wrap ถูก solid
        Vector2 anchor = _bendPoints.Count > 0 ? _bendPoints[0].Position : _hookPosition;
        Rectangle curSolid = _bendPoints.Count > 0 ? _bendPoints[0].Solid : default;

        float   bestDist   = float.MaxValue;
        Vector2 bestCorner = Vector2.Zero;
        Rectangle bestSolid = default;

        foreach (var solid in _owner.Solids)
        {
            if (_bendPoints.Count > 0 && solid == curSolid) continue;

            if (SegmentCrossesRect(_owner.Position, anchor, solid, out Vector2 corner)
                && Vector2.DistanceSquared(corner, anchor)           > 100f
                && Vector2.DistanceSquared(corner, _owner.Position)  > 100f)
            {
                float d = Vector2.DistanceSquared(_owner.Position, corner);
                if (d < bestDist) { bestDist = d; bestCorner = corner; bestSolid = solid; }
            }
        }

        if (bestSolid != default)
        {
            float segLen = Vector2.Distance(bestCorner, anchor);
            if (_ropeLength - segLen >= MinRopeLength)
            {
                _bendPoints.Insert(0, new BendPoint
                {
                    Position      = bestCorner,
                    Solid         = bestSolid,
                    SegmentLength = segLen
                });
                _ropeLength = Vector2.Distance(bestCorner, _owner.Position);
            }
        }
    }

    /// <summary>
    /// ตรวจว่า segment A→B ผ่าน Rectangle ไหมมั้ย
    /// ตรวจแค่ขอบ Top / Left / Right (ไม่ตรวจขอบล่าง — pickaxe บินจากข้างบนเสมอ)
    /// เลือก corner ที่ทำให้ path A→corner→B ไม่ทะลุ solid (geometric validation)
    /// </summary>
    private static bool SegmentCrossesRect(Vector2 a, Vector2 b, Rectangle rect, out Vector2 wrapCorner)
    {
        wrapCorner = Vector2.Zero;

        // ถ้า origin อยู่ภายในด้านใน rect → ไม่นับ (ป้องกัน self-wrap)
        // ใช้ strict interior เพื่อให้ corner บน boundary (เช่น TL, TR) ยังตรวจขอบอื่นได้
        if (a.X > rect.Left && a.X < rect.Right &&
            a.Y > rect.Top  && a.Y < rect.Bottom) return false;

        var tl = new Vector2(rect.Left,  rect.Top);
        var tr = new Vector2(rect.Right, rect.Top);
        var br = new Vector2(rect.Right, rect.Bottom);
        var bl = new Vector2(rect.Left,  rect.Bottom);

        float   bestT  = float.MaxValue;
        Vector2 edgeC  = default, edgeD = default;

        // Top: tl→tr | Left: bl→tl | Right: tr→br  (ข้ามขอบล่าง br→bl)
        TryEdgeTracked(a, b, tl, tr, ref bestT, ref edgeC, ref edgeD);
        TryEdgeTracked(a, b, bl, tl, ref bestT, ref edgeC, ref edgeD);
        TryEdgeTracked(a, b, tr, br, ref bestT, ref edgeC, ref edgeD);

        if (bestT == float.MaxValue) return false;

        // เลือก corner ที่ path A→corner→B ไม่ทะลุผ่าน solid
        // (เช่น left edge: bl→B จะวิ่งทะลุ platform แต่ tl→B จะวิ่งเหนือ)
        bool cOk = PathAvoidsSolid(a, edgeC, b, rect);
        bool dOk = PathAvoidsSolid(a, edgeD, b, rect);

        if      ( cOk && !dOk) wrapCorner = edgeC;
        else if (!cOk &&  dOk) wrapCorner = edgeD;
        else                   // fallback: nearest to A
            wrapCorner = Vector2.DistanceSquared(a, edgeC) <= Vector2.DistanceSquared(a, edgeD)
                       ? edgeC : edgeD;

        return true;
    }

    private static void TryEdgeTracked(Vector2 a, Vector2 b, Vector2 c, Vector2 d,
                                        ref float bestT, ref Vector2 edgeC, ref Vector2 edgeD)
    {
        Vector2 r   = b - a;
        Vector2 s   = d - c;
        float   rxs = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(rxs) < 1e-6f) return;

        Vector2 ca = c - a;
        float   t  = (ca.X * s.Y - ca.Y * s.X) / rxs;
        float   u  = (ca.X * r.Y - ca.Y * r.X) / rxs;

        if (t > 1e-4f && t <= 1f && u >= 0f && u <= 1f && t < bestT)
        {
            bestT = t;
            edgeC = c;
            edgeD = d;
        }
    }

    /// <summary>ตรวจว่า A→mid→B ไม่ตัดผ่านด้านในของ rect (endpoints บน boundary ใช้ได้)</summary>
    private static bool PathAvoidsSolid(Vector2 a, Vector2 mid, Vector2 b, Rectangle rect)
        => !SegmentCrossesRectInterior(a, mid, rect) && !SegmentCrossesRectInterior(mid, b, rect);

    /// <summary>
    /// ตรวจว่า segment A→B ตัดผ่านด้านในของ rect
    /// รวม: b อยู่ในด้านในเลย (เช่น pickaxe ฝังอยู่ใน solid ก่อน hook)
    /// </summary>
    private static bool SegmentCrossesRectInterior(Vector2 a, Vector2 b, Rectangle rect)
    {
        const float eps = 1f;
        // ถ้า endpoint b อยู่ภายใน rect โดยตรง → segment นี้ invalid
        if (b.X > rect.Left + eps && b.X < rect.Right  - eps &&
            b.Y > rect.Top  + eps && b.Y < rect.Bottom - eps)
            return true;

        var tl = new Vector2(rect.Left,  rect.Top);
        var tr = new Vector2(rect.Right, rect.Top);
        var br = new Vector2(rect.Right, rect.Bottom);
        var bl = new Vector2(rect.Left,  rect.Bottom);
        return EdgeInteriorCross(a, b, tl, tr)
            || EdgeInteriorCross(a, b, tr, br)
            || EdgeInteriorCross(a, b, br, bl)
            || EdgeInteriorCross(a, b, bl, tl);
    }

    /// <summary>ตรวจว่า segment A→B ตัด edge C→D ที่จุดภายใน (t และ u ห่างจาก endpoint)</summary>
    private static bool EdgeInteriorCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        Vector2 r   = b - a;
        Vector2 s   = d - c;
        float   rxs = r.X * s.Y - r.Y * s.X;
        if (MathF.Abs(rxs) < 1e-6f) return false;
        Vector2 ca  = c - a;
        float   t   = (ca.X * s.Y - ca.Y * s.X) / rxs;
        float   u   = (ca.X * r.Y - ca.Y * r.X) / rxs;
        const float eps = 1e-3f;
        return t > eps && t < 1f - eps && u >= 0f && u <= 1f;
    }

    // ── Charge / Throw ────────────────────────────────────────────────────────

    /// <summary>
    /// Right Click ค้าง → ชาร์จ
    /// Right Click ปล่อย → ขว้าง
    /// </summary>
    private void HandleChargeAndThrow(float dt)
    {
        bool rightHeld     = InputManager.Instance.IsMouseButtonDown(0);
        bool rightReleased = InputManager.Instance.IsMouseButtonReleased(0);

        // คลายล็อกเมื่อปล่อยคลิกซ้าย (ป้องกันชาร์จซ้ำหลัง launch)
        if (rightReleased) SuppressCharge = false;

        if (rightHeld && !SuppressCharge)
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

        // แปลง mouse screen position → world position ผ่าน camera
        var rawMouse = InputManager.Instance.GetMousePosition();
        var mousePos = ScreenToWorldMouse(new Vector2(rawMouse.X, rawMouse.Y));
        var dir      = mousePos - _owner.Position;

        if (dir == Vector2.Zero)
        {
            ChargeLevel = 0f;
            _state      = PickaxeState.Idle;
            return;
        }

        dir.Normalize();

        AudioManager.Instance.PlaySound("SFX/PickaxeThrow");

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
        _flyVelocity = new Vector2(_flyVelocity.X, _flyVelocity.Y + FlyGravity * dt); // gravity ดึง pickaxe โค้งลง

        Vector2 step   = _flyVelocity * dt;
        _position     += step;
        _flownDistance += Math.Abs(step.X); // นับแค่แนวนอน ให้ขาขึ้น-ลง arc ได้เต็มที่

        // ตรวจ corner ที่ pickaxe บินผ่านใกล้ (ต้องก่อน solid hit)
        // เรียกครั้งเดียวต่อเฟรม — ให้ pickaxe เคลื่อนที่ก่อนแล้วค่อยตรวจรอบถัดไป
        CheckFlightWrap();

        const float PickaxeRadius = 6f;

        // ตรวจชน enemy ก่อน solid — กัน enemy feet ทับพื้น solid แล้ว solid โดนก่อน
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;
            var b = enemy.ColliderBounds;
            if (_position.X + PickaxeRadius > b.Left
             && _position.X - PickaxeRadius < b.Right
             && _position.Y + PickaxeRadius > b.Top
             && _position.Y - PickaxeRadius < b.Bottom)
            {
                enemy.Die();
                StartRecall();
                return;
            }
        }

        // ตรวจชน solid → hook บน surface (snap ไปที่ขอบ solid ที่ใกล้ที่สุด)
        foreach (var solid in _owner.Solids)
        {
            if (_position.X + PickaxeRadius > solid.Left
             && _position.X - PickaxeRadius < solid.Right
             && _position.Y + PickaxeRadius > solid.Top
             && _position.Y - PickaxeRadius < solid.Bottom)
            {
                // snap hook ไปที่ขอบ solid แทน _position (ซึ่งอาจอยู่ข้างในเล็กน้อย)
                float oL = _position.X - solid.Left;
                float oR = solid.Right  - _position.X;
                float oT = _position.Y - solid.Top;
                float oB = solid.Bottom - _position.Y;
                float m  = Math.Min(Math.Min(oL, oR), Math.Min(oT, oB));
                Vector2 hookPt = _position;
                if      (m == oT) hookPt.Y = solid.Top;
                else if (m == oB) hookPt.Y = solid.Bottom;
                else if (m == oL) hookPt.X = solid.Left;
                else              hookPt.X = solid.Right;
                HookToPoint(hookPt);
                return;
            }
        }

        // หมดระยะแนวนอน → หยุด X ให้ตกต่อด้วย gravity
        if (_flownDistance >= _maxFlyDistance)
            _flyVelocity = new Vector2(0f, _flyVelocity.Y);

        // ระยะตรง player → pickaxe เกิน MaxRopeLength → ดึงกลับทันที
        float ropeDist = Vector2.Distance(_position, _owner.Position);
        if (ropeDist > MaxRopeLength)
            StartRecall();
    }

    private Vector2 ScreenToWorldMouse(Vector2 screenPos)
    {
        var camera = SceneManager.Instance.CurrentScene?.Camera;
        return camera != null ? camera.ScreenToWorld(screenPos) : screenPos;
    }

    private void HandleHookedInput(float dt)
    {
        // ── ไต่เชือก: W/↑ = ขึ้น, S/↓ = ลง ────────────────────────────────
        bool climbUp   = InputManager.Instance.IsKeyDown(Keys.W);
        bool climbDown = InputManager.Instance.IsKeyDown(Keys.S);

        if (climbUp)   _ropeLength -= ClimbSpeed * dt;
        if (climbDown) _ropeLength += ClimbSpeed * dt;

        _ropeLength = Math.Max(_ropeLength, MinRopeLength);

        float totalHooked = TotalRopeLength;

        // เชือกยืดเกิน MaxSwingRopeLength → auto-recall
        if (totalHooked > MaxSwingRopeLength)
        {
            StartRecall();
            return;
        }

        // ── คลิกซ้าย / E = ดึงเชือกกลับ ─────────────────────────────────────
        if (InputManager.Instance.IsMouseButtonPressed(1) ||
            InputManager.Instance.IsKeyPressed(Keys.E))
        {
            StartRecall();
            return;
        }

        // คลิกขวา = Player จัดการเอง (Player.Update ตรวจ Pickaxe.IsHooked + คลิกขวา)
        // Space = ปล่อยตัวด้วย momentum (จัดการใน Player.HandleJump)
    }

    /// <summary>
    /// ตรวจว่า player ถึง LaunchTarget ปัจจุบันแล้วหรือยัง
    /// ถ้าถึง → เลื่อน waypoint ต่อ หรือจบ launch ถ้าไม่มีเหลือ
    /// </summary>
    private void CheckLaunchProgress()
    {
        Vector2 target = LaunchTarget;
        float   dist   = Vector2.Distance(_owner.Position, target);

        if (dist >= 20f) return;

        if (_bendPoints.Count > 0)
        {
            _bendPoints.RemoveAt(0); // เลื่อนไป waypoint ถัดไป
        }
        else
        {
            // ถึง hook แล้ว
            IsLaunchComplete = true;
            Recall();
        }
    }

    /// <summary>เริ่ม reel-in: ถ้า hooked ให้เลื่อน _position มาที่ hook ก่อน</summary>
    private void StartRecall()
    {
        if (_state == PickaxeState.Hooked)
            _position = _hookPosition;

        AudioManager.Instance.PlaySound("SFX/PickaxeRope");

        IsHooked  = false;
        _state    = PickaxeState.Recalling;
    }

    /// <summary>
    /// เลื่อน pickaxe กลับหา player ด้วย RecallSpeed
    /// ลบ bend points ทีละอันเมื่อ pickaxe ผ่านไป
    /// พอชิด player → Recall() ทันที
    /// </summary>
    private void UpdateRecall(float dt)
    {
        // target ปัจจุบัน = bend ที่ใกล้ pickaxe สุด (index สุดท้าย) หรือ player ถ้าไม่มี bend
        Vector2 target  = _bendPoints.Count > 0 ? _bendPoints[^1].Position : _owner.Position;
        Vector2 toTarget = target - _position;
        float   dist    = toTarget.Length();

        // ถึง target แล้ว
        if (dist < 10f)
        {
            if (_bendPoints.Count > 0)
            {
                // ลบ bend นี้ แล้ว snap ไปยังตำแหน่งจริง
                _position = _bendPoints[^1].Position;
                _bendPoints.RemoveAt(_bendPoints.Count - 1);
            }
            else
            {
                Recall(); // ถึง player → เก็บกลับ
            }
            return;
        }

        toTarget /= dist; // normalize
        _flyVelocity = toTarget * RecallSpeed; // อัปเดตทิศสำหรับ FlyAngle
        _position   += _flyVelocity * dt;
    }

}
