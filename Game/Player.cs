using System;
using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace WaddleAndGrapple.Game;

// ── Phase 1.1: State Machine ──────────────────────────────────────────────────
public enum PlayerState
{
    Idle,
    Running,
    Sprinting,
    Jumping,
    Falling,
    WallClinging,
    LedgeGrabbing,
    Crouching,
    Sliding,
    Dead,
}

// ─────────────────────────────────────────────────────────────────────────────

public class Player : GameObject
{
    // ── Physics Constants ─────────────────────────────────────────────────────
    public const float Gravity          = 1200f;  // px/s²
    public const float MaxFallSpeed     = 700f;   // px/s
    public const float JumpForce        = -550f;  // px/s (ลบ = ขึ้น)
    public const float SprintMultiplier = 1.6f;
    public const float SlideSpeed       = 420f;   // px/s
    public const float SlideDuration    = 0.45f;  // วินาที
    public const float WallSlideSpeed   = 60f;    // px/s

    // ── Collider Size (placeholder — ปรับเมื่อได้ sprite จริง) ────────────────
    private const int PlayerWidth  = 40;
    private const int PlayerHeight = 60;

    // ── Temporary Ground (ลบเมื่อ Member 4 ส่ง tiles มา) ─────────────────────
    private const float TempGroundY = 400f;

    // ── Phase 7: Death ────────────────────────────────────────────────────────
    private const float FallDeathY      = 480f;   // ขอบล่างหน้าจอ → ตาย
    private const float ScreenLeft      = 0f;     // ขอบซ้าย
    private const float ScreenRight     = 2400f;  // ขอบขวาของ demo scene
    private const float RespawnDelaySec = 1.5f;   // วินาทีก่อน respawn

    // ── Velocity ──────────────────────────────────────────────────────────────
    public float VelocityX;
    public float VelocityY;

    // ── Ground / Wall / Ledge Status ─────────────────────────────────────────
    public bool IsGrounded          { get; set; }
    public bool IsTouchingWallLeft  { get; set; }
    public bool IsTouchingWallRight { get; set; }
    public bool IsAtLedge           { get; set; }
    public int  FacingDirection     { get; set; } = 1; // +1 = ขวา, -1 = ซ้าย

    // ── State Machine ─────────────────────────────────────────────────────────
    public PlayerState State { get; private set; } = PlayerState.Idle;

    // ── PowerUp Flags ─────────────────────────────────────────────────────────
    public bool HasDoubleJump     { get; set; }
    public bool HasUsedDoubleJump { get; set; }

    // ── Stats ─────────────────────────────────────────────────────────────────
    public float MoveSpeed { get; set; } = 200f;

    // ── Slide Timer ───────────────────────────────────────────────────────────
    private float _slideTimer;

    // ── Coyote Time (กระโดดได้แม้เพิ่งออกจากพื้นไปไม่กี่ frame) ─────────────
    private float _coyoteTimer;
    private const float CoyoteTime = 0.1f;

    // ── Dynamic Collider Height (ลดครึ่งหนึ่งตอน Crouching/Sliding) ──────────
    private int _currentHeight = PlayerHeight;

    // ── Coin Count ────────────────────────────────────────────────────────────
    public int CoinCount { get; private set; }

    // ── Active PowerUp Effects ────────────────────────────────────────────────
    private readonly List<PowerUp> _activeEffects = new();

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer _spriteRenderer;
    // TODO (Phase 9): private Animator _animator;

    // ── Collider & Solid Rectangles ───────────────────────────────────────────
    private PlayerBoxCollider _collider;
    private List<Rectangle>   _solidRects = [];

    // ── Phase 7: Death / Respawn ──────────────────────────────────────────────
    private Vector2         _spawnPosition;
    private float           _respawnTimer;
    private List<Rectangle> _hazardRects = [];

    // ── Phase 8: Checkpoints ──────────────────────────────────────────────────
    private List<CheckpointData> _checkpoints = [];

    // อ่านได้จาก Collectible เพื่อตรวจ overlap
    public Rectangle ColliderBounds => _collider?.Bounds ?? Rectangle.Empty;

    // ── Rope Dash ─────────────────────────────────────────────────────────────
    private bool _isRopeDashing = false;

    // ── IcePickaxe ────────────────────────────────────────────────────────────
    public IcePickaxe Pickaxe { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        _spriteRenderer            = AddComponent<SpriteRenderer>();
        _spriteRenderer.Texture    = ResourceManager.Instance.GetTexture("pixel");
        _spriteRenderer.Tint       = Color.Cyan;
        _spriteRenderer.LayerDepth = 0.5f;
        _spriteRenderer.Origin     = new Vector2(0.5f, 0.5f); // center บน texture 1×1
        Scale = new Vector2(PlayerWidth, PlayerHeight); // 40×60 px
        // TODO (Phase 9): _animator = AddComponent<Animator>();

        _collider = AddComponent<PlayerBoxCollider>();
        UpdateColliderBounds();

        Pickaxe = new IcePickaxe(this);
        Pickaxe.Initialize();

        var pickaxeRenderer = AddComponent<PickaxeRenderer>();
        pickaxeRenderer.Setup(this, Pickaxe);
    }

    // ── Update Loop ───────────────────────────────────────────────────────────
    // ลำดับสำคัญ:
    //   1. Input (Phase 3) → เซ็ต VelocityX/Y intent
    //   2. ApplyGravity    → แก้ VelocityY ตาม state
    //   3. MoveAndCollide  → ขยับ Position + resolve collision + update flags
    //   4. Transition      → Jumping → Falling, reset DoubleJump
    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // Phase 7 — Respawn timer (ทำงานแม้ตาย)
        if (State == PlayerState.Dead)
        {
            _respawnTimer -= dt;
            if (_respawnTimer <= 0f) Respawn();
            return;
        }

        // Phase 7 — Screen boundary death (ชนขอบจอ → ตาย)
        if (Position.Y > FallDeathY || Position.X < ScreenLeft || Position.X > ScreenRight)
        {
            Die();
            return;
        }

        // Coyote time — reset เมื่ออยู่บนพื้น, นับถอยหลังเมื่ออยู่ในอากาศ
        if (IsGrounded) _coyoteTimer = CoyoteTime;
        else if (_coyoteTimer > 0f) _coyoteTimer -= dt;

        // Phase 3 — Input (ต้องก่อน physics เพื่อให้ intent ถูก)
        HandleSlide(dt);
        HandleCrouch();
        HandleMove();
        if (Pickaxe.IsHooked) HandleRopeLaunch();
        else                { _isRopeDashing = false; HandleSprint(); }
        HandleWallCling();
        HandleLedgeGrab();
        HandleJump();

        // Phase 4 — Pickaxe input + flight (ก่อน physics)
        Pickaxe.HandleInput(dt);

        // Phase 2 — Physics
        ApplyGravity(dt);
        MoveAndCollide(dt);
        CheckLedge();

        // Phase 4 — Rope constraint (หลัง move เพื่อ correct position)
        Pickaxe.ApplyConstraint();

        // Auto transitions
        if (!IsGrounded && State == PlayerState.Jumping && VelocityY > 0f)
            ChangeState(PlayerState.Falling);
        if (IsGrounded)
            HasUsedDoubleJump = false;

        // Phase 7 — Hazard collision (static traps จาก Member 3/4)
        CheckHazardCollision();

        // Phase 8 — Checkpoint detection
        CheckCheckpoint();
        CheckpointManager.Instance.UpdateSection(Position.X);
        // Phase 9: SyncAnimation()

        UpdateActiveEffects(dt);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 3: Input Methods
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// A/D (หรือ ←/→): เดิน, อัปเดต FacingDirection และ state (Idle/Running)
    /// ข้ามถ้า Sliding หรือ LedgeGrabbing เพราะ state นั้นควบคุม VelocityX เอง
    /// </summary>
    private void HandleMove()
    {
        if (State == PlayerState.Sliding || State == PlayerState.LedgeGrabbing) return;

        bool left  = InputManager.Instance.IsKeyDown(Keys.A) || InputManager.Instance.IsKeyDown(Keys.Left);
        bool right = InputManager.Instance.IsKeyDown(Keys.D) || InputManager.Instance.IsKeyDown(Keys.Right);

        if (left && !right)
        {
            VelocityX       = -MoveSpeed;
            FacingDirection = -1;
        }
        else if (right && !left)
        {
            VelocityX       = MoveSpeed;
            FacingDirection = 1;
        }
        else
        {
            VelocityX = 0f;
        }

        // TODO (Phase 9): flip sprite
        // _spriteRenderer.Effects = FacingDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

        // อัปเดต state เมื่ออยู่บนพื้น
        if (IsGrounded && State != PlayerState.Crouching)
            ChangeState(VelocityX == 0f ? PlayerState.Idle : PlayerState.Running);

        // เข้า Falling ถ้าเดินออกจากขอบโดยไม่ได้กระโดด
        else if (!IsGrounded
              && State != PlayerState.WallClinging
              && State != PlayerState.LedgeGrabbing
              && State != PlayerState.Jumping
              && VelocityY > 0f)
            ChangeState(PlayerState.Falling);
    }

    /// <summary>
    /// Shift + A/D: วิ่งเร็ว — คูณ VelocityX ด้วย SprintMultiplier
    /// เรียกหลัง HandleMove เสมอ เพื่อให้ VelocityX ตั้งค่าแล้ว
    /// </summary>
    private void HandleSprint()
    {
        if (State == PlayerState.Sliding
         || State == PlayerState.LedgeGrabbing
         || State == PlayerState.Crouching) return;

        bool shiftHeld = InputManager.Instance.IsKeyDown(Keys.LeftShift)
                      || InputManager.Instance.IsKeyDown(Keys.RightShift);
        bool moving    = VelocityX != 0f;

        if (shiftHeld && moving)
        {
            VelocityX *= SprintMultiplier;
            if (IsGrounded) ChangeState(PlayerState.Sprinting);
        }
    }

    /// <summary>
    /// Left Shift ขณะแขวนเชือก: พุ่งตรงไปยัง hook point
    /// ใช้ทิศทาง Player → HookPosition เป็น launch vector
    /// </summary>
    private void HandleRopeLaunch()
    {
        // เริ่ม dash เมื่อกด Left Shift ขณะแขวนเชือก
        if (!_isRopeDashing)
        {
            if (!InputManager.Instance.IsKeyDown(Keys.LeftShift)) return;
            _isRopeDashing = true;
        }

        // ดึงตัวเองตรงไปยัง hook ตลอดเวลา (เชือกยังอยู่)
        var dir  = Pickaxe.HookPosition - Position;
        float dist = dir.Length();

        if (dist < 20f)
        {
            // ถึง hook แล้ว → recall แล้วพุ่งต่อด้วย momentum เดิม
            _isRopeDashing = false;
            Pickaxe.Recall();
            ChangeState(PlayerState.Jumping);
            return;
        }

        dir.Normalize();
        const float DashSpeed = 900f;
        VelocityX = dir.X * DashSpeed;
        VelocityY = dir.Y * DashSpeed;
        ChangeState(PlayerState.Jumping);
    }

    /// <summary>
    /// Space: กระโดด 4 แบบ —
    ///   Ground Jump, Wall Jump (kick away), Ledge Jump, Double Jump (PowerUp)
    /// </summary>
    private void HandleJump()
    {
        if (!InputManager.Instance.IsKeyPressed(Keys.Space)) return;

        if (IsGrounded || _coyoteTimer > 0f)
        {
            if (State == PlayerState.Crouching) SetCrouchHeight(false); // reset ก่อน jump
            VelocityY    = JumpForce;
            _coyoteTimer = 0f;
            ChangeState(PlayerState.Jumping);
        }
        else if (State == PlayerState.WallClinging)
        {
            // Kick ออกจากผนัง
            VelocityY       = JumpForce;
            VelocityX       = -FacingDirection * MoveSpeed * 1.2f;
            FacingDirection = -FacingDirection;
            ChangeState(PlayerState.Jumping);
        }
        else if (State == PlayerState.LedgeGrabbing)
        {
            VelocityY = JumpForce;
            ChangeState(PlayerState.Jumping);
        }
        else if (Pickaxe.IsHooked)
        {
            // กระโดดออกจาก rope พร้อม jump force
            Pickaxe.Recall();
            VelocityY = JumpForce;
            ChangeState(PlayerState.Jumping);
        }
        else if (HasDoubleJump && !HasUsedDoubleJump)
        {
            VelocityY         = JumpForce;
            HasUsedDoubleJump = true;
            ChangeState(PlayerState.Jumping);
        }
    }

    /// <summary>
    /// ตรวจและอัปเดต Wall Cling state อัตโนมัติ:
    ///   เข้า  → ต้องอยู่ในอากาศ + ชนผนัง + กดทิศทางเข้าผนัง
    ///   ออก   → แตะพื้น, ไม่ชนผนังแล้ว, หรือปล่อยปุ่ม
    /// </summary>
    private void HandleWallCling()
    {
        // ไม่ wall-cling ขณะแกว่งบน rope
        if (Pickaxe.IsHooked) return;

        bool touchingWall      = IsTouchingWallLeft || IsTouchingWallRight;
        bool pressingLeft      = InputManager.Instance.IsKeyDown(Keys.A) || InputManager.Instance.IsKeyDown(Keys.Left);
        bool pressingRight     = InputManager.Instance.IsKeyDown(Keys.D) || InputManager.Instance.IsKeyDown(Keys.Right);
        bool pressingTowardWall = (IsTouchingWallLeft  && pressingLeft)
                               || (IsTouchingWallRight && pressingRight);

        if (!IsGrounded && touchingWall && pressingTowardWall
            && State != PlayerState.LedgeGrabbing)
        {
            ChangeState(PlayerState.WallClinging);
        }
        else if (State == PlayerState.WallClinging)
        {
            if      (IsGrounded)                   ChangeState(PlayerState.Idle);
            else if (!touchingWall || !pressingTowardWall) ChangeState(PlayerState.Falling);
        }
    }

    /// <summary>
    /// S / ↓: นั่งยองขณะอยู่บนพื้น
    ///   เข้า Crouching → ลดความสูง collider ครึ่งหนึ่ง (ขอบล่างคงที่)
    ///   ออก Crouching  → คืน collider ปกติ
    /// </summary>
    private void HandleCrouch()
    {
        bool crouchHeld = InputManager.Instance.IsKeyDown(Keys.S)
                       || InputManager.Instance.IsKeyDown(Keys.Down);

        // ถ้าไม่กด S แต่ height ยังเป็น crouch → restore เสมอ (source of truth คือ _currentHeight)
        if (!crouchHeld && _currentHeight != PlayerHeight && State != PlayerState.Sliding)
        {
            SetCrouchHeight(false);
            if (State == PlayerState.Crouching)
                ChangeState(VelocityX == 0f ? PlayerState.Idle : PlayerState.Running);
            return;
        }

        if (!IsGrounded || State == PlayerState.Sliding) return;

        if (crouchHeld && _currentHeight == PlayerHeight && State != PlayerState.Crouching)
        {
            SetCrouchHeight(true);
            ChangeState(PlayerState.Crouching);
        }
    }

    /// <summary>
    /// Shift+S หรือ Sprint+S: สไลด์ไปข้างหน้าด้วยความเร็วสูง
    ///   ระหว่าง Slide: VelocityX = SlideSpeed (ทิศตาม FacingDirection), นับ timer
    ///   หมดเวลา: กลับ Crouching (ถ้ายัง hold S) หรือ Idle
    ///   Interaction กับ TankElephant: stub — รอ Member 3
    /// </summary>
    private void HandleSlide(float dt)
    {
        // ── Trigger ──────────────────────────────────────────────────────────
        bool crouchPressed = InputManager.Instance.IsKeyPressed(Keys.S)
                          || InputManager.Instance.IsKeyPressed(Keys.Down);
        bool shiftHeld     = InputManager.Instance.IsKeyDown(Keys.LeftShift)
                          || InputManager.Instance.IsKeyDown(Keys.RightShift);
        bool canSlide      = IsGrounded && crouchPressed
                          && (shiftHeld || State == PlayerState.Sprinting);

        if (canSlide && State != PlayerState.Sliding)
        {
            SetCrouchHeight(true);
            _slideTimer = SlideDuration;
            ChangeState(PlayerState.Sliding);
        }

        if (State != PlayerState.Sliding) return;

        // ── During Slide ──────────────────────────────────────────────────────
        VelocityX    = FacingDirection * SlideSpeed;
        _slideTimer -= dt;

        // TODO (Phase 3 → Member 3): TankElephant interaction
        //   ตรวจ _collider.Bounds ชน TankElephant.Collider.Bounds
        //   ถ้า Player อยู่ต่ำกว่า (slide under the legs) → tankElephant.GetStunned()

        // ── Slide End ─────────────────────────────────────────────────────────
        if (_slideTimer > 0f && IsGrounded) return;

        bool stillCrouching = InputManager.Instance.IsKeyDown(Keys.S)
                           || InputManager.Instance.IsKeyDown(Keys.Down);
        if (stillCrouching)
        {
            ChangeState(PlayerState.Crouching); // ยังนิ้วอยู่ → นั่งยอง
        }
        else
        {
            SetCrouchHeight(false);
            ChangeState(PlayerState.Idle);
        }
    }

    /// <summary>
    /// ขณะ LedgeGrabbing:
    ///   S / ↓  → ปล่อยตัวลง (Falling)
    ///   Space  → Ledge Jump (จัดการใน HandleJump แล้ว)
    /// </summary>
    private void HandleLedgeGrab()
    {
        if (State != PlayerState.LedgeGrabbing) return;

        bool drop = InputManager.Instance.IsKeyDown(Keys.S)
                 || InputManager.Instance.IsKeyDown(Keys.Down);
        if (drop)
            ChangeState(PlayerState.Falling);
    }

    // ── Helper: Crouch Height ─────────────────────────────────────────────────

    /// <summary>
    /// เปลี่ยนความสูง collider ระหว่างยืน ↔ นั่งยอง
    /// ขอบล่างของ collider ไม่เปลี่ยน (Player ไม่ "ลอย" ขึ้นตอนนั่ง)
    /// </summary>
    private void SetCrouchHeight(bool crouching)
    {
        float bottomY  = Position.Y + _currentHeight / 2f; // เก็บขอบล่างไว้ก่อน
        _currentHeight = crouching ? PlayerHeight / 2 : PlayerHeight;
        Position       = new Vector2(Position.X, bottomY - _currentHeight / 2f);
        Scale          = new Vector2(PlayerWidth, _currentHeight); // sync sprite
        UpdateColliderBounds();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 2: Physics
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplyGravity(float dt)
    {
        switch (State)
        {
            case PlayerState.WallClinging:
                VelocityY = WallSlideSpeed;     // ไหลลงผนังช้าๆ
                break;
            case PlayerState.LedgeGrabbing:
                VelocityY = 0f;
                VelocityX = 0f;
                break;
            default:
                VelocityY += Gravity * dt;
                if (VelocityY > MaxFallSpeed) VelocityY = MaxFallSpeed;
                break;
        }
    }

    private void MoveAndCollide(float dt)
    {
        IsGrounded          = false;
        IsTouchingWallLeft  = false;
        IsTouchingWallRight = false;

        // ── Horizontal ────────────────────────────────────────────────────────
        Position = new Vector2(Position.X + VelocityX * dt, Position.Y);
        UpdateColliderBounds();

        foreach (var solid in _solidRects)
        {
            if (!_collider.Bounds.Intersects(solid)) continue;

            if (VelocityX > 0f)
            {
                Position = new Vector2(solid.Left - PlayerWidth / 2f, Position.Y);
                IsTouchingWallRight = true;
            }
            else if (VelocityX < 0f)
            {
                Position = new Vector2(solid.Right + PlayerWidth / 2f, Position.Y);
                IsTouchingWallLeft = true;
            }
            VelocityX = 0f;
            UpdateColliderBounds();
        }

        // ── Vertical ──────────────────────────────────────────────────────────
        Position = new Vector2(Position.X, Position.Y + VelocityY * dt);
        UpdateColliderBounds();

        foreach (var solid in _solidRects)
        {
            if (!_collider.Bounds.Intersects(solid)) continue;

            if (VelocityY > 0f)      // ตกลง → ชน ground
            {
                Position   = new Vector2(Position.X, solid.Top - _currentHeight / 2f);
                IsGrounded = true;
            }
            else if (VelocityY < 0f) // กระโดดขึ้น → ชน ceiling
            {
                Position = new Vector2(Position.X, solid.Bottom + _currentHeight / 2f);
            }
            VelocityY = 0f;
            UpdateColliderBounds();
        }

        // ── Temp Ground ───────────────────────────────────────────────────────
        if (_solidRects.Count == 0)
        {
            float groundTopY = TempGroundY - _currentHeight / 2f;
            if (Position.Y >= groundTopY)
            {
                Position   = new Vector2(Position.X, groundTopY);
                VelocityY  = 0f;
                IsGrounded = true;
            }
        }
    }

    private void CheckLedge()
    {
        // TODO (Phase 2 + Member 4): implement เมื่อ ledge data พร้อม
    }

    private void UpdateColliderBounds()
    {
        if (_collider == null) return;
        _collider.Bounds = new Rectangle(
            (int)(Position.X - PlayerWidth    / 2f),
            (int)(Position.Y - _currentHeight / 2f),
            PlayerWidth,
            _currentHeight
        );
    }

    // ── API สำหรับ Level (Member 4) ───────────────────────────────────────────
    public void SetSolids(List<Rectangle> solids) => _solidRects = solids;

    // ══════════════════════════════════════════════════════════════════════════
    // Phase 1: API (PowerUp / Coin / Death / State)
    // ══════════════════════════════════════════════════════════════════════════

    public void ApplyEffect(PowerUp effect)
    {
        _activeEffects.Add(effect);
        effect.Activate(this);
    }

    private void UpdateActiveEffects(float dt)
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            _activeEffects[i].UpdateEffect(this, dt);
            if (!_activeEffects[i].IsActive)
                _activeEffects.RemoveAt(i);
        }
    }

    public void AddCoin(int value) => CoinCount += value;

    public void Die()
    {
        if (State == PlayerState.Dead) return;
        VelocityX     = 0f;
        VelocityY     = 0f;
        _respawnTimer = RespawnDelaySec;
        Pickaxe.Recall();
        ChangeState(PlayerState.Dead);
        // TODO (Phase 9): play death animation
        // TODO (Phase 8): CheckpointManager.Instance.RespawnPlayer(this)
    }

    private void Respawn()
    {
        Position  = CheckpointManager.Instance.GetRespawnPosition(_spawnPosition);
        VelocityX = 0f;
        VelocityY = 0f;
        SetCrouchHeight(false); // คืน collider ขนาดปกติ
        ChangeState(PlayerState.Falling); // จะตกลงพื้นเอง
        // TODO (Phase 9): play respawn animation
    }

    private void CheckHazardCollision()
    {
        foreach (var hazard in _hazardRects)
        {
            if (_collider.Bounds.Intersects(hazard))
            {
                Die();
                return;
            }
        }
    }

    // ── Phase 8: Checkpoint ───────────────────────────────────────────────────

    private void CheckCheckpoint()
    {
        foreach (var cp in _checkpoints)
        {
            if (_collider.Bounds.Intersects(cp.Bounds))
            {
                SetSpawnPoint(cp.SpawnPoint);
                // TODO (Phase 8 + Member 4): CheckpointManager.Instance.ActivateCheckpoint(cp)
                return;
            }
        }
    }

    // ── API สำหรับ Checkpoint (Member 4) ─────────────────────────────────────

    public void SetSpawnPoint(Vector2 point) => _spawnPosition = point;

    /// <summary>
    /// ส่ง checkpoint data จาก Level
    /// เมื่อ Player เดินผ่าน checkpoint → _spawnPosition อัปเดต → respawn ที่นั่น
    /// </summary>
    public void SetCheckpoints(List<CheckpointData> checkpoints) => _checkpoints = checkpoints;

    // ── API สำหรับ Traps/Enemies (Member 3, Member 4) ────────────────────────
    /// <summary>
    /// ส่ง hazard rectangles ที่ทำให้ Player ตายเมื่อชน
    /// เรียกทุก frame หรือเมื่อ trap เคลื่อนที่ เพื่ออัปเดตตำแหน่ง
    /// </summary>
    public void SetHazards(List<Rectangle> hazards) => _hazardRects = hazards;

    public void ChangeState(PlayerState newState)
    {
        if (State == newState) return;
        State = newState;
        // TODO (Phase 9): _animator.Play(newState.ToString().ToLower())
    }
}

// ── Concrete BoxCollider สำหรับ Player ───────────────────────────────────────
internal sealed class PlayerBoxCollider : BoxCollider { }

/// <summary>
/// ข้อมูล Checkpoint ที่ Member 4 ส่งให้ Player
///   Bounds     — พื้นที่สัมผัส (Player เดินผ่าน)
///   SpawnPoint — จุด respawn เมื่อ activate checkpoint นี้แล้วตาย
/// </summary>
public readonly struct CheckpointData(Rectangle bounds, Vector2 spawnPoint)
{
    public readonly Rectangle Bounds     = bounds;
    public readonly Vector2   SpawnPoint = spawnPoint;
}
