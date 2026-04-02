using Microsoft.Xna.Framework;
using WaddleAndGrapple.Game.Example;

namespace WaddleAndGrapple.Game;

/// <summary>
/// ให้ Player กระโดดได้ 2 ครั้ง (one-time use)
/// ใช้ Double Jump แล้ว → effect หมด (deactivate เมื่อ HasUsedDoubleJump == true)
/// Player.HandleJump() ตรวจ HasDoubleJump ก่อน jump ครั้งที่ 2
/// </summary>
public class DoubleJumpPowerUp : PowerUp
{
    public override Color ItemColor => new Color(0, 220, 255); // ฟ้าสว่าง

    public DoubleJumpPowerUp()
    {
        Duration = 15f; // มีเวลา 15 วินาที — ใช้ double jump ได้หลายครั้งภายในช่วงนี้
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
