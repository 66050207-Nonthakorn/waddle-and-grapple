using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// เพิ่ม MoveSpeed ×1.5 เป็นเวลา 10 วินาที
/// OnDeactivate หาร MoveSpeed กลับด้วย multiplier เดิม
/// </summary>
public class SpeedBoostPowerUp : PowerUp
{
    public override Color ItemColor => new Color(255, 160, 0);
    protected override string SpriteName => "Collectibles/SpeedBoost";

    private const float Multiplier = 1.5f;

    public SpeedBoostPowerUp()
    {
        Duration = 10f;
    }

    protected override void OnActivate(Player player)
    {
        player.MoveSpeed *= Multiplier;
        player.SetActiveSheet("speed");
    }

    protected override void OnDeactivate(Player player)
    {
        player.MoveSpeed /= Multiplier;
        player.ClearSheet("speed");
    }
}
