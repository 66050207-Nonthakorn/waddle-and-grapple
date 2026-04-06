using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

// ── State Machine ─────────────────────────────────────────────────────────────
public enum Enemy3State
{
    Idle,
    Attacking,
    Dead,
}

// ─────────────────────────────────────────────────────────────────────────────

public class Enemy3 : GameObject
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
    public const float DisplayScale = 2f; // เปลี่ยนเป็น 2f พอทำ Level จริงเสร็จ

    // ── AI Ranges ─────────────────────────────────────────────────────────────
    public float DetectionRange { get; set; } = 400f; // ระยะมองเห็น player และระยะโยนค้อน

    // ── Combat ────────────────────────────────────────────────────────────────
    public float AttackCooldown { get; set; } = 1.2f;
    private float _attackTimer;

    // ── Attack Animation Duration ─────────────────────────────────────────────
    // 6 frames × 0.083 s — ปรับตามจำนวน frame จริงใน spritesheet
    private const float AttackAnimDuration = 4 * 0.10f + 1.0f;
    private float _attackAnimTimer;

    // ── Velocity ──────────────────────────────────────────────────────────────
    public float VelocityX;
    public float VelocityY;

    // ── Ground Status ─────────────────────────────────────────────────────────
    public bool IsGrounded      { get; set; }
    public int  FacingDirection { get; set; } = 1; // +1 = ขวา, -1 = ซ้าย

    // ── State Machine ─────────────────────────────────────────────────────────
    public Enemy3State State { get; private set; } = Enemy3State.Idle;

    // ── Spawn ────────────────────────────────────────────────────────
    private Vector2 _spawnPosition;

    // ── Scene Reference (สำหรับ spawn ThrowingHammer) ────────────────────────
    private Scene _scene;
    private int _hammerCount;

    // ── Player Reference ──────────────────────────────────────────────────────
    private Player _player;

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer   _spriteRenderer;
    private Animator         _animator;
    private Enemy3BoxCollider _collider;
    private List<Rectangle>  _solidRects = [];

    // ── Death ─────────────────────────────────────────────────────────────────
    // 7 frames × 0.13 s — ตรงกับ dead animation ที่ลงทะเบียนใน Initialize
    private const float DeadAnimDuration = 7 * 0.13f;
    private float _deadTimer;

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        _spawnPosition   = Position;

        Scale       = new Vector2(DisplayScale, DisplayScale);
        _animator   = AddComponent<Animator>();
        _spriteRenderer            = GetComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;

        // TODO: แทนที่ "Enemy/Enemy-SpriteSheet" ด้วย path spritesheet จริง
        //       และปรับ rows/columns/totalFrames ให้ตรงกับไฟล์
        var f = new AnimationFactory(
            ResourceManager.Instance.GetTexture("Enemy3-SpriteSheet"),
            rows: 3, columns: 6
        );

        _animator.AddAnimation("standing",   f.CreateFromRow(row: 0, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("attack",     f.CreateFromRow(row: 1, totalFrames: 4, frameDuration: 0.10f, isLooping: false));
        _animator.AddAnimation("dead",       f.CreateFromRow(row: 2, totalFrames: 6, frameDuration: 0.13f, isLooping: false));

        _animator.Play("standing");

        _collider = AddComponent<Enemy3BoxCollider>();
        UpdateColliderBounds();
    }

    // ── Update Loop ───────────────────────────────────────────────────────────
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        if (WorldTime.IsFrozen) return;

        // Dead: นับ timer รอ animation จบ แล้ว deactivate
        if (State == Enemy3State.Dead)
        {
            if (_deadTimer > 0f) _deadTimer -= dt;
            else base.Active = false;
            return;
        }

        // Cooldown / wait timers
        if (_attackTimer      > 0f) _attackTimer      -= dt;
        if (_attackAnimTimer  > 0f) _attackAnimTimer  -= dt;

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
            // ── Idle ─────────────────────────
            case Enemy3State.Idle:
                if (playerInSight)
                {
                    ChangeState(Enemy3State.Attacking);
                }
                break;

            // ── Attack ─────────────────────────────────────────
            case Enemy3State.Attacking:
                if (playerInSight && (distToPlayer <= DetectionRange))
                {
                    ThrowHammerAttack();
                }
                break;
        }
    }

    private void ThrowHammerAttack()
    {
        if (_attackTimer > 0f) return; // ยังอยู่ใน cooldown

        _attackTimer     = AttackCooldown;
        _attackAnimTimer = AttackAnimDuration;

        // เผชิญหน้ากับ player ก่อน attack
        FacingDirection = _player.Position.X > Position.X ? 1 : -1;

        SpawnHammer();
    }

    private void SpawnHammer()
    {
        if (_scene == null || _player == null) return;

        Vector2 dir = Vector2.Normalize(_player.Position - Position);

        var hammer = _scene.AddGameObject<ThrowingHammer>($"hammer_{_hammerCount++}");
        hammer.Position = Position;
        hammer.Setup(dir, _player, _solidRects);
        hammer.Initialize();
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
            case Enemy3State.Attacking:
                _animator.Play(_attackAnimTimer > 0f ? "attack" : "standing");
                break;
            case Enemy3State.Dead:
                _animator.Play("dead");
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

    private void ChangeState(Enemy3State newState)
    {
        if (State == newState) return;

        switch (newState)
        {
            case Enemy3State.Dead:
                _deadTimer = DeadAnimDuration;
                break;
        }

        State = newState;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Public API
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>ส่ง Scene reference เพื่อให้ Enemy spawn ThrowingHammer ได้</summary>
    public void SetScene(Scene scene) => _scene = scene;

    /// <summary>ส่ง Player reference จาก Level เพื่อให้ Enemy ติดตาม</summary>
    public void SetPlayer(Player player) => _player = player;

    /// <summary>ส่ง solid rectangles จาก Level (เหมือน Player.SetSolids)</summary>
    public void SetSolids(List<Rectangle> solids) => _solidRects = solids;

    public Rectangle ColliderBounds => _collider?.Bounds ?? Rectangle.Empty;

    /// <summary>เรียกจาก hazard/trap หรือ Player เมื่อต้องการกำจัด enemy</summary>
    public void Die()
    {
        if (State == Enemy3State.Dead) return;
        VelocityX = 0f;
        VelocityY = 0f;
        ChangeState(Enemy3State.Dead);
    }
}

// ── Concrete BoxCollider สำหรับ Enemy ────────────────────────────────────────
internal sealed class Enemy3BoxCollider : BoxCollider { }
