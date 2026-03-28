using System;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.Components.Tile;

public class Tilemap : Component
{
    public Texture2D Tileset { get; set; }
    public int SourceTileSize { get; set; }
    public int DestinationTileSize { get; set; }
    
    public float Layer { get; set; } = 0f;
    public int[,] MapData { get; set; }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Tileset == null || MapData == null) return;

        int tilesetColumns = Tileset.Width / SourceTileSize;
        int mapRows = MapData.GetLength(0);
        int mapCols = MapData.GetLength(1);

        // Compute the world-space visible area from the active camera
        var camera = SceneManager.Instance.CurrentScene?.Camera;
        int startX = 0, startY = 0, endX = mapCols - 1, endY = mapRows - 1;

        if (camera != null)
        {
            float scaledTile = DestinationTileSize * GameObject.Scale.X;
            Rectangle visible = camera.GetVisibleArea();

            // Convert world visible rect to tile indices (relative to this tilemap's position)
            startX = Math.Max(0, (int)Math.Floor((visible.Left   - GameObject.Position.X) / scaledTile));
            startY = Math.Max(0, (int)Math.Floor((visible.Top    - GameObject.Position.Y) / scaledTile));
            endX   = Math.Min(mapCols - 1, (int)Math.Ceiling((visible.Right  - GameObject.Position.X) / scaledTile));
            endY   = Math.Min(mapRows - 1, (int)Math.Ceiling((visible.Bottom - GameObject.Position.Y) / scaledTile));
        }

        Vector2 tileScale = new Vector2((float)DestinationTileSize / SourceTileSize) * GameObject.Scale;

        for (int y = startY; y <= endY; y++)
        {
            for (int x = startX; x <= endX; x++)
            {
                int tileIndex = MapData[y, x];
                if (tileIndex == -1) continue;

                int tileX = tileIndex % tilesetColumns;
                int tileY = tileIndex / tilesetColumns;

                Rectangle sourceRect = new Rectangle(tileX * SourceTileSize, tileY * SourceTileSize, SourceTileSize, SourceTileSize);
                Vector2 position = new Vector2(x * DestinationTileSize, y * DestinationTileSize) * GameObject.Scale;

                spriteBatch.Draw(Tileset, position + GameObject.Position, sourceRect, Color.White,
                    GameObject.Rotation.Z, Vector2.Zero, tileScale, SpriteEffects.None, Layer);
            }
        }
    }
}