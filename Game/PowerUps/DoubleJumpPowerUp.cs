using WaddleAndGrapple.Game.Example;

namespace WaddleAndGrapple.Game;

/// <summary>
/// ให้ Player กระโดดได้ 2 ครั้ง (one-time use)
/// ใช้ Double Jump แล้ว → effect หมด (deactivate เมื่อ HasUsedDoubleJump == true)
/// Player.HandleJump() ตรวจ HasDoubleJump ก่อน jump ครั้งที่ 2
/// </summary>
public class DoubleJumpPowerUp : PowerUp
{
    public DoubleJumpPowerUp()
    {
        Duration = 0f; // one-time use ไม่มีเวลาหมด
    }

    protected override void OnActivate(Player player)
    {
        player.HasDoubleJump     = true;
        player.HasUsedDoubleJump = false;
    }

    protected override void OnDeactivate(Player player)
    {
        player.HasDoubleJump     = false;
        player.HasUsedDoubleJump = false;
    }
}
