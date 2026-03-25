using ComputerGameFinal.Game.Example;

namespace ComputerGameFinal.Game;

/// <summary>
/// ชะลอ world objects เป็นเวลา 8 วินาที (timer เดินปกติ)
/// การ implement จริงรอ TimerManager จาก Member 5
/// </summary>
public class SlowTimePowerUp : PowerUp
{
    public SlowTimePowerUp()
    {
        Duration = 8f;
    }

    protected override void OnActivate(Player player)
    {
        // TODO (Phase 6 + Member 5): TimerManager.Instance.ApplySlowTime(Duration)
    }

    protected override void OnDeactivate(Player player)
    {
        // TODO (Phase 6 + Member 5): TimerManager.Instance.ResetTime()
    }
}
