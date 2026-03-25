using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components.Physics;
using ComputerGameFinal.Game.Example;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

/// <summary>
/// Base class สำหรับของที่เก็บได้ทุกชนิด (Coin, PowerUp item ฯลฯ)
///
/// วิธีใช้:
///   1. เพิ่มเข้า Scene ด้วย AddGameObject
///   2. เรียก collectible.SetPlayer(player) เพื่อให้รู้จัก Player
///   3. Override OnCollect(Player) เพื่อกำหนด effect
/// </summary>
public abstract class Collectible : GameObject
{
    // ขนาด collider (subclass ปรับได้ก่อน Initialize)
    protected int ColliderWidth  = 32;
    protected int ColliderHeight = 32;

    private CollectibleBoxCollider _collider;
    private Player _player;

    public bool IsCollected { get; private set; }

    /// <summary>เรียกจาก Level หลัง AddGameObject เพื่อผูก Player</summary>
    public void SetPlayer(Player player) => _player = player;

    public override void Initialize()
    {
        _collider = AddComponent<CollectibleBoxCollider>();
        SyncColliderBounds();
    }

    /// <summary>
    /// ตรวจ overlap กับ Player ทุก frame
    /// Subclass ที่ override Update ต้องเรียก base.Update(gameTime)
    /// </summary>
    public override void Update(GameTime gameTime)
    {
        if (IsCollected || !Active || _player == null) return;

        SyncColliderBounds();

        if (_collider.Bounds.Intersects(_player.ColliderBounds))
            DoCollect();
    }

    /// <summary>Effect เมื่อเก็บ — implement ใน subclass</summary>
    public abstract void OnCollect(Player player);

    // ── Private ───────────────────────────────────────────────────────────────

    private void DoCollect()
    {
        IsCollected = true;
        Active      = false; // หยุด draw + stop future collision checks
        OnCollect(_player);
    }

    private void SyncColliderBounds()
    {
        _collider.Bounds = new Rectangle(
            (int)(Position.X - ColliderWidth  / 2f),
            (int)(Position.Y - ColliderHeight / 2f),
            ColliderWidth,
            ColliderHeight);
    }
}

// concrete subclass ที่สร้าง instance ได้ (BoxCollider เป็น abstract)
internal sealed class CollectibleBoxCollider : BoxCollider { }
