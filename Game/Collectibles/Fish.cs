using System.Collections.Generic;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// ปลา — เก็บได้เพื่อเพิ่ม FishCount ของ Player
///
/// วิธีใส่ใน Scene:
///   var fish = scene.AddGameObject&lt;Fish&gt;("fish_1");
///   fish.Position = new Vector2(300, 480);
///   fish.Value = 1;
///   fish.SetPlayer(player);
/// </summary>
public class Fish : Collectible
{
    public int Value { get; set; } = 1;

    public override void Initialize()
    {
        ColliderWidth  = 32;
        ColliderHeight = 32;

        base.Initialize(); // ตั้ง collider

        Scale = Vector2.One;

        // Fish.png: 256×64, 8 cols × 2 rows, frame = 32×32
        // row 0 = idle animation (8 frames)
        var sheet  = ResourceManager.Instance.GetTexture("Collectibles/Fish");
        const int FrameW = 32, FrameH = 32, FrameCount = 8;
        var frames = new List<Microsoft.Xna.Framework.Rectangle>();
        for (int i = 0; i < FrameCount; i++)
            frames.Add(new Microsoft.Xna.Framework.Rectangle(i * FrameW, 0, FrameW, FrameH));

        var anim     = new Animation(sheet, frames, frameDuration: 0.1f, isLooping: true);
        var animator = AddComponent<Animator>();
        animator.AddAnimation("idle", anim);
        animator.Play("idle");

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.LayerDepth = 0.5f;
    }

    public override void OnCollect(Player player)
    {
        player.AddFish(Value);
        // TODO (Phase 9): เล่น sound effect (AudioManager)
        // TODO (Phase 9): spawn particle
    }
}
