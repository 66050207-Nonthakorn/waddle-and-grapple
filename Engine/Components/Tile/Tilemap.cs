using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Engine.Components.Tile;

public class Tilemap : Component
{
    public Texture2D Tileset { get; set; }
    public int SourceTileSize { get; set; }
    public int DestinationTileSize { get; set; }
    public float Layer { get; set; } = 0f;
    public int [,] MapData { get; set; }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (Tileset == null || MapData == null) return;

        int tilesetColumns = Tileset.Width / SourceTileSize;

        for (int y = 0; y < MapData.GetLength(0); y++)
        {
            for (int x = 0; x < MapData.GetLength(1); x++)
            {
                int tileIndex = MapData[y, x];
                if (tileIndex == -1) continue; // Skip empty tiles

                int tileX = tileIndex % tilesetColumns;
                int tileY = tileIndex / tilesetColumns; 

                Rectangle sourceRect = new Rectangle(tileX * SourceTileSize, tileY * SourceTileSize, SourceTileSize, SourceTileSize);
                Vector2 tileScale = new Vector2((float)DestinationTileSize / SourceTileSize) * GameObject.Scale;
                Vector2 position = new Vector2(x * DestinationTileSize, y * DestinationTileSize) * GameObject.Scale;

                spriteBatch.Draw(Tileset, position + GameObject.Position, sourceRect, Color.White,
                    GameObject.Rotation.Z, Vector2.Zero, tileScale, SpriteEffects.None, Layer);
            }
        }
    }
}