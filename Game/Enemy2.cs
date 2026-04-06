using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

// ── State Machine ─────────────────────────────────────────────────────────────
public enum Enemy2State
{
    Idle,
    Patrolling,
    Taunting,         // เล่น emote เมื่อเห็น player ก่อน chase
    Chasing,
    Attacking,
    FallingDown,      // ตกขอบ — อยู่กลางอากาศ
    GettingUp,        // แตะพื้นหลังตก — รอ animation จบก่อน resume
    ReturningToSpawn,
    Stunned,
    Blocking,
    Dead,
}

// ─────────────────────────────────────────────────────────────────────────────

public class Enemy2 : GameObject
{
    // ── Physics Constants ─────────────────────────────────────────────────────
    private const float Gravity      = 1200f;  // px/s²
    private const float MaxFallSpeed = 700f;   // px/s

    // ── Collider Size ─────────────────────────────────────────────────────────
    private const int EnemyWidth  = 40; // เปลี่ยนเป็น 80 พอทำ Level จริงเสร็จ
    private const int EnemyHeight = 60; // เปลี่ยนเป็น 120 พอทำ Level จริงเสร็จ

    // ── Temporary Ground (ลบเมื่อ tiles พร้อม) ───────────────────────────────
    private const float TempGroundY = 400f;

    // ── Sprite Scale ──────────────────────────────────────────────────────────
    public const float DisplayScale = 1f; // เปลี่ยนเป็น 2f พอทำ Level จริงเสร็จ

    // ── Movement Speeds ───────────────────────────────────────────────────────
    public float PatrolSpeed { get; set; } = 80f;
    public float ChaseSpeed  { get; set; } = 175f;
    public float ReturnSpeed { get; set; } = 200f;

    // ── AI Ranges ─────────────────────────────────────────────────────────────
    public float PatrolRadius   { get; set; } = 150f; // ระยะ patrol ซ้าย/ขวาจาก spawn
    public float DetectionRange { get; set; } = 250f; // ระยะมองเห็น player
    public float AttackRange    { get; set; } = 50f;  // ระยะ melee         เปลี่ยนเป็น 60
    public float LeashRange     { get; set; } = 400f; // ระยะสูงสุดก่อน return to spawn

    // ── Combat ────────────────────────────────────────────────────────────────
    public float AttackCooldown { get; set; } = 2f;
    private float _attackTimer;

    // ── Attack Animation Duration ─────────────────────────────────────────────
    // 6 frames × 0.083 s — ปรับตามจำนวน frame จริงใน spritesheet
    private const float AttackAnimDuration = 7 * 0.083f;
    private float _attackAnimTimer;

    // ── Velocity ──────────────────────────────────────────────────────────────
    public float VelocityX;
    public float VelocityY;

    // ── Ground Status ─────────────────────────────────────────────────────────
    public bool IsGrounded      { get; set; }
    public int  FacingDirection { get; set; } = 1; // +1 = ขวา, -1 = ซ้าย

    // ── State Machine ─────────────────────────────────────────────────────────
    public Enemy2State State { get; private set; } = Enemy2State.Idle;

    // ── Spawn / Patrol ────────────────────────────────────────────────────────
    private Vector2 _spawnPosition;
    private int     _patrolDirection = 1;

    // ── Player Reference ──────────────────────────────────────────────────────
    private Player _player;

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer   _spriteRenderer;
    private Animator         _animator;
    private Enemy2BoxCollider _collider;
    private List<Rectangle>  _solidRects = [];

    // ── Patrol Wait ───────────────────────────────────────────────────────────
    private const float PatrolWaitDuration = 3.5f;  // วินาทีหยุดที่ขอบ patrol ก่อนเดินต่อ
    private float _patrolWaitTimer;

    // ── Taunt ─────────────────────────────────────────────────────────────────
    // 7 frames × 0.083 s — ตรงกับ emote animation ที่ลงทะเบียนใน Initialize
    private const float TauntAnimDuration = 7 * 0.083f + 0.10f;
    private float _tauntTimer;

    // ── Getting Up ────────────────────────────────────────────────────────────
    // 4 frames × 0.10 s — ตรงกับ gettingup animation ที่ลงทะเบียนใน Initialize
    private const float GettingUpAnimDuration = 4 * 0.10f + 0.31f;
    private float _gettingUpTimer;

    // ── Death ─────────────────────────────────────────────────────────────────
    // 7 frames × 0.13 s — ตรงกับ dead animation ที่ลงทะเบียนใน Initialize
    private const float DeadAnimDuration = 7 * 0.13f;
    private float _deadTimer;

    // Stunned
    private const float StunnedAnimDuration = 5 * 0.10f * 10;
    private float _stunnedTimer;

    // Blocking pickaxe
    private const float BlockDamageAnimDuration = 1 * 1.0f;
    private float _blockDamageTimer;

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        _spawnPosition   = Position;
        _patrolDirection = 1;

        Scale       = new Vector2(DisplayScale, DisplayScale);
        _animator   = AddComponent<Animator>();
        _spriteRenderer            = GetComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;

        // TODO: แทนที่ "Enemy/Enemy-SpriteSheet" ด้วย path spritesheet จริง
        //       และปรับ rows/columns/totalFrames ให้ตรงกับไฟล์
        var f = new AnimationFactory(
            ResourceManager.Instance.GetTexture("Enemy2-SpriteSheet"),
            rows: 9, columns: 8
        );

        _animator.AddAnimation("standing",   f.CreateFromRow(row: 0, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("emote",      f.CreateFromRow(row: 1, totalFrames: 6, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("walk",       f.CreateFromRow(row: 2, totalFrames: 8, frameDuration: 0.089f));
        _animator.AddAnimation("run",        f.CreateFromRow(row: 2, totalFrames: 8, frameDuration: 0.060f));
        _animator.AddAnimation("attack",     f.CreateFromRow(row: 3, totalFrames: 6, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("block",      f.CreateFromRow(row: 4, totalFrames: 1, frameDuration: 1.0f));
        _animator.AddAnimation("dead",       f.CreateFromRow(row: 5, totalFrames: 7, frameDuration: 0.13f, isLooping: false));
        _animator.AddAnimation("freefall",   f.CreateFromRow(row: 6, totalFrames: 4, frameDuration: 0.083f));
        _animator.AddAnimation("stunned",    f.CreateFromRow(row: 7, totalFrames: 5, frameDuration: 0.10f));
        _animator.AddAnimation("gettingup",  f.CreateFromRow(row: 8, totalFrames: 4, frameDuration: 0.10f, isLooping: false));

        _animator.Play("standing");

        _collider = AddComponent<Enemy2BoxCollider>();
        UpdateColliderBounds();
    }

    // ── Update Loop ───────────────────────────────────────────────────────────
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (WorldTime.IsFrozen) return;

        // Dead: นับ timer รอ animation จบ แล้ว deactivate
        if (State == Enemy2State.Dead)
        {
            if (_deadTimer > 0f) _deadTimer -= dt;
            else base.Active = false;
            return;
        }

        // Cooldown / wait timers
        if (_attackTimer      > 0f) _attackTimer      -= dt;
        if (_attackAnimTimer  > 0f) _attackAnimTimer  -= dt;
        if (_patrolWaitTimer  > 0f) _patrolWaitTimer  -= dt;
        if (_tauntTimer       > 0f) _tauntTimer       -= dt;
        if (_gettingUpTimer   > 0f) _gettingUpTimer   -= dt;
        if (_stunnedTimer     > 0f) _stunnedTimer     -= dt;
        if (_blockDamageTimer > 0f) _blockDamageTimer -= dt;

        // AI decision → ตั้ง VelocityX
        UpdateAI();

        // Physics
        ApplyGravity(dt);
        MoveAndCollide(dt);
        HandleAirborneTransitions();

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

        float distToSpawn  = System.MathF.Abs(Position.X - _spawnPosition.X);
        float distToPlayer  = Vector2.Distance(Position, _player.Position);
        bool  playerInSight = CanSeePlayer(distToPlayer);

        // Leash: ออกไกลเกิน LeashRange → กลับ spawn (ไม่ interrupt ขณะกลางอากาศหรือลุกขึ้น)
        if (distToSpawn > LeashRange
            && State != Enemy2State.ReturningToSpawn
            && State != Enemy2State.FallingDown
            && State != Enemy2State.GettingUp)
        {
            ChangeState(Enemy2State.ReturningToSpawn);
        }

        switch (State)
        {
            // ── Idle: เริ่ม patrol ทันที ──────────────────────────────────────
            case Enemy2State.Idle:
                ChangeState(Enemy2State.Patrolling);
                break;

            // ── Patrol: เดินไปมาในรัศมี PatrolRadius ─────────────────────────
            case Enemy2State.Patrolling:
                if (playerInSight)
                {
                    ChangeState(Enemy2State.Taunting);
                    break;
                }
                HandlePatrol();
                break;

            // ── Taunting: หยุด หันหา player เล่น emote แล้วค่อย chase ─────────
            case Enemy2State.Taunting:
                VelocityX = 0f;
                FacingDirection = _player.Position.X > Position.X ? 1 : -1;
                if (_tauntTimer <= 0f)
                    ChangeState(Enemy2State.Chasing);
                break;

            // ── Chase: วิ่งตาม player ─────────────────────────────────────────
            case Enemy2State.Chasing:
                if (!playerInSight)
                {
                    // ยังอยู่ในเขต patrol → กลับ patrol; ออกนอกเขต → คืน spawn
                    ChangeState(distToSpawn <= PatrolRadius
                        ? Enemy2State.Patrolling
                        : Enemy2State.ReturningToSpawn);
                    break;
                }
                if (distToPlayer <= AttackRange)
                {
                    TryMeleeAttack();
                    break;
                }
                ChasePlayer();
                break;

            // ── Attacking: หยุดนิ่ง รอ animation จบ ─────────────────────────
            case Enemy2State.Attacking:
                VelocityX = 0f;
                if (_attackAnimTimer <= 0f)
                    ChangeState(playerInSight ? Enemy2State.Chasing : Enemy2State.Patrolling);
                break;

            // ── Falling: physics จัดการ ไม่ควบคุม horizontal ──────────────────
            case Enemy2State.FallingDown:
                VelocityX = 0f;
                break;

            // ── Getting Up: หยุดนิ่ง รอ animation จบ → กลับ patrol ───────────
            case Enemy2State.GettingUp:
                VelocityX = 0f;
                if (_gettingUpTimer <= 0f)
                    ChangeState(Enemy2State.Patrolling);
                break;

            // ── Return to Spawn ───────────────────────────────────────────────
            case Enemy2State.ReturningToSpawn:
                HandleReturnToSpawn();
                if (distToSpawn < 10f)
                {
                    VelocityX = 0f;
                    ChangeState(Enemy2State.Patrolling);
                }
                break;

            // Stunned
            case Enemy2State.Stunned:
                VelocityX = 0f;
                if (_stunnedTimer <= 0f)
                    ChangeState(Enemy2State.Patrolling);
                break;

            // Blocking damage
            case Enemy2State.Blocking:
                VelocityX = 0f;
                if (_blockDamageTimer <= 0f)
                    ChangeState(Enemy2State.Chasing);
                break;
        }
    }

    private void HandlePatrol()
    {
        // กำลังรอที่ขอบ patrol — หยุดนิ่ง รอ timer หมด
        if (_patrolWaitTimer > 0f)
        {
            VelocityX = 0f;
            return;
        }

        float patrolLeft  = _spawnPosition.X - PatrolRadius;
        float patrolRight = _spawnPosition.X + PatrolRadius;

        // ถึงขอบ → สลับทิศและตั้ง wait timer
        if (Position.X <= patrolLeft && _patrolDirection == -1)
        {
            _patrolDirection = 1;
            _patrolWaitTimer = PatrolWaitDuration;
            VelocityX = 0f;
            return;
        }
        if (Position.X >= patrolRight && _patrolDirection == 1)
        {
            _patrolDirection = -1;
            _patrolWaitTimer = PatrolWaitDuration;
            VelocityX = 0f;
            return;
        }

        FacingDirection = _patrolDirection;
        VelocityX       = _patrolDirection * PatrolSpeed;
    }

    private void ChasePlayer()
    {
        float dir = _player.Position.X > Position.X ? 1f : -1f;
        FacingDirection = (int)dir;
        VelocityX       = dir * ChaseSpeed;
    }

    private void TryMeleeAttack()
    {
        if (_attackTimer > 0f) return; // ยังอยู่ใน cooldown

        _attackTimer     = AttackCooldown;
        _attackAnimTimer = AttackAnimDuration;
        ChangeState(Enemy2State.Attacking);

        // เผชิญหน้ากับ player ก่อน attack
        FacingDirection = _player.Position.X > Position.X ? 1 : -1;

        _player.Die();
    }

    private void HandleReturnToSpawn()
    {
        float dir = _spawnPosition.X > Position.X ? 1f : -1f;
        FacingDirection = (int)dir;
        VelocityX       = dir * ReturnSpeed;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Line-of-Sight Raycast
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// คืน true ถ้าศัตรูมองเห็น player:
    ///   1. player อยู่ในหน้าที่ศัตรูหัน (FacingDirection)
    ///   2. ระยะไม่เกิน DetectionRange
    ///   3. ไม่มี solid tile บัง line segment ระหว่างศัตรู → player
    /// </summary>
    private bool CanSeePlayer(float distToPlayer)
    {
        if (distToPlayer > DetectionRange) return false;

        // ตรวจว่า player อยู่ด้านที่ศัตรูหัน
        float dirToPlayer = _player.Position.X - Position.X;
        if (FacingDirection * dirToPlayer < 0f) return false;

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
            case Enemy2State.Patrolling:
                _animator.Play(_patrolWaitTimer > 0f ? "standing" : "walk");
                break;
            case Enemy2State.Taunting:
                _animator.Play("emote");
                break;
            case Enemy2State.ReturningToSpawn:
            case Enemy2State.Chasing:
                _animator.Play("run");
                break;
            case Enemy2State.Attacking:
                _animator.Play("attack");
                break;
            case Enemy2State.FallingDown:
                _animator.Play("freefall");
                break;
            case Enemy2State.GettingUp:
                _animator.Play("gettingup");
                break;
            case Enemy2State.Dead:
                _animator.Play("dead");
                break;
            case Enemy2State.Stunned:
                _animator.Play("stunned");
                break;
            case Enemy2State.Blocking:
                _animator.Play("block");
                break;
            default:
                _animator.Play("standing");
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Airborne Transitions (เรียกหลัง MoveAndCollide ทุก frame)
    // ══════════════════════════════════════════════════════════════════════════

    private void HandleAirborneTransitions()
    {
        switch (State)
        {
            // เดินตกขอบ → เข้า FallingDown
            case Enemy2State.Patrolling:
            case Enemy2State.Chasing:
            case Enemy2State.ReturningToSpawn:
                if (!IsGrounded)
                    ChangeState(Enemy2State.FallingDown);
                break;

            // แตะพื้นหลังตก → เข้า GettingUp
            case Enemy2State.FallingDown:
                if (IsGrounded)
                    ChangeState(Enemy2State.GettingUp);
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Physics (เหมือน Player)
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplyGravity(float dt)
    {
        VelocityY += Gravity * dt;
        if (VelocityY > MaxFallSpeed) VelocityY = MaxFallSpeed;
    }

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
                // ชนผนังขณะ patrol → สลับทิศ
                if (State == Enemy2State.Patrolling) _patrolDirection = -1;
            }
            else if (VelocityX < 0f)
            {
                Position = new Vector2(solid.Right + EnemyWidth / 2f, Position.Y);
                if (State == Enemy2State.Patrolling) _patrolDirection = 1;
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

    private void ChangeState(Enemy2State newState)
    {
        if (State == newState) return;

        switch (newState)
        {
            case Enemy2State.Patrolling:
                _patrolWaitTimer = 0f;
                break;
            case Enemy2State.Taunting:
                _tauntTimer = TauntAnimDuration;
                break;
            case Enemy2State.GettingUp:
                _gettingUpTimer = GettingUpAnimDuration;
                break;
            case Enemy2State.Dead:
                _deadTimer = DeadAnimDuration;
                break;
            case Enemy2State.Stunned:
                _stunnedTimer = StunnedAnimDuration;
                break;
            case Enemy2State.Blocking:
                _blockDamageTimer = BlockDamageAnimDuration;
                break;
        }

        State = newState;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>ส่ง Player reference จาก Level เพื่อให้ Enemy ติดตาม</summary>
    public void SetPlayer(Player player) => _player = player;

    /// <summary>ส่ง solid rectangles จาก Level (เหมือน Player.SetSolids)</summary>
    public void SetSolids(List<Rectangle> solids) => _solidRects = solids;

    public Rectangle ColliderBounds => _collider?.Bounds ?? Rectangle.Empty;

    /// <summary>เรียกจาก hazard/trap หรือ Player เมื่อต้องการกำจัด enemy</summary>
    public void Die()
    {
        if (State == Enemy2State.Dead) return;
        VelocityX = 0f;
        VelocityY = 0f;
        ChangeState(Enemy2State.Dead);
    }

    public void Stun()
    {
        if (State == Enemy2State.Stunned) return;
        ChangeState(Enemy2State.Stunned);
    }

    public void BlockDamage()
    {
        if (State == Enemy2State.Blocking) return;
        ChangeState(Enemy2State.Blocking);
    }
}

// ── Concrete BoxCollider สำหรับ Enemy ────────────────────────────────────────
internal sealed class Enemy2BoxCollider : BoxCollider { }
