using WaddleAndGrapple.Game.Systems;
using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
using Microsoft.Xna.Framework;
using System;

namespace WaddleAndGrapple.Game;

// ── State Machine ─────────────────────────────────────────────────────────────
public enum GruntLephantState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Stunned,
    Dead,
}

// ─────────────────────────────────────────────────────────────────────────────

public class GruntLephant : Enemy
{
    // ── Physics Constants ─────────────────────────────────────────────────────
    private const float Gravity      = 1200f;  // px/s²
    private const float MaxFallSpeed = 700f;   // px/s

    // ── Collider Size ─────────────────────────────────────────────────────────
    private const int EnemyWidth  = 48;
    private const int EnemyHeight = 64;

    // ── Sprite Scale ──────────────────────────────────────────────────────────
    public const float DisplayScale = 1f;

    // ── Movement Speeds ───────────────────────────────────────────────────────
    public float PatrolSpeed { get; set; } = 100f;
    public float ChaseSpeed  { get; set; } = 130f;

    // ── AI Ranges ─────────────────────────────────────────────────────────────
    public float PatrolRadius   { get; set; } = 150f; // ระยะ patrol ซ้าย/ขวาจาก spawn
    public float DetectionRange { get; set; } = 250f; // ระยะมองเห็น player
    public float AttackRange    { get; set; } = 50f;  // ระยะที่ attack ได้
    public float ChaseTolerance { get; set; } = 20f;  // tolerance เมื่อเข้าใกล้ player ในแกน X เพื่อไม่ให้ศัตรูหันซ้ายขวารัวๆ

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
    public GruntLephantState State { get; private set; } = GruntLephantState.Idle;

    // ── Spawn / Patrol ────────────────────────────────────────────────────────
    private Vector2 _spawnPosition;
    public int     _patrolDirection = 1;

    // ── Player Reference ──────────────────────────────────────────────────────
    private Player _player;

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer   _spriteRenderer;
    private Animator         _animator;
    private GruntLephantBoxCollider _collider;
    private List<Rectangle>  _solidRects = [];

    // ── Patrol Wait ───────────────────────────────────────────────────────────
    private const float PatrolWaitDuration = 3.5f;  // วินาทีหยุดที่ขอบ patrol ก่อนหันแล้วเดินต่อ
    private float _patrolWaitTimer;

    // ── Stunned ───────────────────────────────────────────────────────────
    private const float SlipAnimDuration      = 4 * 0.083f;      // 4 frames × 0.083s
    private const float KnockedOutDuration    = 3f;            // นอนค้างอยู่เป็นเวลา ... วินาที
    private const float GettingUpAnimDuration = 4 * 0.10f;       // 4 frames × 0.10s
    private float _stunnedTimer;
    private StunnedPhase _stunnedPhase = StunnedPhase.Slip;

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        _spawnPosition   = Position;
        // _patrolDirection = 1;

        Scale       = new Vector2(DisplayScale, DisplayScale);
        _animator   = AddComponent<Animator>();
        _spriteRenderer            = GetComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;

        var f = new AnimationFactory(
            ResourceManager.Instance.GetTexture("Enemy/Enemy1-SpriteSheet"),
            rows: 8, columns: 8
        );

        _animator.AddAnimation("standing",   f.CreateFromRow(row: 0, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("idle",       f.CreateFromRow(row: 1, totalFrames: 7, frameDuration: 0.16f));
        _animator.AddAnimation("walk",       f.CreateFromRow(row: 2, totalFrames: 8, frameDuration: 0.089f));
        _animator.AddAnimation("run",        f.CreateFromRow(row: 2, totalFrames: 8, frameDuration: 0.067f));
        _animator.AddAnimation("attack",     f.CreateFromRow(row: 3, totalFrames: 7, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("dead",       f.CreateFromRow(row: 4, totalFrames: 7, frameDuration: 0.13f, isLooping: false));
        _animator.AddAnimation("slip",       f.CreateFromRow(row: 5, totalFrames: 4, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("stunned",    f.CreateFromRow(row: 6, totalFrames: 5, frameDuration: 0.10f));
        _animator.AddAnimation("gettingup",  f.CreateFromRow(row: 7, totalFrames: 4, frameDuration: 0.10f, isLooping: false));

        _animator.UseBottomLeftAnchor = false;
        _spriteRenderer.DrawOffset    = Vector2.Zero;
        _animator.Play("standing");

        _collider = AddComponent<GruntLephantBoxCollider>();
        UpdateColliderBounds();
    }

    // ── Update Loop ───────────────────────────────────────────────────────────
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (WorldTime.IsFrozen) return;

        // Dead: นับ timer รอ animation จบ แล้ว deactivate
        if (State == GruntLephantState.Dead)
        {
            // dead animation จบแล้ว → ลบออกจาก scene
            if (_animator.IsCurrentAnimationFinished && SceneKey != null)
                SceneManager.Instance.CurrentScene.RemoveGameObject(SceneKey);
            return;
        }

        // Cooldown / wait timers
        if (_attackTimer      > 0f) _attackTimer      -= dt;
        if (_attackAnimTimer  > 0f) _attackAnimTimer  -= dt;
        if (_patrolWaitTimer  > 0f) _patrolWaitTimer  -= dt;
        if (_stunnedTimer     > 0f) _stunnedTimer     -= dt;

        // AI decision → ตั้ง VelocityX
        UpdateAI();

        // Physics
        ApplyGravity(dt);
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
            // ── Idle: เริ่ม patrol ทันที ──────────────────────────────────────
            case GruntLephantState.Idle:
                ChangeState(GruntLephantState.Patrolling);
                break;

            // ── Patrol: เดินไปมาในรัศมี PatrolRadius ─────────────────────────
            case GruntLephantState.Patrolling:
                if (playerInSight)
                {
                    ChangeState(GruntLephantState.Chasing);
                    break;
                }
                HandlePatrol();
                break;

            // ── Chase: วิ่งตาม player ─────────────────────────────────────────
            case GruntLephantState.Chasing:
                if (!playerInSight)
                {
                    ChangeState(GruntLephantState.Patrolling);
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
            case GruntLephantState.Attacking:
                VelocityX = 0f;
                if (_attackAnimTimer <= 0f)
                    ChangeState(playerInSight ? GruntLephantState.Chasing : GruntLephantState.Patrolling);
                break;

            // Stunned - Animation Sequence
            case GruntLephantState.Stunned:
                VelocityX = 0f;
                HandleStunnedSequence();
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

        // ตรวจว่ามีพื้นหน้าศัตรู หากไม่มี → หันกลับ
        if (!IsGroundAheadInDirection(_patrolDirection))
        {
            _patrolDirection *= -1;
            _patrolWaitTimer = PatrolWaitDuration;
            VelocityX = 0f;
            return;
        }

        FacingDirection = _patrolDirection;
        VelocityX       = _patrolDirection * PatrolSpeed;
    }

    private void ChasePlayer()
    {
        float dirToPlayer = _player.Position.X - Position.X;
        
        // ถ้าผู้เล่นอยู่ใกล้พอในแกน X → หยุดเดิน เพื่อไม่ให้ FacingDirection เปลี่ยนระหว่างซ้ายกับขวารัวๆ
        // FacingDirection ยังคงเป็นค่าเดิม
        if (Math.Abs(dirToPlayer) <= ChaseTolerance)
        {
            VelocityX = 0f;
            return;
        }

        float dir = dirToPlayer > 0f ? 1f : -1f;

        // ตรวจว่ามีพื้นหน้าศัตรูหรือไม่ หากไม่มีให้ยืนจ้องหน้าเฉยๆ
        if (!IsGroundAheadInDirection((int)dir))
        {
            VelocityX = 0f;
            return;
        }

        FacingDirection = (int)dir;
        VelocityX       = dir * ChaseSpeed;
    }

    /// <summary>
    /// จัดการ animation sequence สำหรับ knockout:
    /// Slip → KnockedOut (นอนค้าง) → GettingUp
    /// </summary>
    private void HandleStunnedSequence()
    {
        switch (_stunnedPhase)
        {
            case StunnedPhase.Slip:
                // อยู่ใน slip animation หรือจบแล้ว?
                if (_animator.IsCurrentAnimationFinished)
                {
                    _stunnedPhase = StunnedPhase.KnockedOut;
                    _stunnedTimer = KnockedOutDuration;
                }
                break;

            case StunnedPhase.KnockedOut:
                // นอนค้างและรอ timer หมด
                if (_stunnedTimer <= 0f)
                {
                    _stunnedPhase = StunnedPhase.GettingUp;
                    _stunnedTimer = GettingUpAnimDuration;
                }
                break;

            case StunnedPhase.GettingUp:
                // เล่น getting up animation จนจบ
                if (_animator.IsCurrentAnimationFinished)
                {
                    // ลุกขึ้นสำเร็จ → กลับไปเดิน
                    ChangeState(GruntLephantState.Patrolling);
                }
                break;
        }
    }

    /// <summary>
    /// ตรวจว่าหากเดินไปในทิศที่กำหนด จะมีพื้นรับตัวใต้ฟุตหรือไม่
    /// คืน true ถ้าเดินต่อได้ (มีพื้นหรือจะชนกำแพง)
    /// คืน false ถ้าจะตกลงไป (ไม่มีพื้น) → ต้องถอยหรือหันกลับ
    /// </summary>
    private bool IsGroundAheadInDirection(int direction)
    {
        // คำนวณตำแหน่ง "ฟุต" ของศัตรู (ล่างสุด) และเลื่อนไปข้างหน้า
        float checkX = Position.X + direction * (EnemyWidth / 2f + 5f); // 5px พอเพื่อ lookahead
        float checkY = Position.Y + EnemyHeight / 2f + 5f; // ต่ำกว่าฟุตเล็กน้อย

        // ตรวจทั้ง solid rectangles ว่ามีตัวไหนสนับสนุนศัตรู
        // (โดยเฉพาะตรวจว่ามีพื้นใต้ขา)
        foreach (var solid in _solidRects)
        {
            // ถ้า checkY อยู่ในช่องโล่ง (ไม่ชนเพดาน/พื้น) และ checkX อยู่บน solid
            // ← นั่นแปลว่ามี "พื้น" ให้ยืน
            if (solid.Left <= checkX && checkX <= solid.Right
                && solid.Top <= checkY && checkY <= solid.Bottom)
            {
                return true; // มีพื้นรับ
            }
        }

        return false; // ไม่มีพื้น → จะตกลงไป
    }

    private void TryMeleeAttack()
    {
        if (_attackTimer > 0f) return; // ยังอยู่ใน cooldown
        if (_player.State == PlayerState.Sliding) return; // player กำลังสไลด์ → ไม่โจมตี

        _attackTimer     = AttackCooldown;
        _attackAnimTimer = AttackAnimDuration;
        ChangeState(GruntLephantState.Attacking);

        // เผชิญหน้ากับ player ก่อน attack
        FacingDirection = _player.Position.X > Position.X ? 1 : -1;

        _player.Die();
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

        if (State == GruntLephantState.Patrolling) {
            // ตรวจว่า player อยู่ด้านที่ศัตรูหัน
            float dirToPlayer = _player.Position.X - Position.X;
            if (FacingDirection * dirToPlayer < 0f) return false;
        }

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
            case GruntLephantState.Patrolling:
                _animator.Play(_patrolWaitTimer > 0f ? "standing" : "walk");
                break;
            case GruntLephantState.Chasing:
                _animator.Play("run");
                break;
            case GruntLephantState.Attacking:
                _animator.Play("attack");
                break;
            case GruntLephantState.Dead:
                _animator.Play("dead");
                break;
            case GruntLephantState.Stunned:
                // เล่น animation sequence: slip → stunned → gettingup
                switch (_stunnedPhase)
                {
                    case StunnedPhase.Slip:
                        _animator.Play("slip");
                        break;
                    case StunnedPhase.KnockedOut:
                        _animator.Play("stunned");
                        break;
                    case StunnedPhase.GettingUp:
                        _animator.Play("gettingup");
                        break;
                }
                break;
            default:
                _animator.Play("standing");
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
                if (State == GruntLephantState.Patrolling) {
                    _patrolDirection = -1;
                    _patrolWaitTimer = PatrolWaitDuration;
                }
            }
            else if (VelocityX < 0f)
            {
                Position = new Vector2(solid.Right + EnemyWidth / 2f, Position.Y);
                if (State == GruntLephantState.Patrolling) {
                    _patrolDirection = 1;
                    _patrolWaitTimer = PatrolWaitDuration;
                }
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

    private void ChangeState(GruntLephantState newState)
    {
        if (State == newState) return;

        switch (newState)
        {
            case GruntLephantState.Patrolling:
                _patrolWaitTimer = 0f;
                break;
            case GruntLephantState.Stunned:
                // เริ่ม sequence: slip → knocked out → getting up
                _stunnedPhase = StunnedPhase.Slip;
                _stunnedTimer = SlipAnimDuration;
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
    public override bool IsAlive => State != GruntLephantState.Dead;

    /// <summary>key ที่ใช้ตอน AddGameObject — ตั้งจาก Level เพื่อให้ลบตัวเองออกจาก scene ได้</summary>

    /// <summary>รีเซ็ต enemy กลับไปยังตำแหน่ง spawn และเริ่ม patrol ใหม่</summary>
    public override void ResetToSpawn()
    {
        Position     = _spawnPosition;
        VelocityX    = 0f;
        VelocityY    = 0f;
        IsGrounded   = false;
        _patrolDirection = 1;
        State = GruntLephantState.Idle; // bypass ChangeState guard so Patrolling transition fires
        ChangeState(GruntLephantState.Patrolling);
        _animator.Play("walk");
    }

    /// <summary>เรียกจาก hazard/trap หรือ Player เมื่อต้องการกำจัด enemy</summary>
    public override void Die()
    {
        if (State == GruntLephantState.Dead) return;
        VelocityX = 0f;
        VelocityY = 0f;
        ChangeState(GruntLephantState.Dead);
        _animator.Play("dead");
    }

    public override void Stun()
    {
        if (State == GruntLephantState.Stunned) return;
        ChangeState(GruntLephantState.Stunned);
    }
}

// ── Concrete BoxCollider สำหรับ Enemy ────────────────────────────────────────
internal sealed class GruntLephantBoxCollider : BoxCollider { }
