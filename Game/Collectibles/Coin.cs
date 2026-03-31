using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Game.Example;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// เหรียญทอง — เก็บได้เพื่อเพิ่ม CoinCount ของ Player
///
/// วิธีใส่ใน Scene:
///   var coin = scene.AddGameObject&lt;Coin&gt;("coin_1");
///   coin.Position = new Vector2(300, 480);
///   coin.Value = 1;
///   coin.SetPlayer(player);
/// </summary>
public class Coin : Collectible
{
    public int Value { get; set; } = 1;

    public override void Initialize()
    {
        ColliderWidth  = 24;
        ColliderHeight = 24;

        base.Initialize(); // ตั้ง collider

        // pixel texture ขนาด 1×1 → ยืดด้วย Scale ให้เป็น 24×24 px
        // TODO (Phase 9): ใส่ sprite จริงจาก Member 5
        Scale      = new Vector2(24, 24);
        var sr     = AddComponent<SpriteRenderer>();
        sr.Texture    = ResourceManager.Instance.GetTexture("pixel");
        sr.Tint       = Color.Gold;
        sr.LayerDepth = 0.5f;
    }

    public override void OnCollect(Player player)
    {
        player.AddCoin(Value);
        // TODO (Phase 9): เล่น sound effect (AudioManager)
        // TODO (Phase 9): spawn particle
    }
}
