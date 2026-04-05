using System;
using System.Collections.Generic;
using WaddleAndGrapple.Engine.Components.Physics;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Engine.Components.Tile;

public class TileCollider : Collider
{
    private readonly HashSet<int> _solidTiles = [];

    public void SetSolid(params int[] indices)
    {
        foreach (var i in indices) _solidTiles.Add(i);
    }

    public void AddSolid(int index) => _solidTiles.Add(index);
    public void RemoveSolid(int index) => _solidTiles.Remove(index);
    public bool IsSolid(int index) => _solidTiles.Contains(index);

    public List<Rectangle> GetSolidRects()
    {
        var (tilemap, scaledW, scaledH) = GetSetup();
        var result = new List<Rectangle>();
        if (tilemap == null) return result;
        int rows = tilemap.MapData.GetLength(0);
        int cols = tilemap.MapData.GetLength(1);
        for (int ty = 0; ty < rows; ty++)
            for (int tx = 0; tx < cols; tx++)
                if (_solidTiles.Contains(tilemap.MapData[ty, tx]))
                    result.Add(GetTileRect(tx, ty, scaledW, scaledH));
        return result;
    }

    // ── Collider interface ───────────────────────────────────────────────────

    public override bool IsIntersect(Collider other) => other switch
    {
        BoxCollider box       => IsIntersect(box),
        CircleCollider circle => IsIntersect(circle),
        _                     => false,
    };

    public override bool IsIntersect(BoxCollider other)
    {
        var (tilemap, scaledW, scaledH) = GetSetup();
        if (tilemap == null) return false;

        Rectangle worldBox = other.WorldBounds;
        int mapRows = tilemap.MapData.GetLength(0);
        int mapCols = tilemap.MapData.GetLength(1);
        GetTileRange(worldBox.Left, worldBox.Top, worldBox.Right, worldBox.Bottom,
            mapRows, mapCols, scaledW, scaledH, out int sx, out int sy, out int ex, out int ey);

        for (int ty = sy; ty <= ey; ty++)
            for (int tx = sx; tx <= ex; tx++)
                if (_solidTiles.Contains(tilemap.MapData[ty, tx]) &&
                    worldBox.Intersects(GetTileRect(tx, ty, scaledW, scaledH)))
                    return true;
        return false;
    }

    public override bool IsIntersect(CircleCollider other)
    {
        var (tilemap, scaledW, scaledH) = GetSetup();
        if (tilemap == null) return false;

        Vector2 center = other.GetCenter();
        float r = other.Radius;
        int mapRows = tilemap.MapData.GetLength(0);
        int mapCols = tilemap.MapData.GetLength(1);
        GetTileRange(center.X - r, center.Y - r, center.X + r, center.Y + r,
            mapRows, mapCols, scaledW, scaledH, out int sx, out int sy, out int ex, out int ey);

        for (int ty = sy; ty <= ey; ty++)
            for (int tx = sx; tx <= ex; tx++)
                if (_solidTiles.Contains(tilemap.MapData[ty, tx]) &&
                    GetMTVCircleBox(center, r, GetTileRect(tx, ty, scaledW, scaledH)) != Vector2.Zero)
                    return true;
        return false;
    }

    public override Vector2 GetMTV(Collider other) => other switch
    {
        BoxCollider box       => GetMTV(box),
        CircleCollider circle => GetMTV(circle),
        _                     => Vector2.Zero,
    };

    // MTV convention: a.GetMTV(b) pushes a out of b.
    // TileCollider=a (static), BoxCollider=b (dynamic).
    // ResolveOverlap pushes b by -GetMTV → player moves by accumulated. ✓
    public override Vector2 GetMTV(BoxCollider other)
    {
        var (tilemap, scaledW, scaledH) = GetSetup();
        if (tilemap == null) return Vector2.Zero;

        int mapRows = tilemap.MapData.GetLength(0);
        int mapCols = tilemap.MapData.GetLength(1);

        Vector2 accumulated = Vector2.Zero;
        Rectangle simBox = other.WorldBounds;

        GetTileRange(simBox.Left, simBox.Top, simBox.Right, simBox.Bottom,
            mapRows, mapCols, scaledW, scaledH, out int sx, out int sy, out int ex, out int ey);

        for (int ty = sy; ty <= ey; ty++)
        {
            for (int tx = sx; tx <= ex; tx++)
            {
                if (!_solidTiles.Contains(tilemap.MapData[ty, tx])) continue;

                Rectangle tileRect = GetTileRect(tx, ty, scaledW, scaledH);
                if (!simBox.Intersects(tileRect)) continue;

                // Pushes simBox (player) out of tileRect
                Vector2 correction = GetMTVBoxBox(simBox, tileRect);
                if (correction == Vector2.Zero) continue;

                accumulated += correction;
                simBox = new Rectangle(
                    (int)(simBox.X + correction.X),
                    (int)(simBox.Y + correction.Y),
                    simBox.Width, simBox.Height);
            }
        }

        return -accumulated;
    }

    public override Vector2 GetMTV(CircleCollider other)
    {
        var (tilemap, scaledW, scaledH) = GetSetup();
        if (tilemap == null) return Vector2.Zero;

        int mapRows = tilemap.MapData.GetLength(0);
        int mapCols = tilemap.MapData.GetLength(1);

        Vector2 accumulated = Vector2.Zero;
        Vector2 simCenter = other.GetCenter();
        float r = other.Radius;

        GetTileRange(simCenter.X - r, simCenter.Y - r, simCenter.X + r, simCenter.Y + r,
            mapRows, mapCols, scaledW, scaledH, out int sx, out int sy, out int ex, out int ey);

        for (int ty = sy; ty <= ey; ty++)
        {
            for (int tx = sx; tx <= ex; tx++)
            {
                if (!_solidTiles.Contains(tilemap.MapData[ty, tx])) continue;

                Vector2 correction = GetMTVCircleBox(simCenter, r, GetTileRect(tx, ty, scaledW, scaledH));
                if (correction == Vector2.Zero) continue;

                accumulated += correction;
                simCenter += correction;
            }
        }

        return -accumulated;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private (Tilemap tilemap, int scaledW, int scaledH) GetSetup()
    {
        var tilemap = GameObject.GetComponent<Tilemap>();
        if (tilemap?.MapData == null || tilemap.Tileset == null) return (null, 0, 0);
        int scaledW = (int)(tilemap.DestinationTileSize * GameObject.Scale.X);
        int scaledH = (int)(tilemap.DestinationTileSize * GameObject.Scale.Y);
        return (tilemap, scaledW, scaledH);
    }

    private Rectangle GetTileRect(int tx, int ty, int scaledW, int scaledH)
        => new Rectangle(
            (int)GameObject.Position.X + tx * scaledW,
            (int)GameObject.Position.Y + ty * scaledH,
            scaledW,
            scaledH);

    private void GetTileRange(float left, float top, float right, float bottom,
        int mapRows, int mapCols, int scaledW, int scaledH,
        out int startX, out int startY, out int endX, out int endY)
    {
        float ox = GameObject.Position.X;
        float oy = GameObject.Position.Y;

        startX = Math.Max(0,          (int)Math.Floor((left   - ox) / scaledW));
        startY = Math.Max(0,          (int)Math.Floor((top    - oy) / scaledH));
        endX   = Math.Min(mapCols - 1, (int)Math.Ceiling((right  - ox) / scaledW));
        endY   = Math.Min(mapRows - 1, (int)Math.Ceiling((bottom - oy) / scaledH));
    }

    // Box vs Box MTV — same logic as BoxCollider.GetMTV(BoxCollider)
    private static Vector2 GetMTVBoxBox(Rectangle a, Rectangle b)
    {
        float overlapX = Math.Min(a.Right, b.Right)  - Math.Max(a.Left, b.Left);
        float overlapY = Math.Min(a.Bottom, b.Bottom) - Math.Max(a.Top, b.Top);

        if (overlapX <= 0 || overlapY <= 0) return Vector2.Zero;

        Vector2 centerA = new Vector2(a.Center.X, a.Center.Y);
        Vector2 centerB = new Vector2(b.Center.X, b.Center.Y);

        if (overlapX <= overlapY)
        {
            float sign = centerA.X < centerB.X ? -1f : 1f;
            return new Vector2(sign * overlapX, 0f);
        }
        else
        {
            float sign = centerA.Y < centerB.Y ? -1f : 1f;
            return new Vector2(0f, sign * overlapY);
        }
    }

    // Circle vs Box MTV — same logic as CircleCollider.GetMTV(BoxCollider)
    private static Vector2 GetMTVCircleBox(Vector2 center, float radius, Rectangle box)
    {
        Vector2 boxCenter = new Vector2(box.Center.X, box.Center.Y);
        Vector2 boxHalf   = new Vector2(box.Width / 2f, box.Height / 2f);

        Vector2 diff    = center - boxCenter;
        Vector2 clamped = Vector2.Clamp(diff, -boxHalf, boxHalf);
        Vector2 closest = boxCenter + clamped;
        Vector2 toCircle = center - closest;

        float dist = toCircle.Length();
        if (dist <= 1e-6f)
        {
            // Center is inside the box — push out along shortest axis
            float left   = center.X - box.Left;
            float right  = box.Right - center.X;
            float top    = center.Y - box.Top;
            float bottom = box.Bottom - center.Y;

            float   min    = left;
            Vector2 normal = -Vector2.UnitX;

            if (right  < min) { min = right;  normal =  Vector2.UnitX;  }
            if (top    < min) { min = top;     normal = -Vector2.UnitY; }
            if (bottom < min) { min = bottom;  normal =  Vector2.UnitY; }

            return normal * (radius + min);
        }

        float overlap = radius - dist;
        if (overlap <= 0f) return Vector2.Zero;

        return (toCircle / dist) * overlap;
    }
}
