using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;

namespace WaddleAndGrapple.Engine.Components.Tile;

/// <summary>
/// Loads Tiled map editor exports (JSON format).
/// Export from Tiled: File → Export As → JSON map files (*.json)
/// </summary>
public static class TiledMapLoader
{
    /// <summary>
    /// Result of loading a Tiled JSON map.
    /// </summary>
    public class TiledMap
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public List<TiledTileLayer> TileLayers { get; set; } = [];
        public List<TiledObjectLayer> ObjectLayers { get; set; } = [];
    }

    public class TiledTileLayer
    {
        public string Name { get; set; }
        public int[,] MapData { get; set; }
        public bool Visible { get; set; }
        public float Opacity { get; set; }
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
    }

    public class TiledObjectLayer
    {
        public string Name { get; set; }
        public List<TiledObject> Objects { get; set; } = [];
    }

    public class TiledObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public Dictionary<string, string> Properties { get; set; } = [];
    }

    /// <summary>
    /// Load a Tiled JSON map file and parse it into a TiledMap structure.
    /// </summary>
    /// <param name="jsonPath">Absolute or relative path to the .json map file.</param>
    public static TiledMap Load(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Tiled map file not found: {jsonPath}");

        string json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var map = new TiledMap
        {
            Width = root.GetProperty("width").GetInt32(),
            Height = root.GetProperty("height").GetInt32(),
            TileWidth = root.GetProperty("tilewidth").GetInt32(),
            TileHeight = root.GetProperty("tileheight").GetInt32(),
        };

        // Parse layers
        foreach (var layer in root.GetProperty("layers").EnumerateArray())
        {
            string layerType = layer.GetProperty("type").GetString();

            if (layerType == "tilelayer")
            {
                map.TileLayers.Add(ParseTileLayer(layer, map.Width, map.Height));
            }
            else if (layerType == "objectgroup")
            {
                map.ObjectLayers.Add(ParseObjectLayer(layer));
            }
            // group layers contain nested layers
            else if (layerType == "group")
            {
                ParseGroupLayer(layer, map);
            }
        }

        return map;
    }

    private static void ParseGroupLayer(JsonElement group, TiledMap map)
    {
        if (!group.TryGetProperty("layers", out var layers)) return;

        foreach (var layer in layers.EnumerateArray())
        {
            string layerType = layer.GetProperty("type").GetString();

            if (layerType == "tilelayer")
                map.TileLayers.Add(ParseTileLayer(layer, map.Width, map.Height));
            else if (layerType == "objectgroup")
                map.ObjectLayers.Add(ParseObjectLayer(layer));
            else if (layerType == "group")
                ParseGroupLayer(layer, map);
        }
    }

    private static TiledTileLayer ParseTileLayer(JsonElement layer, int mapWidth, int mapHeight)
    {
        int width = layer.TryGetProperty("width", out var w) ? w.GetInt32() : mapWidth;
        int height = layer.TryGetProperty("height", out var h) ? h.GetInt32() : mapHeight;

        var tileLayer = new TiledTileLayer
        {
            Name = layer.GetProperty("name").GetString(),
            Visible = !layer.TryGetProperty("visible", out var vis) || vis.GetBoolean(),
            Opacity = layer.TryGetProperty("opacity", out var op) ? (float)op.GetDouble() : 1f,
            OffsetX = layer.TryGetProperty("offsetx", out var ox) ? ox.GetInt32() : 0,
            OffsetY = layer.TryGetProperty("offsety", out var oy) ? oy.GetInt32() : 0,
            MapData = new int[height, width],
        };

        var data = layer.GetProperty("data");

        // Tiled uses 0 = empty, 1-based GIDs. We convert to 0-based with -1 = empty.
        int i = 0;
        foreach (var gid in data.EnumerateArray())
        {
            int rawGid = gid.GetInt32();

            // Clear flip flags (bits 29-31 are used for flipping in Tiled)
            int tileId = rawGid & 0x0FFFFFFF;

            int row = i / width;
            int col = i % width;
            tileLayer.MapData[row, col] = tileId == 0 ? -1 : tileId - 1;
            i++;
        }

        return tileLayer;
    }

    private static TiledObjectLayer ParseObjectLayer(JsonElement layer)
    {
        var objectLayer = new TiledObjectLayer
        {
            Name = layer.GetProperty("name").GetString(),
        };

        foreach (var obj in layer.GetProperty("objects").EnumerateArray())
        {
            var tiledObj = new TiledObject
            {
                Id = obj.GetProperty("id").GetInt32(),
                Name = obj.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Type = obj.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                X = (float)obj.GetProperty("x").GetDouble(),
                Y = (float)obj.GetProperty("y").GetDouble(),
                Width = obj.TryGetProperty("width", out var w) ? (float)w.GetDouble() : 0,
                Height = obj.TryGetProperty("height", out var h) ? (float)h.GetDouble() : 0,
            };

            // Parse custom properties
            if (obj.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateArray())
                {
                    string propName = prop.GetProperty("name").GetString();
                    string propValue = prop.GetProperty("value").ToString();
                    tiledObj.Properties[propName] = propValue;
                }
            }

            objectLayer.Objects.Add(tiledObj);
        }

        return objectLayer;
    }

    /// <summary>
    /// Create GameObjects with Tilemap components from a loaded TiledMap.
    /// One GameObject is created per tile layer.
    /// </summary>
    /// <param name="scene">The scene to add tilemaps to.</param>
    /// <param name="map">The parsed TiledMap.</param>
    /// <param name="tileset">The tileset texture to use.</param>
    /// <param name="baseLayer">Base draw layer depth (each subsequent layer adds 0.01).</param>
    /// <param name="solidTileIndices">Optional tile indices to mark as solid (collision). Applied to the first tile layer only if not null.</param>
    /// <returns>List of created tile layer GameObjects.</returns>
    public static List<GameObject> CreateTilemapObjects(
        Scene scene,
        TiledMap map,
        Texture2D tileset,
        float baseLayer = 0.5f,
        int[] solidTileIndices = null)
    {
        var objects = new List<GameObject>();
        float layer = baseLayer;

        for (int i = 0; i < map.TileLayers.Count; i++)
        {
            var tileLayer = map.TileLayers[i];

            string name = $"tilemap_{tileLayer.Name}";
            var go = scene.AddGameObject<GameObject>(name);

            if (tileLayer.OffsetX != 0 || tileLayer.OffsetY != 0)
                go.Position = new Vector2(tileLayer.OffsetX, tileLayer.OffsetY);

            var tilemap = go.AddComponent<Tilemap>();
            tilemap.Tileset = tileset;
            tilemap.SourceTileSize = map.TileWidth;
            tilemap.DestinationTileSize = map.TileWidth;
            tilemap.Layer = layer;
            tilemap.MapData = tileLayer.MapData;

            // Add collision to the first tile layer by default
            if (solidTileIndices != null && i == 0)
            {
                var collider = go.AddComponent<TileCollider>();
                collider.SetSolid(solidTileIndices);
            }

            objects.Add(go);
            layer -= 0.01f; // layers on top draw over lower layers
        }

        return objects;
    }
}
