using Microsoft.Xna.Framework;
using WaddleAndGrapple.Game.Example;

namespace WaddleAndGrapple.Game;

/// <summary>
/// เก็บ item → เกจเต็ม, กด double jump (ขณะลอยอยู่) → เกจ drain 0.4s → ability หาย
/// ไม่มี time limit — ถ้าไม่ได้ใช้ เกจอยู่เต็มตลอด
/// </summary>
public class DoubleJumpPowerUp : PowerUp
{
    public override Color ItemColor => new Color(0, 220, 255);
    protected override string SpriteName => "Collectibles/DoubleJump";

    private const float DrainDuration = 0.4f;

    private float _gaugeRatio = 1f;
    public override float GaugeRatio => _gaugeRatio;

    private bool  _draining   = false;
    private float _drainTimer = 0f;

    public DoubleJumpPowerUp()
    {
        Duration = 0f; // ไม่นับเวลา
    }

    protected override void OnActivate(Player player)
    {
        player.HasDoubleJump     = true;
        player.HasUsedDoubleJump = false;
        _gaugeRatio          = 1f;
        _draining   = false;
        _drainTimer = 0f;
    }

    protected override void OnDeactivate(Player player)
    {
        player.HasDoubleJump     = false;
        player.HasUsedDoubleJump = false;
        _gaugeRatio = 0f;
    }

    public override void UpdateEffect(Player player, float dt)
    {
        if (!IsActive) return;

        // ตรวจ: player.HasDoubleJump เพิ่งถูก set เป็น false (แสดงว่าใช้ double jump แล้ว)
        // HasDoubleJump จะยังคงเป็น true จนกว่า Player.HandleJump จะ set HasUsedDoubleJump=true
        // แต่เราตรวจ HasUsedDoubleJump ก็ได้ — Player reset มันเป็น false แค่ตอน IsGrounded
        // ซึ่งตอน double jump player ต้องอยู่ในอากาศ ดังนั้น HasUsedDoubleJump=true ค้างอยู่ได้
        if (!_draining && player.HasUsedDoubleJump && !player.IsGrounded)
        {
            _draining   = true;
            _drainTimer = 0f;
        }

        if (_draining)
        {
            _drainTimer += dt;
            _gaugeRatio  = System.Math.Max(0f, 1f - _drainTimer / DrainDuration);
            if (_drainTimer >= DrainDuration)
                Deactivate(player);
        }
    }
}
