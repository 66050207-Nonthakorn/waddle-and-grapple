using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Game;

// ── Data for one parallax layer ───────────────────────────────────────────────
public struct ParallaxLayer
{
    public Texture2D Texture;
    public float     ScrollFactor; // 0 = ติดจอ, 0.5 = เลื่อนครึ่งความเร็ว, 1 = เคลื่อนตาม world
    public float     LayerDepth;   // 0.0 = หลังสุด, ควรน้อยกว่า SpriteRenderer (0.5)
    public Color     Tint;

    public ParallaxLayer(Texture2D texture, float scrollFactor, float layerDepth = 0.05f, Color? tint = null)
    {
        Texture      = texture;
        ScrollFactor = scrollFactor;
        LayerDepth   = layerDepth;
        Tint         = tint ?? Color.White;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ParallaxBackground — Component ที่ attach กับ GameObject ใดก็ได้
//
// ใช้งาน:
//   var bg = bgObj.AddComponent<ParallaxBackground>();
//   bg.AddLayer("sky",       scrollFactor: 0.1f, layerDepth: 0.00f);
//   bg.AddLayer("mountains", scrollFactor: 0.3f, layerDepth: 0.01f);
//   bg.AddLayer("trees",     scrollFactor: 0.6f, layerDepth: 0.02f);
// ─────────────────────────────────────────────────────────────────────────────
public class ParallaxBackground : Component
{
    private readonly List<ParallaxLayer> _layers = new();

    // เพิ่ม layer โดยโหลด texture จากชื่อ asset (ResourceManager)
    public void AddLayer(string assetName, float scrollFactor, float layerDepth = 0.05f, Color? tint = null)
    {
        var tex = ResourceManager.Instance.GetTexture(assetName);
        _layers.Add(new ParallaxLayer(tex, scrollFactor, layerDepth, tint));
    }

    // เพิ่ม layer โดยส่ง texture โดยตรง
    public void AddLayer(Texture2D texture, float scrollFactor, float layerDepth = 0.05f, Color? tint = null)
    {
        _layers.Add(new ParallaxLayer(texture, scrollFactor, layerDepth, tint));
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        var camera = SceneManager.Instance.CurrentScene?.Camera;
        if (camera == null) return;

        float camX    = camera.Position.X;
        float screenW = ScreenManager.Instance.nativeWidth;
        float screenH = ScreenManager.Instance.nativeHeight;

        // Top-left ของ visible area ใน world space
        float visLeft = camX - screenW / 2f;
        float visTop  = camera.Position.Y - screenH / 2f;

        foreach (var layer in _layers)
        {
            if (layer.Texture == null) continue;
            DrawLayer(spriteBatch, layer, visLeft, visTop, screenW, screenH);
        }
    }

    private static void DrawLayer(
        SpriteBatch spriteBatch,
        ParallaxLayer layer,
        float visLeft, float visTop,
        float screenW, float screenH)
    {
        // สเกลให้ texture สูงเต็มจอ
        float scale     = screenH / layer.Texture.Height;
        float tileWorldW = layer.Texture.Width * scale;

        // "anchor" ของการ tile คือ camX * ScrollFactor ใน world space
        // anchor นี้เลื่อนช้ากว่า camera → เกิด parallax effect
        float anchorX = visLeft + screenW / 2f;   // camera center world X
        float bgOriginX = anchorX * layer.ScrollFactor;

        // หา tile แรกที่ cover visLeft
        float offset = (visLeft - bgOriginX) % tileWorldW;
        if (offset > 0f) offset -= tileWorldW;
        float startX = visLeft + offset;

        // วาด tile จนครอบ visible area
        float drawX = startX;
        while (drawX < visLeft + screenW + tileWorldW)
        {
            spriteBatch.Draw(
                layer.Texture,
                new Vector2(drawX, visTop),
                sourceRectangle: null,
                layer.Tint,
                rotation:    0f,
                origin:      Vector2.Zero,
                scale:       new Vector2(scale, scale),
                effects:     SpriteEffects.None,
                layerDepth:  layer.LayerDepth
            );
            drawX += tileWorldW;
        }
    }
}
