using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

// ── State Machine ─────────────────────────────────────────────────────────────
public enum E1eP4MKIIState
{
    Idle,
    Igniting,
    Explode,
    Dead,
}

// ─────────────────────────────────────────────────────────────────────────────

public class E1eP4MKII : Enemy
{
    // ── Collider Size ─────────────────────────────────────────────────────────
    private const int EnemyWidth  = 40; // เปลี่ยนเป็น 80 พอทำ Level จริงเสร็จ
    private const int EnemyHeight = 60; // เปลี่ยนเป็น 120 พอทำ Level จริงเสร็จ

    // ── Temporary Ground (ลบเมื่อ tiles พร้อม) ───────────────────────────────
    private const float TempGroundY = 400f;

    // ── Sprite Scale ──────────────────────────────────────────────────────────
    public const float DisplayScale = 2f; // เปลี่ยนเป็น 2f พอทำ Level จริงเสร็จ

    // ── AI Ranges ─────────────────────────────────────────────────────────────
    public float DetectionRange { get; set; } = 60f; // ระยะมองเห็น player และระยะโยนค้อน

    // ── Kill Zone ─────────────────────────────────────────────────────────────
    // KillZoneScaleReference = ระยะที่ทำให้ explode2 มีขนาดพอดีกับ DisplayScale (ค่าคงที่ ห้ามเปลี่ยน)
    private const float KillZoneScaleReference = 80f; // DO NOT TOUCH!!!!!!!!!!
    public float KillZoneRange { get; set; } = 80f; // ปรับค่านี้เพื่อเปลี่ยนทั้งระยะฆ่าและขนาด explosion

    // ── Explode Animation Durations ───────────────────────────────────────────
    private const float Explode1AnimDuration = 2 * 0.13f; // ตรงกับ explode1 (2 frames)
    private const float Explode2AnimDuration = 7 * 0.083f; // ตรงกับ explode2 (7 frames)
    private float _explode1Timer;
    private float _explode2Timer;

    // ── Velocity ──────────────────────────────────────────────────────────────
    public float VelocityX;
    public float VelocityY;

    // ── Ground Status ─────────────────────────────────────────────────────────
    public bool IsGrounded      { get; set; }
    public int  FacingDirection { get; set; } = 1; // +1 = ขวา, -1 = ซ้าย

    // ── State Machine ─────────────────────────────────────────────────────────
    public E1eP4MKIIState State { get; private set; } = E1eP4MKIIState.Idle;

    // ── Player Reference ──────────────────────────────────────────────────────
    private Player _player;

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer   _spriteRenderer;
    private Animator         _animator;
    private E1eP4MKIIBoxCollider _collider;
    private List<Rectangle>  _solidRects = [];

    // ── Death ─────────────────────────────────────────────────────────────────
    // 7 frames × 0.13 s — ตรงกับ dead animation ที่ลงทะเบียนใน Initialize
    private const float DeadAnimDuration = 8 * 0.13f;
    private float _deadTimer;

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        Scale       = new Vector2(DisplayScale, DisplayScale);
        _animator   = AddComponent<Animator>();
        _spriteRenderer            = GetComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;

        // TODO: แทนที่ "Enemy/Enemy-SpriteSheet" ด้วย path spritesheet จริง
        //       และปรับ rows/columns/totalFrames ให้ตรงกับไฟล์
        var f1 = new AnimationFactory(
            ResourceManager.Instance.GetTexture("Enemy4-SpriteSheet"),
            rows: 3, columns: 8
        );

        _animator.AddAnimation("idle",     f1.CreateFromRow(row: 0, totalFrames: 7, frameDuration: 0.083f));
        _animator.AddAnimation("dead",     f1.CreateFromRow(row: 1, totalFrames: 7, frameDuration: 0.13f, isLooping: false));
        _animator.AddAnimation("explode1", f1.CreateFromRow(row: 2, totalFrames: 2, frameDuration: 0.13f, isLooping: false));

        var f2 = new AnimationFactory(
            ResourceManager.Instance.GetTexture("Explosion-SpriteSheet"),
            rows: 1, columns: 7
        );

        _animator.AddAnimation("explode2", f2.CreateFromRow(row: 0, totalFrames: 7, frameDuration: 0.083f, isLooping: false));

        _animator.Play("idle");

        _collider = AddComponent<E1eP4MKIIBoxCollider>();
        UpdateColliderBounds();
    }

    // ── Update Loop ───────────────────────────────────────────────────────────
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (WorldTime.IsFrozen) return;

        // Dead: นับ timer รอ animation จบ แล้ว deactivate
        if (State == E1eP4MKIIState.Dead)
        {
            if (_deadTimer > 0f) _deadTimer -= dt;
            else base.Active = false;
            return;
        }

        // Explode timers
        if (_explode1Timer > 0f) _explode1Timer -= dt;
        if (_explode2Timer > 0f) _explode2Timer -= dt;

        // Pickaxe hit check (ก่อน AI เพื่อ preempt state machine)
        CheckPickaxeHit();

        // AI decision → ตั้ง VelocityX
        UpdateAI();

        // Physics
        MoveAndCollide(dt);

        // Animation + sprite flip
        SyncAnimation();
        Rotation = FacingDirection == -1
            ? QuaternionUtils.Euler(0, 180, 0)
            : Vector3.Zero;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // AI
    // ══════════════════════════════════════════════════════════════════════════

    private void UpdateAI()
    {
        if (_player == null) return;

        float distToPlayer  = Vector2.Distance(Position, _player.Position);
        bool  playerInSight = CanSeePlayer(distToPlayer);

        switch (State)
        {
            // ── Idle: หยุดนิ่ง รอ player เข้าใกล้ ──────────────────────────────
            case E1eP4MKIIState.Idle:
                if (playerInSight)
                    ChangeState(E1eP4MKIIState.Igniting);
                break;

            // ── Igniting: เล่น explode1 รอ animation จบ ────────────────────────
            case E1eP4MKIIState.Igniting:
                if (_explode1Timer <= 0f)
                    ChangeState(E1eP4MKIIState.Explode); // ระเบิดเสมอ ฆ่า player ถ้าอยู่ใน killzone
                break;

            // ── Explode: เล่น explode2 รอ animation จบ แล้ว deactivate ทันที ────
            case E1eP4MKIIState.Explode:
                if (_explode2Timer <= 0f)
                    base.Active = false;
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Pickaxe Hit Detection
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// ตรวจว่า pickaxe ที่กำลังบินอยู่ชนกับ collider ของ E1eP4MKII ไหม
    /// ใช้ได้เฉพาะตอน Idle — ถ้าชนให้ตายเงียบๆ โดยไม่ระเบิด
    /// </summary>
    private void CheckPickaxeHit()
    {
        if (State != E1eP4MKIIState.Idle) return;
        if (_player == null) return;
        if (_player.Pickaxe.CurrentState != IcePickaxe.PickaxeStateKind.Flying) return;

        const float PickaxeRadius = 6f;
        var pos = _player.Pickaxe.PickaxePosition;
        var expanded = new Rectangle(
            _collider.Bounds.Left   - (int)PickaxeRadius,
            _collider.Bounds.Top    - (int)PickaxeRadius,
            _collider.Bounds.Width  + (int)PickaxeRadius * 2,
            _collider.Bounds.Height + (int)PickaxeRadius * 2
        );
        if (!expanded.Contains((int)pos.X, (int)pos.Y)) return;

        _player.Pickaxe.Recall();
        Die();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Line-of-Sight Raycast
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// คืน true ถ้าศัตรูมองเห็น player:
    ///   1. ระยะไม่เกิน DetectionRange
    ///   2. ไม่มี solid tile บัง line segment ระหว่างศัตรู → player
    /// </summary>
    private bool CanSeePlayer(float distToPlayer)
    {
        if (distToPlayer > DetectionRange) return false;

        // Raycast: solid ตัวไหนบัง line of sight → มองไม่เห็น
        foreach (var solid in _solidRects)
            if (SegmentIntersectsRect(Position, _player.Position, solid))
                return false;

        return true;
    }

    /// <summary>คืน true ถ้า line segment (a→b) ตัดผ่าน rectangle ใดๆ</summary>
    private static bool SegmentIntersectsRect(Vector2 a, Vector2 b, Rectangle rect)
    {
        // endpoint อยู่ในกล่องเลย → ตัดกันแน่
        if (rect.Contains((int)a.X, (int)a.Y) || rect.Contains((int)b.X, (int)b.Y))
            return true;

        // ทดสอบ segment กับขอบทั้ง 4 ของ rectangle
        var tl = new Vector2(rect.Left,  rect.Top);
        var tr = new Vector2(rect.Right, rect.Top);
        var bl = new Vector2(rect.Left,  rect.Bottom);
        var br = new Vector2(rect.Right, rect.Bottom);

        return SegmentsIntersect(a, b, tl, tr)  // ขอบบน
            || SegmentsIntersect(a, b, tr, br)  // ขอบขวา
            || SegmentsIntersect(a, b, br, bl)  // ขอบล่าง
            || SegmentsIntersect(a, b, bl, tl); // ขอบซ้าย
    }

    /// <summary>ตรวจ intersection ของ 2 line segments ด้วย cross-product</summary>
    private static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float d1x = p2.X - p1.X, d1y = p2.Y - p1.Y;
        float d2x = p4.X - p3.X, d2y = p4.Y - p3.Y;
        float cross = d1x * d2y - d1y * d2x;

        if (System.MathF.Abs(cross) < 1e-10f) return false; // parallel

        float dx = p3.X - p1.X, dy = p3.Y - p1.Y;
        float t = (dx * d2y - dy * d2x) / cross;
        float u = (dx * d1y - dy * d1x) / cross;

        return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Animation Sync
    // ══════════════════════════════════════════════════════════════════════════

    private void SyncAnimation()
    {
        switch (State)
        {
            case E1eP4MKIIState.Igniting:
                _animator.Play("explode1");
                break;
            case E1eP4MKIIState.Explode:
                _animator.Play("explode2");
                break;
            case E1eP4MKIIState.Dead:
                _animator.Play("dead");
                break;
            default:
                _animator.Play("idle");
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Physics (เหมือน Player)
    // ══════════════════════════════════════════════════════════════════════════

    private void MoveAndCollide(float dt)
    {
        IsGrounded = false;

        // ── Horizontal ────────────────────────────────────────────────────────
        Position = new Vector2(Position.X + VelocityX * dt, Position.Y);
        UpdateColliderBounds();

        foreach (var solid in _solidRects)
        {
            if (!_collider.Bounds.Intersects(solid)) continue;

            if (VelocityX > 0f)
            {
                Position = new Vector2(solid.Left - EnemyWidth / 2f, Position.Y);
            }
            else if (VelocityX < 0f)
            {
                Position = new Vector2(solid.Right + EnemyWidth / 2f, Position.Y);
            }
            VelocityX = 0f;
            UpdateColliderBounds();
        }

        // ── Vertical ──────────────────────────────────────────────────────────
        Position = new Vector2(Position.X, Position.Y + VelocityY * dt);
        UpdateColliderBounds();

        foreach (var solid in _solidRects)
        {
            bool hit = _collider.Bounds.Left   < solid.Right
                    && _collider.Bounds.Right  > solid.Left
                    && _collider.Bounds.Top    < solid.Bottom
                    && _collider.Bounds.Bottom >= solid.Top;
            if (!hit) continue;

            if (VelocityY > 0f)
            {
                Position   = new Vector2(Position.X, solid.Top - EnemyHeight / 2f);
                IsGrounded = true;
            }
            else if (VelocityY < 0f)
            {
                Position = new Vector2(Position.X, solid.Bottom + EnemyHeight / 2f);
            }
            VelocityY = 0f;
            UpdateColliderBounds();
        }

        // ── Temp Ground ───────────────────────────────────────────────────────
        if (_solidRects.Count == 0)
        {
            float groundTopY = TempGroundY - EnemyHeight / 2f;
            if (Position.Y >= groundTopY)
            {
                Position   = new Vector2(Position.X, groundTopY);
                VelocityY  = 0f;
                IsGrounded = true;
            }
        }
    }

    private void UpdateColliderBounds()
    {
        if (_collider == null) return;
        _collider.Bounds = new Rectangle(
            (int)(Position.X - EnemyWidth  / 2f),
            (int)(Position.Y - EnemyHeight / 2f),
            EnemyWidth,
            EnemyHeight
        );
    }

    // ══════════════════════════════════════════════════════════════════════════
    // State Machine
    // ══════════════════════════════════════════════════════════════════════════

    private void ChangeState(E1eP4MKIIState newState)
    {
        if (State == newState) return;

        switch (newState)
        {
            case E1eP4MKIIState.Igniting:
                _explode1Timer = Explode1AnimDuration;
                break;
            case E1eP4MKIIState.Explode:
                _explode2Timer = Explode2AnimDuration;
                float explodeScale = DisplayScale * (KillZoneRange / KillZoneScaleReference);
                Scale = new Vector2(explodeScale, explodeScale);
                if (_player != null && Vector2.Distance(Position, _player.Position) <= KillZoneRange)
                    _player.Die();
                break;
            case E1eP4MKIIState.Dead:
                _deadTimer = DeadAnimDuration;
                break;
        }

        State = newState;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>ส่ง Player reference จาก Level เพื่อให้ Enemy ติดตาม</summary>
    public override void SetPlayer(Player player) => _player = player;

    /// <summary>ส่ง solid rectangles จาก Level (เหมือน Player.SetSolids)</summary>
    public override void SetSolids(List<Rectangle> solids) => _solidRects = solids;

    public override Rectangle ColliderBounds => _collider?.Bounds ?? Rectangle.Empty;

    /// <summary>เรียกจาก hazard/trap หรือ Player เมื่อต้องการกำจัด enemy</summary>
    public override void Die()
    {
        if (State == E1eP4MKIIState.Dead) return;
        VelocityX = 0f;
        VelocityY = 0f;
        ChangeState(E1eP4MKIIState.Dead);
    }
}

// ── Concrete BoxCollider สำหรับ Enemy ────────────────────────────────────────
internal sealed class E1eP4MKIIBoxCollider : BoxCollider { }
