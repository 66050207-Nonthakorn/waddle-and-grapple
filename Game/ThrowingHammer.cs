using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

public class ThrowingHammer : GameObject
{
    // ── Collider Size ─────────────────────────────────────────────────────────
    private const int HammerWidth  = 20;
    private const int HammerHeight = 20;

    // ── Physics ───────────────────────────────────────────────────────────────
    private const float HammerSpeed   = 320f;  // px/s
    private const float HammerGravity = 0.1f;  // gravity scale — slight arc
    private const float Lifetime      = 6f;    // วินาทีก่อน deactivate อัตโนมัติ

    // ── Sprite ────────────────────────────────────────────────────────────────
    public const float DisplayScale = 2f;

    // ── Runtime State ─────────────────────────────────────────────────────────
    private Vector2        _initialVelocity;
    private Player         _player;
    private List<Rectangle> _solidRects = [];
    private float          _lifetime;

    // ── Components ────────────────────────────────────────────────────────────
    private Rigidbody2D        _rb;
    private HammerBoxCollider  _collider;

    // ═════════════════════════════════════════════════════════════════════════
    // Called by Enemy3 BEFORE Initialize to configure the projectile.
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// กำหนด direction (normalized), Player reference และ solid rectangles ก่อน Initialize
    /// </summary>
    public void Setup(Vector2 direction, Player player, List<Rectangle> solids)
    {
        _initialVelocity = direction * HammerSpeed;
        _player          = player;
        _solidRects      = solids;
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═════════════════════════════════════════════════════════════════════════

    public override void Initialize()
    {
        _lifetime = Lifetime;
        Scale     = new Vector2(DisplayScale, DisplayScale);

        // ── Sprite + Animation ────────────────────────────────────────────────
        var spriteRenderer = AddComponent<SpriteRenderer>();
        spriteRenderer.LayerDepth = 0.45f;

        var animator = AddComponent<Animator>();
        var f = new AnimationFactory(
            ResourceManager.Instance.GetTexture("ThrowProjectile-SpriteSheet"),
            rows: 1, columns: 5
        );
        animator.AddAnimation("spin9arm", f.CreateFromRow(row: 0, totalFrames: 5, frameDuration: 0.10f));
        animator.Play("spin9arm");

        // ── Physics ───────────────────────────────────────────────────────────
        _rb              = AddComponent<Rigidbody2D>();
        _rb.GravityScale = HammerGravity;
        _rb.Velocity     = _initialVelocity;

        // ── Collider (bounds sync'd manually in Update like all other objects) ─
        _collider = AddComponent<HammerBoxCollider>();
        _collider.IsTrigger = true;
        SyncColliderBounds();
    }

    public override void Update(GameTime gameTime)
    {
        if (!Active) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _lifetime -= dt;
        if (_lifetime <= 0f)
        {
            Active = false;
            return;
        }

        // Rigidbody2D already moved Position via UpdateComponents before Update().
        SyncColliderBounds();

        // ── Solid collision → destroy hammer ─────────────────────────────────
        foreach (var solid in _solidRects)
        {
            if (_collider.Bounds.Intersects(solid))
            {
                Active = false;
                return;
            }
        }

        // ── Player hit → kill player and destroy hammer ───────────────────────
        if (_player != null && _player.Active)
        {
            if (_collider.Bounds.Intersects(_player.ColliderBounds))
            {
                _player.Die();
                Active = false;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void SyncColliderBounds()
    {
        if (_collider == null) return;
        _collider.Bounds = new Rectangle(
            (int)(Position.X - HammerWidth  / 2f),
            (int)(Position.Y - HammerHeight / 2f),
            HammerWidth,
            HammerHeight
        );
    }
}

// ── Concrete BoxCollider สำหรับ ThrowingHammer ───────────────────────────────
internal sealed class HammerBoxCollider : BoxCollider { }
