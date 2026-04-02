using Microsoft.Xna.Framework;
using WaddleAndGrapple.Game.Example;

namespace WaddleAndGrapple.Game;

/// <summary>
/// ชะลอ world objects เป็นเวลา 8 วินาที (timer เดินปกติ)
/// การ implement จริงรอ TimerManager จาก Member 5
/// </summary>
public class SlowTimePowerUp : PowerUp
{
    public override Color ItemColor => new Color(180, 0, 255); // ม่วงสดใส

    public SlowTimePowerUp()
    {
        Duration = 8f;
    }

    protected override void OnActivate(Player player)   => WorldTime.SetSlow();
    protected override void OnDeactivate(Player player) => WorldTime.SetNormal();
}
