using System;
using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.Utils;
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
    GoalReached,
    Launching,  // ดึงตัวเองไปตามเชือก (right-click ขณะ Hooked)
}

// ─────────────────────────────────────────────────────────────────────────────

public class Player : GameObject
{
    // ── Physics Constants ─────────────────────────────────────────────────────
    public const float Gravity          = 1200f;  // px/s²
    public const float MaxFallSpeed     = 350f;   // px/s
    public const float JumpForce        = -500f;  // px/s (ลบ = ขึ้น)
    public const float SprintMultiplier = 1.6f;
    public const float SlideSpeed       = 550f;   // px/s
    public const int   SlideLoopCount  = 2;       // ← เปลี่ยนตรงนี้เพื่อยืด/สั้นระยะสไลด์ (จำนวนรอบของ row 13)
    public const float SlideDuration   = 0.083f * 4f * (1 + SlideLoopCount); // slidestart 1× + row13 N×
    public const float WallSlideSpeed    = 60f;    // px/s
    public const float WallJumpXMultiplier = 2.8f;  // ตัวคูณแรงพุ่งแนวนอนตอน wall jump
    public const float WallJumpYMultiplier = 1.5f;  // ตัวคูณแรงพุ่งแนวตั้งตอน wall jump (> 1 = สูงขึ้น)
    public const float RopeLaunchSpeed  = 450f;   // ความเร็วดึงตัวเองไปตามเชือก (px/s)
    public const float SwingXBoost      = 600f;   // แรง X เพิ่มเติมตอนแกว่งเชือก (px/s²) ↑ = แกว่งแรงขึ้น

    // ── Collider Size (placeholder — ปรับเมื่อได้ sprite จริง) ────────────────
    private const int PlayerWidth  = 16;
    private const int PlayerHeight = 32;

    // ── Temp player should fit a 16x16 tile and use a 64×64 source frame scaled down.
    public const float DisplayScale = 1f;

    // ── Temporary Ground (ลบเมื่อ Member 4 ส่ง tiles มา) ─────────────────────
    private const float TempGroundY = 400f;

    // ── Phase 7: Death ────────────────────────────────────────────────────────
    private const float DefaultFallDeathY = 600f;   // ขอบล่างหน้าจอ → ตาย
    private const float DefaultScreenLeft = 0f;     // ขอบซ้าย
    private const float DefaultScreenRight = 4800f; // ขอบขวาของ demo scene
    private float _fallDeathY = DefaultFallDeathY;
    private float _screenLeft = DefaultScreenLeft;
    private float _screenRight = DefaultScreenRight;
    private const float RespawnDelaySec = 1.5f;   // วินาทีก่อน respawn

    // ── Velocity ──────────────────────────────────────────────────────────────
    public float VelocityX;
    public float VelocityY;

    // ── Ground / Wall / Ledge Status ─────────────────────────────────────────
    public bool IsGrounded          { get; set; }
    public bool IsTouchingWallLeft  { get; set; }
    public bool IsTouchingWallRight { get; set; }
    public bool IsTouchingCeiling   { get; set; }
    public bool IsAtLedge           { get; set; }
    public int  FacingDirection     { get; set; } = 1; // +1 = ขวา, -1 = ซ้าย

    // ── State Machine ─────────────────────────────────────────────────────────
    public PlayerState State { get; private set; } = PlayerState.Idle;

    // ── PowerUp Flags ─────────────────────────────────────────────────────────
    public bool HasDoubleJump     { get; set; }
    public bool HasUsedDoubleJump { get; set; }

    // ── Stats ─────────────────────────────────────────────────────────────────
    public float MoveSpeed  { get; set; } = 200f;
    public float SpeedScale { get; set; } = 0.5f;  // ← ปรับตรงนี้ใน Level เพื่อเปลี่ยนความเร็วโดยรวม (0.5 = ช้าลงครึ่ง)

    // ── Slide Timer ───────────────────────────────────────────────────────────
    private float _slideTimer;

    // ── Coyote Time (กระโดดได้แม้เพิ่งออกจากพื้นไปไม่กี่ frame) ─────────────
    private float _coyoteTimer;
    private const float CoyoteTime = 0.1f;

    // ── Dynamic Collider Height (ลดครึ่งหนึ่งตอน Crouching/Sliding) ──────────
    private int _currentHeight = PlayerHeight;

    // ── Fish Count ────────────────────────────────────────────────────────────
    public int FishCount { get; private set; }

    // ── Goal / Death Animation (row 14) ──────────────────────────────────────
    private const int   DeadAnimTotalFrames   = 6;    // ทุก frame ในแถว (ตายครบแถว)
    private const int   GoalAnimTotalFrames   = 4;    // แค่ 4 frame แรกตอน goal
    private const float GoalAnimFrameDuration = 0.1f;
    private const int   GoalAnimLoopsRequired = 3;
    public  bool IsGoalAnimationComplete { get; private set; }

    // ── Active PowerUp Effects ────────────────────────────────────────────────
    private readonly List<PowerUp> _activeEffects = new();
    public IReadOnlyList<PowerUp> ActiveEffects => _activeEffects;

    // ── Components ────────────────────────────────────────────────────────────
    private SpriteRenderer _spriteRenderer;
    private Animator       _animator;

    // ── Collider & Solid Rectangles ───────────────────────────────────────────
    private PlayerBoxCollider _collider;
    private List<Rectangle>   _solidRects = [];

    // ── Phase 7: Death / Respawn ──────────────────────────────────────────────
    private Vector2         _spawnPosition;
    private float           _respawnTimer;
    private List<Rectangle> _hazardRects = [];

    // ── Active Spritesheet ────────────────────────────────────────────────────
    private bool _speedSheetActive = false;
    private bool _slowSheetActive  = false;
    private string _lastSheet = "normal"; // "normal" | "speed" | "slow"

    // ── Phase 8: Checkpoints ──────────────────────────────────────────────────
    private List<CheckpointData> _checkpoints = [];

    // อ่านได้จาก Collectible เพื่อตรวจ overlap
    public Rectangle ColliderBounds => _collider?.Bounds ?? Rectangle.Empty;

    // ── Animation Timers ──────────────────────────────────────────────────────
    private float _idleTimer;
    private const float IdleBreathDelay   = 3f;    // วินาทีก่อนเล่น Breathe
    private float _jumpAnimTimer;                  // > 0 → เล่น jumpstart / walljumpstart
    private float _slideAnimTimer;                 // > 0 → เล่น slidestart
    private float _slideEndAnimTimer;              // > 0 → เล่น slideend (row 12 reversed)

    // ── Wall Jump Cooldown ────────────────────────────────────────────────────
    private const float WallJumpCooldown = 0.35f; // วินาทีที่ห้าม re-cling หลัง wall jump
    private float _wallJumpCooldownTimer = 0f;    // countdown
    private int   _wallJumpedFromSide    = 0;     // 1=เพิ่ง jump จากกำแพงขวา, -1=ซ้าย

    // ── Sprint (Shift+A/D) ────────────────────────────────────────────────────
    private int   _sprintDir       = 0;          // 0=ไม่ sprint, 1=ขวา, -1=ซ้าย
    private bool  _ropeHitWall     = false;      // true = เพิ่ง jump จากกำแพงตอน rope pull → auto-cling
    private float _ledgeReleaseCooldown = 0f;   // ห้าม re-grab หลังปล่อย/กระโดดจาก ledge
    private float _wallClingLockTimer  = 0f;    // หลัง kick เข้ากำแพงตรงข้าม: ห้าม pressingAway exit ชั่วคราว
    private float _jumpBufferTimer     = 0f;    // Space ถูกกดเมื่อกี้ — buffer ไว้ใช้ตอน wall cling
    private const float JumpBufferWindow = 0.2f; // window ที่ buffer jump input (วินาที)
    private bool  _wasGroundedPrev = false;      // debug: ป้องกัน [LAND] spam ทุก frame

    // ── Footstep Sound ────────────────────────────────────────────────────────
    private float _footstepTimer = 0f;
    private const float FootstepInterval = 0.32f; // วินาทีระหว่างเสียงก้าว

    // ── Rope Dash ─────────────────────────────────────────────────────────────
    private bool _isRopeDashing = false;

    // ── IcePickaxe ────────────────────────────────────────────────────────────
    public IcePickaxe Pickaxe { get; private set; }

    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        Scale    = new Vector2(DisplayScale, DisplayScale);
        _animator = AddComponent<Animator>();

        // Animator สร้าง SpriteRenderer ให้อัตโนมัติ — เซ็ต LayerDepth เพิ่ม
        _spriteRenderer            = GetComponent<SpriteRenderer>();
        _spriteRenderer.LayerDepth = 0.5f;

        // SpriteSheet เดียว: 14 rows × 9 columns, frame 64×64
        // row ตาม JSON: y/64
        // row 0=Standing, 1=Breathe, 2=Walk, 3=Crouch, 4=CrouchWalk,
        //     5=Running,  6=GroundJumpStartup, 7=Jumping, 8=Freefall,
        //     9=LedgeGrab, 10=WallSlide, 11=WallJumpStartup,
        //    12=SlideStartEnd, 13=SlideLoop
        RegisterAnimations("Player/Player-SpriteSheet");
        _animator.Play("standing");

        _collider = AddComponent<PlayerBoxCollider>();
        UpdateColliderBounds();

        Pickaxe = new IcePickaxe(this);
        Pickaxe.Initialize();
        Pickaxe.SetEnemies(_enemies); // forward enemies ที่อาจถูกตั้งไว้ก่อน Initialize

        var pickaxeRenderer = AddComponent<PickaxeRenderer>();
        pickaxeRenderer.Setup(this, Pickaxe);

        AddComponent<PowerUpBarRenderer>();
        AddComponent<FishHUD>();
    }

    // ── Spritesheet Helpers ───────────────────────────────────────────────────

    private void RegisterAnimations(string sheetName)
    {
        var f = new AnimationFactory(
            ResourceManager.Instance.GetTexture(sheetName),
            rows: 15, columns: 9
        );

        _animator.AddAnimation("standing",      f.CreateFromRow(row:  0, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("breathe",       f.CreateFromRow(row:  1, totalFrames: 7, frameDuration: 0.10f));
        _animator.AddAnimation("walk",          f.CreateFromRow(row:  2, totalFrames: 9, frameDuration: 0.083f));
        _animator.AddAnimation("crouch",        f.CreateFromRow(row:  3, totalFrames: 4, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("crouchwalk",    f.CreateFromRow(row:  4, totalFrames: 6, frameDuration: 0.10f));
        _animator.AddAnimation("run",           f.CreateFromRow(row:  5, totalFrames: 8, frameDuration: 0.083f));
        _animator.AddAnimation("jumpstart",     f.CreateFromRow(row:  6, totalFrames: 1, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("jump",          f.CreateFromRow(row:  7, totalFrames: 4, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("freefall",      f.CreateFromRow(row:  8, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("ledgegrab",     f.CreateFromRow(row:  9, totalFrames: 1, frameDuration: 0.083f));
        _animator.AddAnimation("wallslide",     f.CreateFromRow(row: 10, totalFrames: 4, frameDuration: 0.083f));
        _animator.AddAnimation("walljumpstart", f.CreateFromRow(row: 11, totalFrames: 3, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("slidestart",    f.CreateFromRow(row: 12, totalFrames: 4, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("slide",         f.CreateFromRow(row: 13, totalFrames: 4, frameDuration: 0.083f));
        _animator.AddAnimation("slideend",      f.CreateFromRowReversed(row: 12, totalFrames: 4, frameDuration: 0.083f, isLooping: false));
        _animator.AddAnimation("dead",          f.CreateFromRow(row: 14, totalFrames: DeadAnimTotalFrames, frameDuration: GoalAnimFrameDuration, isLooping: false));
        _animator.AddAnimation("goal",          f.CreateFromRow(row: 14, totalFrames: GoalAnimTotalFrames, frameDuration: GoalAnimFrameDuration));
    }

    /// <summary>เรียกจาก PowerUp.OnActivate — เปิดใช้ sheet ของไอเท็มนั้น</summary>
    public void SetActiveSheet(string sheetName)
    {
        if (sheetName == "speed") _speedSheetActive = true;
        if (sheetName == "slow")  _slowSheetActive  = true;
        _lastSheet = sheetName; // บันทึกไอเท็มล่าสุดที่เก็บได้
        ApplySheet();
    }

    /// <summary>เรียกจาก PowerUp.OnDeactivate — ปิด sheet ของไอเท็มนั้น และ fallback</summary>
    public void ClearSheet(string sheetName)
    {
        if (sheetName == "speed") _speedSheetActive = false;
        if (sheetName == "slow")  _slowSheetActive  = false;

        // fallback: ใช้ sheet ของ effect ที่ยังเหลืออยู่ ถ้าไม่มีแล้วก็ normal
        if (_slowSheetActive && sheetName == "speed")
            _lastSheet = "slow";
        else if (_speedSheetActive && sheetName == "slow")
            _lastSheet = "speed";
        else if (!_speedSheetActive && !_slowSheetActive)
            _lastSheet = "normal";

        ApplySheet();
    }

    private void ApplySheet()
    {
        string textureName = _lastSheet switch
        {
            "speed" => "Player/SpeedBoostPlayer-SpriteSheet",
            "slow"  => "Player/SlowDownPlayer-SpriteSheet",
            _       => "Player/Player-SpriteSheet",
        };

        string playingNow = _animator.CurrentAnimationName;
        RegisterAnimations(textureName);
        if (playingNow != null) _animator.Play(playingNow);
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

        // หยุดทุกอย่างเมื่อเกมจบ (goal reached)
        if (WorldTime.IsFrozen) return;

        // Goal animation — เล่นครบ 3 รอบแล้วส่งสัญญาณให้ GoalFlag ขึ้น overlay
        if (State == PlayerState.GoalReached)
        {
            SyncAnimation(dt);
            if (_animator.CurrentLoopCount >= GoalAnimLoopsRequired)
                IsGoalAnimationComplete = true;
            return;
        }

        // Phase 7 — Respawn timer (ทำงานแม้ตาย)
        if (State == PlayerState.Dead)
        {
            SyncAnimation(dt);
            _respawnTimer -= dt;
            if (_respawnTimer <= 0f) Respawn();
            return;
        }

        // Phase 7 — Screen boundary
        if (Position.Y > _fallDeathY) { Die(); return; } // ตกหล่น → ตาย

        // ชนขอบซ้าย/ขวา → หยุดแค่นั้น ไม่ตาย
        if (Position.X < _screenLeft)
        {
            Position  = new Vector2(_screenLeft, Position.Y);
            VelocityX = 0f;
        }
        else if (Position.X > _screenRight)
        {
            Position  = new Vector2(_screenRight, Position.Y);
            VelocityX = 0f;
        }

        // Coyote time — reset เมื่ออยู่บนพื้น, นับถอยหลังเมื่ออยู่ในอากาศ
        if (IsGrounded) _coyoteTimer = CoyoteTime;
        else if (_coyoteTimer > 0f) _coyoteTimer -= dt;

        // Wall jump cooldown
        if (_wallJumpCooldownTimer > 0f) _wallJumpCooldownTimer -= dt;
        if (_ledgeReleaseCooldown > 0f)  _ledgeReleaseCooldown  -= dt;
        if (_wallClingLockTimer    > 0f) _wallClingLockTimer    -= dt;
        if (_jumpBufferTimer       > 0f) _jumpBufferTimer       -= dt;

        // ── Left-click ขณะ Hooked → เริ่มดึงตัวเองไปตามเชือก ──────────────────
        if (Pickaxe.IsHooked && InputManager.Instance.IsMouseButtonPressed(0))
        {
            Pickaxe.SuppressCharge = true;
            Pickaxe.StartLaunch();
            ChangeState(PlayerState.Launching);
        }

        // ── Launching state: ดึงตัวเองตามเชือก — ข้าม input ปกติทั้งหมด ──────
        if (State == PlayerState.Launching)
        {
            HandleRopePull();
            Pickaxe.HandleInput(dt);   // ให้ pickaxe อัปเดต waypoints
            // ไม่ ApplyGravity — velocity ควบคุมโดย HandleRopePull
            MoveAndCollide(dt);
            CheckLedge();

            // ชนวัตถุขณะ launch
            if (IsTouchingWallLeft || IsTouchingWallRight || IsGrounded || IsTouchingCeiling)
            {
                _ropeHitWall = IsTouchingWallLeft || IsTouchingWallRight;
                VelocityY = IsTouchingCeiling ? 0f : JumpForce;  // ชนเพดาน → ร่วงลงอิสระ ไม่ดีดกลับ
                Pickaxe.Recall();
                ChangeState(PlayerState.Falling);
            }

            // ไม่ ApplyConstraint — เราเป็นคนดึงเอง
            CheckHazardCollision();
            CheckCheckpoint();
            CheckpointManager.Instance.UpdateSection(Position.X, Position.Y);
            SyncAnimation(dt);
            Rotation = FacingDirection == -1
                ? QuaternionUtils.Euler(0, 180, 0)
                : Vector3.Zero;
            UpdateActiveEffects(dt);
            return;
        }

        // Phase 3 — Input (ต้องก่อน physics เพื่อให้ intent ถูก)
        HandleSlide(dt);
        HandleCrouch();
        HandleMove();
        HandleSprint();
        if (Pickaxe.IsHooked) HandleRopeLaunch();
        else                  _isRopeDashing = false;
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

        // Swing X boost — เพิ่ม tangential force แนวนอนตาม sin(θ) ของมุมเชือก
        if (Pickaxe.IsHooked && !IsGrounded)
        {
            Vector2 anchor   = Pickaxe.LaunchTarget;
            Vector2 toPlayer = Position - anchor;
            float   ropeLen  = toPlayer.Length();
            if (ropeLen > 0.001f)
            {
                float sinTheta = toPlayer.X / ropeLen;   // sin ของมุมจากแนวดิ่ง
                VelocityX += SwingXBoost * sinTheta * dt;
            }
        }

        // Auto transitions
        if (!IsGrounded && State == PlayerState.Jumping && VelocityY > 0f)
            ChangeState(PlayerState.Falling);
        if (IsGrounded)
            HasUsedDoubleJump = false;

        // Phase 7 — Hazard collision (static traps จาก Member 3/4)
        CheckHazardCollision();

        // Phase 8 — Checkpoint detection
        CheckCheckpoint();
        CheckpointManager.Instance.UpdateSection(Position.X, Position.Y);
        // Sync animation to state
        SyncAnimation(dt);

        // Flip sprite ตาม FacingDirection ผ่าน Y rotation
        Rotation = FacingDirection == -1
            ? QuaternionUtils.Euler(0, 180, 0)
            : Vector3.Zero;

        UpdateActiveEffects(dt);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Animation Sync
    // ══════════════════════════════════════════════════════════════════════════

    private const float JumpStartDuration     = 0.083f; // ความยาว 1 frame
    private const float WallJumpStartDuration = 0.083f * 3f;
    private const float SlideStartDuration    = 0.083f * 4f;
    private const float SlideEndAnimDuration  = 0.083f * 4f; // row 12 reversed

    // ขยับ sprite ขึ้น (ไม่กระทบ collider) เพื่อชดเชย sprite art ที่วาดต่ำกว่า frame กลาง
    private const float DeadGoalSpriteOffsetY = 0f; // ← ปรับตรงนี้ถ้าต้องการขึ้น/ลงมากกว่านี้

    private void SyncAnimation(float dt)
    {
        // ── countdown timers ──────────────────────────────────────────────────
        if (_jumpAnimTimer     > 0f) _jumpAnimTimer     -= dt;
        if (_slideAnimTimer    > 0f) _slideAnimTimer    -= dt;
        if (_slideEndAnimTimer > 0f) _slideEndAnimTimer -= dt;

        // ชดเชย center shift ของ collider ตอน crouch:
        // SetCrouchHeight เลื่อน Position.Y ลง (PlayerHeight - _currentHeight)/2 px
        // → ขยับ sprite กลับขึ้นเท่าเดิมเพื่อไม่ให้ sprite จมพื้น
        float crouchCompensation = (_currentHeight - PlayerHeight) / 2f; // 0=ยืน, -8=ย่อ

        bool isDeadGoal  = State == PlayerState.Dead || State == PlayerState.GoalReached;
        _spriteRenderer.DrawOffset = isDeadGoal ? new Vector2(0f, DeadGoalSpriteOffsetY)
                                   : new Vector2(0f, crouchCompensation);

        switch (State)
        {
            // ── Idle: Standing ก่อน พอ 3 วิ เล่น Breathe ────────────────────
            case PlayerState.Idle:
                _idleTimer += dt;
                if (_idleTimer >= IdleBreathDelay)
                    _animator.Play("breathe");
                else
                    _animator.Play("standing");
                break;

            // ── Walk / Crouch Walk ────────────────────────────────────────────
            case PlayerState.Running:
                _idleTimer = 0f;
                _animator.Play("walk");
                _footstepTimer -= dt;
                if (_footstepTimer <= 0f)
                {
                    AudioManager.Instance.PlaySound("SFX/Walk");
                    _footstepTimer = FootstepInterval;
                }
                break;

            case PlayerState.Sprinting:
                _idleTimer = 0f;
                _animator.Play("run");
                _footstepTimer -= dt;
                if (_footstepTimer <= 0f)
                {
                    AudioManager.Instance.PlaySound("SFX/Walk");
                    _footstepTimer = FootstepInterval / SprintMultiplier;
                }
                break;

            // ── Jump: jumpstart 1 frame → jump ────────────────────────────────
            case PlayerState.Jumping:
                _idleTimer = 0f;
                if (_jumpAnimTimer > 0f)
                    _animator.Play(_jumpAnimTimer > WallJumpStartDuration - JumpStartDuration
                        ? "walljumpstart" : "jumpstart");
                else
                    _animator.Play("jump");
                break;

            case PlayerState.Falling:
                _idleTimer = 0f;
                _animator.Play("freefall");
                break;

            case PlayerState.WallClinging:
                _idleTimer = 0f;
                _animator.Play("wallslide");
                break;

            case PlayerState.LedgeGrabbing:
                _idleTimer = 0f;
                _animator.Play("ledgegrab");
                break;

            case PlayerState.Crouching:
                _idleTimer = 0f;
                if (VelocityX != 0f)
                    _animator.Play("crouchwalk");
                else if (_animator.CurrentAnimationName == "crouchwalk")
                    _animator.PlayAtEnd("crouch"); // หยุดเดิน → ข้ามไป pose นั่งยองเลย
                else
                    _animator.Play("crouch");
                break;

            // ── Slide: slidestart → slide → slideend ──────────────────────────
            case PlayerState.Sliding:
                _idleTimer = 0f;
                if (_slideAnimTimer > 0f)
                    _animator.Play("slidestart");
                else if (_slideEndAnimTimer > 0f)
                    _animator.Play("slideend");
                else
                    _animator.Play("slide");
                break;

            case PlayerState.Dead:
                _idleTimer = 0f;
                _animator.Play("dead");
                break;

            case PlayerState.GoalReached:
                _idleTimer = 0f;
                _animator.Play("goal");
                break;

            case PlayerState.Launching:
                _idleTimer = 0f;
                _animator.Play("jump");
                break;

            default:
                _idleTimer = 0f;
                _animator.Play("standing");
                break;
        }
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

        bool left  = InputManager.Instance.IsKeyDown(Keys.A);
        bool right = InputManager.Instance.IsKeyDown(Keys.D);

        // ระหว่าง wall jump cooldown — บล็อกการเคลื่อนที่เข้าหากำแพงที่เพิ่ง jump
        if (_wallJumpCooldownTimer > 0f)
        {
            if (_wallJumpedFromSide ==  1) right = false;
            if (_wallJumpedFromSide == -1) left  = false;
        }

        if (left && !right)
        {
            VelocityX       = -MoveSpeed * SpeedScale;
            FacingDirection = -1;
        }
        else if (right && !left)
        {
            VelocityX       = MoveSpeed * SpeedScale;
            FacingDirection = 1;
        }
        else
        {
            // ระหว่าง cooldown หลัง wall jump — รักษา kick velocity แทนที่จะ reset เป็น 0
            if (_wallJumpCooldownTimer > 0f)
                VelocityX = -_wallJumpedFromSide * MoveSpeed * WallJumpXMultiplier * SpeedScale;
            else
                VelocityX = 0f;
        }

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
    /// Double-tap A/D: sprint ไปทิศที่ double-tap
    /// sprint ต่อเนื่องตราบที่ยัง hold ปุ่มนั้นอยู่, หยุดเมื่อปล่อย
    /// </summary>
    private void HandleSprint()
    {
        if (State == PlayerState.Sliding
         || State == PlayerState.LedgeGrabbing
         || State == PlayerState.Crouching)
        {
            _sprintDir = 0;
            return;
        }

        bool shiftHeld = InputManager.Instance.IsKeyDown(Keys.LeftShift)
                      || InputManager.Instance.IsKeyDown(Keys.RightShift);
        bool dHeld = InputManager.Instance.IsKeyDown(Keys.D);
        bool aHeld = InputManager.Instance.IsKeyDown(Keys.A);

        // Shift+D = sprint ขวา, Shift+A = sprint ซ้าย
        if      (shiftHeld && dHeld)  _sprintDir = 1;
        else if (shiftHeld && aHeld)  _sprintDir = -1;
        else                          _sprintDir = 0;

        // apply sprint
        if (_sprintDir != 0 && VelocityX != 0f && IsGrounded)
        {
            VelocityX *= SprintMultiplier;
            ChangeState(PlayerState.Sprinting);
        }
        else if (State == PlayerState.Sprinting)
        {
            ChangeState(VelocityX == 0f ? PlayerState.Idle : PlayerState.Running);
        }
    }

    /// <summary>
    /// ขณะ Launching: เคลื่อนตัวไปตาม waypoints ของเชือก (bend → hook)
    /// Left-click / E ยกเลิก, พอถึง hook → Jumping/Falling
    /// </summary>
    private void HandleRopePull()
    {
        // ยกเลิก
        if (InputManager.Instance.IsMouseButtonPressed(1))
        {
            Pickaxe.Recall();
            ChangeState(PlayerState.Falling);
            return;
        }

        // จบ launch แล้ว (ถึง hook)
        if (Pickaxe.IsLaunchComplete)
        {
            ChangeState(VelocityY <= 0f ? PlayerState.Jumping : PlayerState.Falling);
            return;
        }

        Vector2 target = Pickaxe.LaunchTarget;
        Vector2 dir    = target - Position;
        float   dist   = dir.Length();
        if (dist < 0.001f) return;

        dir.Normalize();
        VelocityX       = dir.X * RopeLaunchSpeed;
        VelocityY       = dir.Y * RopeLaunchSpeed;
        FacingDirection = dir.X >= 0f ? 1 : -1;
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

        // พุ่งไปตามเส้นเชือกเส้นแรก (player → bend แรก หรือ hook ถ้าไม่มี bend)
        Vector2 target = Pickaxe.LaunchTarget;
        var dir  = target - Position;
        float dist = dir.Length();

        if (dist < 20f)
        {
            // ถึง waypoint แรกแล้ว → recall แล้วพุ่งต่อด้วย momentum เดิม
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
        bool jumpPressed = InputManager.Instance.IsKeyPressed(Keys.Space);
        if (jumpPressed) _jumpBufferTimer = JumpBufferWindow;

        bool hasJumpIntent = jumpPressed || _jumpBufferTimer > 0f;
        if (!hasJumpIntent) return;

        // อยู่ในพื้นที่แคบ (ย่ออยู่ + มีเพดานบัง) → กระโดดไม่ได้
        if (_currentHeight != PlayerHeight && !CanStandUp()) return;

        if (IsGrounded || _coyoteTimer > 0f)
        {
            bool coyote = !IsGrounded && _coyoteTimer > 0f;
            Console.WriteLine($"[JUMP] GroundJump{(coyote ? "(coyote)" : "")}  pos={Position.X:F0},{Position.Y:F0}");
            if (State == PlayerState.Crouching) SetCrouchHeight(false);
            VelocityY         = JumpForce;
            _coyoteTimer      = 0f;
            _jumpBufferTimer  = 0f;
            AudioManager.Instance.PlaySound("SFX/Jump");
            ChangeState(PlayerState.Jumping);
        }
        else if (State == PlayerState.WallClinging)
        {
            int wallSide           = IsTouchingWallRight ? 1 : -1;
            _wallJumpedFromSide    = wallSide;
            _wallJumpCooldownTimer = WallJumpCooldown;
            VelocityY              = JumpForce * WallJumpYMultiplier;
            VelocityX              = -wallSide * MoveSpeed * WallJumpXMultiplier * SpeedScale;
            FacingDirection        = -wallSide;
            _jumpBufferTimer       = 0f;
            Console.WriteLine($"[JUMP] WallJump  side={wallSide}  kickVX={VelocityX:F0}  kickVY={VelocityY:F0}  (buffered={!jumpPressed})");
            AudioManager.Instance.PlaySound("SFX/Jump");
            ChangeState(PlayerState.Jumping);
        }
        else if (State == PlayerState.LedgeGrabbing)
        {
            Console.WriteLine($"[JUMP] LedgeJump  pos={Position.X:F0},{Position.Y:F0}");
            _ledgeReleaseCooldown = 0.25f;
            VelocityY        = JumpForce;
            _jumpBufferTimer = 0f;
            AudioManager.Instance.PlaySound("SFX/Jump");
            ChangeState(PlayerState.Jumping);
        }
        else if (Pickaxe.IsHooked)
        {
            Console.WriteLine($"[JUMP] RopeJump");
            Pickaxe.Recall();
            VelocityY        = JumpForce;
            _jumpBufferTimer = 0f;
            AudioManager.Instance.PlaySound("SFX/Jump");
            ChangeState(PlayerState.Jumping);
        }
        else if (HasDoubleJump && !HasUsedDoubleJump)
        {
            Console.WriteLine($"[JUMP] DoubleJump");
            VelocityY         = JumpForce;
            HasUsedDoubleJump = true;
            _jumpBufferTimer  = 0f;
            AudioManager.Instance.PlaySound("SFX/Jump");
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
        bool pressingLeft      = InputManager.Instance.IsKeyDown(Keys.A);
        bool pressingRight     = InputManager.Instance.IsKeyDown(Keys.D);
        bool pressingTowardWall  = (IsTouchingWallLeft  && pressingLeft)
                                || (IsTouchingWallRight && pressingRight);
        bool pressingAwayFromWall = (IsTouchingWallLeft  && pressingRight)
                                 || (IsTouchingWallRight && pressingLeft);

        // บล็อก re-cling กำแพงเดิมระหว่าง cooldown หลัง wall jump
        bool blockedByCooldown = _wallJumpCooldownTimer > 0f
            && ((_wallJumpedFromSide ==  1 && IsTouchingWallRight)
             || (_wallJumpedFromSide == -1 && IsTouchingWallLeft));

        // kick พุ่งไปชนกำแพงฝั่งตรงข้าม → cling ได้อัตโนมัติแม้ไม่กดปุ่ม
        // ใช้ _wallJumpedFromSide != 0 แทน cooldown เพราะ cooldown อาจหมดก่อนถึงกำแพง
        bool kickedIntoOppositeWall = _wallJumpedFromSide != 0
            && ((_wallJumpedFromSide ==  1 && IsTouchingWallLeft)
             || (_wallJumpedFromSide == -1 && IsTouchingWallRight));

        // reset _wallJumpedFromSide เมื่อลงพื้น (ป้องกัน stale auto-cling)
        if (IsGrounded) _wallJumpedFromSide = 0;

        // rope pull พุ่งชนกำแพงแล้วกระโดด → auto-cling เฉพาะตอนขาลง (ขึ้นไม่ถึงแพลตฟอร์ม)
        bool autoClingSuffix = _ropeHitWall && touchingWall && VelocityY >= 0f;
        if (IsGrounded || !touchingWall) _ropeHitWall = false;

        if (!IsGrounded && touchingWall && ((pressingTowardWall && VelocityY >= 0f) || kickedIntoOppositeWall || autoClingSuffix)
            && State != PlayerState.LedgeGrabbing
            && !blockedByCooldown)
        {
            if (kickedIntoOppositeWall)
            {
                _wallJumpedFromSide = 0;
                _wallClingLockTimer = 0.15f; // ล็อก 0.15s ไม่ให้ pressingAway exit ทันที
                // buffer ยังคงอยู่ → HandleJump จะ fire wall jump จากกำแพงฝั่งนี้ทันที
            }
            else
            {
                _jumpBufferTimer = 0f; // press/rope cling: ต้องกด Space ใหม่ ไม่ใช้ buffer เก่า
            }

            string reason = pressingTowardWall ? "press" : kickedIntoOppositeWall ? "kick" : "rope";
            if (State != PlayerState.WallClinging)
                Console.WriteLine($"[WALL] Cling  side={(IsTouchingWallRight?"R":"L")}  reason={reason}  VX={VelocityX:F0} VY={VelocityY:F0}");
            ChangeState(PlayerState.WallClinging);
        }
        else if (State == PlayerState.WallClinging)
        {
            // ถ้ากด Space อยู่ → ให้ HandleJump fire wall jump ก่อน อย่าเพิ่ง exit
            bool jumpHeld = InputManager.Instance.IsKeyDown(Keys.Space) || _jumpBufferTimer > 0f;
            bool canExitByPress = pressingAwayFromWall && _wallClingLockTimer <= 0f && !jumpHeld;
            Console.WriteLine($"[WALL_EXIT?] touch={touchingWall} away={pressingAwayFromWall} lock={_wallClingLockTimer:F3} canExit={canExitByPress}");
            if      (IsGrounded)                        ChangeState(PlayerState.Idle);
            else if (!touchingWall || canExitByPress)   ChangeState(PlayerState.Falling);
        }
    }

    /// <summary>
    /// S / ↓: นั่งยองขณะอยู่บนพื้น
    ///   เข้า Crouching → ลดความสูง collider ครึ่งหนึ่ง (ขอบล่างคงที่)
    ///   ออก Crouching  → คืน collider ปกติ
    /// </summary>
    private void HandleCrouch()
    {
        bool crouchHeld = InputManager.Instance.IsKeyDown(Keys.S);

        // ถ้าไม่กด S แต่ height ยังเป็น crouch → ลองลุก แต่ต้องเช็ค headroom ก่อน
        if (!crouchHeld && _currentHeight != PlayerHeight && State != PlayerState.Sliding)
        {
            if (CanStandUp())
            {
                SetCrouchHeight(false);
                if (State == PlayerState.Crouching)
                    ChangeState(VelocityX == 0f ? PlayerState.Idle : PlayerState.Running);
            }
            // else: มีเพดานอยู่บน → ค้าง Crouching อัตโนมัติโดยไม่ต้องกด S
            // พอเดินออกจากพื้นที่แคบ CanStandUp() จะ return true และลุกเองทันที
            return;
        }

        // ลงพื้นระหว่าง hold S และ collider ยัง crouch height → คืน state Crouching
        if (crouchHeld && IsGrounded && _currentHeight != PlayerHeight
            && State != PlayerState.Crouching && State != PlayerState.Sliding)
        {
            ChangeState(PlayerState.Crouching);
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
    /// Shift+A/D: สไลด์ไปซ้าย/ขวาด้วยความเร็วสูง
    ///   ระหว่าง Slide: VelocityX = SlideSpeed (ทิศตาม FacingDirection), นับ timer
    ///   หมดเวลา: กลับ Crouching (ถ้ายัง hold S) หรือ Idle
    ///   Interaction กับ TankElephant: stub — รอ Member 3
    /// </summary>
    private void HandleSlide(float dt)
    {
        // ── Trigger ──────────────────────────────────────────────────────────
        bool shiftHeld  = InputManager.Instance.IsKeyDown(Keys.LeftShift)
                       || InputManager.Instance.IsKeyDown(Keys.RightShift);
        bool sPressed   = InputManager.Instance.IsKeyPressed(Keys.S);
        bool canSlide   = IsGrounded && shiftHeld && sPressed
                       && (_currentHeight == PlayerHeight || CanStandUp()); // อยู่ใต้เพดาน → slide ไม่ได้

        if (canSlide && State != PlayerState.Sliding)
        {
            // หันหน้าไปทางไหนอยู่ก็ slide ไปทางนั้น (FacingDirection คงเดิม)
            SetCrouchHeight(true);
            _slideTimer = SlideDuration;
            ChangeState(PlayerState.Sliding);
        }

        if (State != PlayerState.Sliding) return;

        // ── During Slide ──────────────────────────────────────────────────────
        // ช่วง slideend animation: หยุดเคลื่อนที่ รอ animation จบก่อน transition
        if (_slideEndAnimTimer > 0f)
        {
            VelocityX = 0f;
            if (_slideEndAnimTimer > dt) return; // ยังไม่จบ

            // slideend จบแล้ว → transition ออกจาก Sliding
            bool stillCrouching = InputManager.Instance.IsKeyDown(Keys.S);
            if (stillCrouching || !CanStandUp())
            {
                ChangeState(PlayerState.Crouching);
            }
            else
            {
                SetCrouchHeight(false);
                ChangeState(PlayerState.Idle);
            }
            return;
        }

        VelocityX    = FacingDirection * SlideSpeed * SpeedScale;
        _slideTimer -= dt;

        // ตรวจชน enemy ขณะสไลด์ → enemy ตาย
        foreach (var enemy in _enemies)
            if (_collider.Bounds.Intersects(enemy.ColliderBounds))
                enemy.Die();

        // TODO (Phase 3 → Member 3): TankElephant interaction
        //   ตรวจ _collider.Bounds ชน TankElephant.Collider.Bounds
        //   ถ้า Player อยู่ต่ำกว่า (slide under the legs) → tankElephant.GetStunned()

        // ── Slide End ─────────────────────────────────────────────────────────
        if (_slideTimer > 0f && IsGrounded) return;

        // เริ่ม slideend animation (หยุดนิ่ง รอ reverse animation จบ)
        _slideEndAnimTimer = SlideEndAnimDuration;
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
                 ;
        if (drop)
        {
            _ledgeReleaseCooldown = 0.25f;
            ChangeState(PlayerState.Falling);
        }
    }

    // ── Helper: Crouch Height ─────────────────────────────────────────────────

    /// <summary>
    /// ตรวจว่าพื้นที่เหนือหัวว่างพอที่จะลุกขึ้นยืนได้หรือไม่
    /// เปรียบเทียบ solid rects กับ headroom rectangle ที่จะเพิ่มขึ้นเมื่อลุก
    /// </summary>
    private bool CanStandUp()
    {
        if (_currentHeight == PlayerHeight) return true; // ยืนอยู่แล้ว

        float bottomY    = Position.Y + _currentHeight / 2f;  // ขอบล่าง (เท้า) — คงที่
        float standTopY  = bottomY - PlayerHeight;             // ขอบบนขณะยืน
        float crouchTopY = Position.Y - _currentHeight / 2f;  // ขอบบนขณะย่อ

        // พื้นที่เพิ่มเติมที่ต้องการ = ระหว่าง standTopY กับ crouchTopY
        var headroom = new Rectangle(
            (int)(Position.X - PlayerWidth / 2f),
            (int)standTopY,
            PlayerWidth,
            (int)(crouchTopY - standTopY));

        foreach (var solid in _solidRects)
            if (headroom.Intersects(solid)) return false;

        return true;
    }

    /// <summary>
    /// เปลี่ยนความสูง collider ระหว่างยืน ↔ นั่งยอง
    /// ขอบล่างของ collider ไม่เปลี่ยน (Player ไม่ "ลอย" ขึ้นตอนนั่ง)
    /// </summary>
    private void SetCrouchHeight(bool crouching)
    {
        float bottomY  = Position.Y + _currentHeight / 2f; // คงขอบล่างไว้ → IsGrounded ไม่เปลี่ยน
        _currentHeight = crouching ? PlayerHeight / 2 : PlayerHeight;
        Position       = new Vector2(Position.X, bottomY - _currentHeight / 2f);
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
                VelocityX = 0f;
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
        _wasGroundedPrev    = IsGrounded;
        IsGrounded          = false;
        IsTouchingWallLeft  = false;
        IsTouchingWallRight = false;
        IsTouchingCeiling   = false;

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
            // ใช้ >= สำหรับขอบล่าง เพื่อจับกรณี collider.Bottom == solid.Top พอดี
            bool hit = _collider.Bounds.Left  < solid.Right
                    && _collider.Bounds.Right  > solid.Left
                    && _collider.Bounds.Top    < solid.Bottom
                    && _collider.Bounds.Bottom >= solid.Top;
            if (!hit) continue;

            if (VelocityY > 0f)      // ตกลง → ลงจอดบน solid
            {
                if (!_wasGroundedPrev)
                {
                    Console.WriteLine($"[LAND]  pos={Position.X:F0},{Position.Y:F0}  impactVY={VelocityY:F0}  state={State}");
                    AudioManager.Instance.PlaySound("SFX/Landing");
                }
                Position   = new Vector2(Position.X, solid.Top - _currentHeight / 2f);
                IsGrounded = true;
            }
            else if (VelocityY < 0f) // กระโดดขึ้น → หัวชนใต้ solid → ดีดกลับลง
            {
                Position          = new Vector2(Position.X, solid.Bottom + _currentHeight / 2f);
                IsTouchingCeiling = true;
            }
            VelocityY = 0f;
            UpdateColliderBounds();
        }

        // ── Static wall adjacency probe ───────────────────────────────────────
        // ตรวจกำแพงด้วย bounding box ขยาย 2px ซ้าย/ขวา โดย inset Y เพื่อไม่ชน floor/ceiling
        {
            int insetY   = 4;
            var probeR   = new Rectangle(_collider.Bounds.Right,      _collider.Bounds.Top + insetY, 2, _collider.Bounds.Height - insetY * 2);
            var probeL   = new Rectangle(_collider.Bounds.Left - 2,   _collider.Bounds.Top + insetY, 2, _collider.Bounds.Height - insetY * 2);
            foreach (var solid in _solidRects)
            {
                if (probeR.Intersects(solid)) IsTouchingWallRight = true;
                if (probeL.Intersects(solid)) IsTouchingWallLeft  = true;
            }
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
        if (IsGrounded) return;
        if (_ledgeReleaseCooldown > 0f) return;
        if (State == PlayerState.LedgeGrabbing
         || State == PlayerState.WallClinging
         || State == PlayerState.Sliding
         || State == PlayerState.Launching) return;
        if (!IsTouchingWallLeft && !IsTouchingWallRight) return;
        if (Pickaxe.IsHooked) return;
        if (VelocityY < -150f) return;

        // ── Probe-based ledge detection ───────────────────────────────────────
        // "ขอบ" คือ: มีกำแพงระดับมือ (playerTop) + ไม่มีกำแพงเหนือหัว
        // → player อยู่ที่ขอบบนสุดของกำแพงจริง ไม่ใช่กลางกำแพง

        const int ProbeW     = 3;   // ความกว้าง probe แนวนอน (px)
        const int HandH      = 6;   // ความสูง probe ระดับมือ (px)
        const int AboveH     = 12;  // ความสูง probe เหนือหัว (px)

        int top   = _collider.Bounds.Top;
        int right = _collider.Bounds.Right;
        int left  = _collider.Bounds.Left;

        bool checkRight = IsTouchingWallRight;
        bool checkLeft  = IsTouchingWallLeft;

        int probeX      = checkRight ? right : left - ProbeW;
        int facingDir   = checkRight ? 1 : -1;

        // probe ระดับมือ — ต้องมีกำแพง
        var handProbe  = new Rectangle(probeX, top,          ProbeW, HandH);
        // probe เหนือหัว — ต้องไม่มีกำแพง (ถ้ามี = อยู่กลางกำแพง ไม่ใช่ขอบ)
        var aboveProbe = new Rectangle(probeX, top - AboveH, ProbeW, AboveH);

        bool wallAtHands   = false;
        bool wallAboveHead = false;

        foreach (var solid in _solidRects)
        {
            if (handProbe.Intersects(solid))  wallAtHands   = true;
            if (aboveProbe.Intersects(solid)) wallAboveHead = true;
        }

        if (!wallAtHands || wallAboveHead) return;

        // หา solid ที่มือแตะ เพื่อ snap ตำแหน่ง
        foreach (var solid in _solidRects)
        {
            if (!handProbe.Intersects(solid)) continue;

            Position        = new Vector2(Position.X, solid.Top + _currentHeight / 2f);
            VelocityX       = 0f;
            VelocityY       = 0f;
            FacingDirection = facingDir;
            UpdateColliderBounds();
            Console.WriteLine($"[LEDGE] Grab  side={(facingDir>0?"R":"L")}  snapY={solid.Top}  pos={Position.X:F0},{Position.Y:F0}");
            ChangeState(PlayerState.LedgeGrabbing);
            return;
        }
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
    public IReadOnlyList<Rectangle> Solids => _solidRects;
    public void SetWorldBounds(float left, float right, float fallDeathY)
    {
        _screenLeft = left;
        _screenRight = right;
        _fallDeathY = fallDeathY;
    }

    private List<Enemy> _enemies = [];
    public void SetEnemies(List<Enemy> enemies)
    {
        _enemies = enemies;
        Pickaxe?.SetEnemies(enemies); // Pickaxe อาจยังเป็น null ถ้าเรียกก่อน Initialize
    }

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

    public void AddFish(int value) => FishCount += value;

    /// <summary>เรียกจาก GoalFlag เมื่อ player แตะธง — เล่น animation 3 รอบก่อนขึ้น overlay</summary>
    public void TriggerGoalReached()
    {
        if (State == PlayerState.GoalReached) return;
        VelocityX = 0f;
        VelocityY = 0f;
        ChangeState(PlayerState.GoalReached);
    }

    public void Die()
    {
        if (State == PlayerState.Dead) return;
        VelocityX     = 0f;
        VelocityY     = 0f;
        _respawnTimer = RespawnDelaySec;
        Pickaxe.Recall();
        AudioManager.Instance.PlaySound("SFX/PlayerDead", 0.5f);
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

        Console.WriteLine($"[STATE] {State} → {newState}  |  VX={VelocityX:F0} VY={VelocityY:F0}  grounded={IsGrounded}  L={IsTouchingWallLeft} R={IsTouchingWallRight}");

        // Set animation startup timers เมื่อเข้า state ใหม่
        switch (newState)
        {
            case PlayerState.Jumping when State == PlayerState.WallClinging:
                _jumpAnimTimer = WallJumpStartDuration; // walljumpstart 3 frames
                break;
            case PlayerState.Jumping:
                _jumpAnimTimer = JumpStartDuration;     // jumpstart 1 frame
                break;
            case PlayerState.Sliding:
                _slideAnimTimer    = SlideStartDuration;   // slidestart 4 frames
                _slideEndAnimTimer = 0f;                   // reset slideend
                break;
            case PlayerState.Idle:
                _idleTimer     = 0f;  // reset breathe timer
                _footstepTimer = 0f;  // next walk starts a step immediately
                break;
        }

        State = newState;
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
