using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine;

namespace WaddleAndGrapple.Engine.Components.Tile;

/// <summary>
/// Loads Tiled map files (JSON/TMJ) and spawns registered game objects from object layers.
///
/// Usage:
///   var loader = new MapLoader(scene, "Content/maps/Level1.tmj");
///
///   loader.Register&lt;SawTrap&gt;("SawTrap", (trap, obj) =>
///   {
///       trap.MoveRange      = obj.FloatProp("moveRange", 150f);
///       trap.MoveHorizontal = obj.BoolProp("moveHorizontal", true);
///       trap.Player         = player;
///   });
///
///   var result = loader.Load(tileset, solidTileIndices: solidIds);
///   // result.GetSpawned&lt;SawTrap&gt;()  — enumerate all spawned SawTraps
/// </summary>
public class MapLoader(Scene scene, string mapPath)
{
    // ── Data types ────────────────────────────────────────────────────────────

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

    // ── Fields ────────────────────────────────────────────────────────────────

    private readonly Scene _scene = scene ?? throw new ArgumentNullException(nameof(scene));
    private readonly string _mapPath = mapPath ?? throw new ArgumentNullException(nameof(mapPath));
    private readonly Dictionary<string, Func<TiledObject, GameObject>> _registry
        = new(StringComparer.OrdinalIgnoreCase);

    // ── Registration ──────────────────────────────────────────────────────────

    /// <summary>
    /// Register a game object type by its Tiled Class name.
    /// The object is created, positioned at (X, Y) from the map, then
    /// <paramref name="configure"/> is called to apply custom properties.
    /// </summary>
    public MapLoader Register<T>(
        string typeName,
        Action<T, TiledObject> configure = null)
        where T : GameObject, new()
    {
        _registry[typeName] = obj =>
        {
            string goName = string.IsNullOrWhiteSpace(obj.Name)
                ? $"{typeName}_{obj.Id}"
                : obj.Name;

            var go = _scene.AddGameObject<T>(goName);
            go.Position = new Vector2(obj.X, obj.Y);
            configure?.Invoke(go, obj);
            return go;
        };
        return this;
    }

    /// <summary>Register a fully custom factory for a Tiled Class name.</summary>
    public MapLoader Register(string typeName, Func<TiledObject, GameObject> factory)
    {
        _registry[typeName] = factory ?? throw new ArgumentNullException(nameof(factory));
        return this;
    }

    // ── Loading ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Parse the map file, build tilemap GameObjects, and spawn all registered objects.
    /// </summary>
    /// <param name="tileset">Tileset texture used for tile layers.</param>
    /// <param name="baseLayer">Base draw depth for tile layers (default 0.5).</param>
    /// <param name="solidTileIndices">Tile indices treated as solid (first tile layer only).</param>
    public LoadResult Load(
        Texture2D tileset,
        float baseLayer = 0.5f,
        int[] solidTileIndices = null)
    {
        var map      = ParseMap(_mapPath);
        var tilemaps = BuildTilemapObjects(_scene, map, tileset, baseLayer, solidTileIndices);

        var spawned      = new List<GameObject>();
        var unregistered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var objectLayer in map.ObjectLayers)
        {
            foreach (var obj in objectLayer.Objects)
            {
                // Match on Type/Class first, fall back to object Name.
                string key = string.IsNullOrWhiteSpace(obj.Type) ? obj.Name : obj.Type;
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (_registry.TryGetValue(key, out var factory))
                {
                    var go = factory(obj);
                    if (go != null) spawned.Add(go);
                }
                else
                {
                    unregistered.Add(key);
                }
            }
        }

        return new LoadResult(tilemaps, spawned, map, unregistered);
    }

    // ── Tiled JSON parsing (private) ──────────────────────────────────────────

    private static TiledMap ParseMap(string jsonPath)
    {
        if (!File.Exists(jsonPath))
            throw new FileNotFoundException($"Tiled map file not found: {jsonPath}");

        string json = File.ReadAllText(jsonPath);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var map = new TiledMap
        {
            Width     = root.GetProperty("width").GetInt32(),
            Height    = root.GetProperty("height").GetInt32(),
            TileWidth  = root.GetProperty("tilewidth").GetInt32(),
            TileHeight = root.GetProperty("tileheight").GetInt32(),
        };

        foreach (var layer in root.GetProperty("layers").EnumerateArray())
        {
            string layerType = layer.GetProperty("type").GetString();

            if      (layerType == "tilelayer")   map.TileLayers.Add(ParseTileLayer(layer, map.Width, map.Height));
            else if (layerType == "objectgroup") map.ObjectLayers.Add(ParseObjectLayer(layer));
            else if (layerType == "group")       ParseGroupLayer(layer, map);
        }

        return map;
    }

    private static void ParseGroupLayer(JsonElement group, TiledMap map)
    {
        if (!group.TryGetProperty("layers", out var layers)) return;

        foreach (var layer in layers.EnumerateArray())
        {
            string layerType = layer.GetProperty("type").GetString();

            if      (layerType == "tilelayer")   map.TileLayers.Add(ParseTileLayer(layer, map.Width, map.Height));
            else if (layerType == "objectgroup") map.ObjectLayers.Add(ParseObjectLayer(layer));
            else if (layerType == "group")       ParseGroupLayer(layer, map);
        }
    }

    private static TiledTileLayer ParseTileLayer(JsonElement layer, int mapWidth, int mapHeight)
    {
        int width  = layer.TryGetProperty("width",  out var w) ? w.GetInt32() : mapWidth;
        int height = layer.TryGetProperty("height", out var h) ? h.GetInt32() : mapHeight;

        var tileLayer = new TiledTileLayer
        {
            Name    = layer.GetProperty("name").GetString(),
            Visible = !layer.TryGetProperty("visible", out var vis) || vis.GetBoolean(),
            Opacity = layer.TryGetProperty("opacity",  out var op)  ? (float)op.GetDouble() : 1f,
            OffsetX = layer.TryGetProperty("offsetx",  out var ox)  ? ox.GetInt32() : 0,
            OffsetY = layer.TryGetProperty("offsety",  out var oy)  ? oy.GetInt32() : 0,
            MapData = new int[height, width],
        };

        int i = 0;
        foreach (var gid in layer.GetProperty("data").EnumerateArray())
        {
            // Use uint: flipped/rotated tiles have bits 29-31 set, exceeding int.MaxValue.
            uint rawGid = gid.GetUInt32();
            int tileId  = (int)(rawGid & 0x0FFFFFFFu); // strip flip flags

            tileLayer.MapData[i / width, i % width] = tileId == 0 ? -1 : tileId - 1;
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
                Id     = obj.GetProperty("id").GetInt32(),
                Name   = obj.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                // Tiled 1.9+ uses "class"; older versions use "type".
                Type   = obj.TryGetProperty("class", out var cls) && cls.GetString() is { Length: > 0 } clsStr
                             ? clsStr
                             : obj.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "",
                X      = (float)obj.GetProperty("x").GetDouble(),
                Y      = (float)obj.GetProperty("y").GetDouble(),
                Width  = obj.TryGetProperty("width",  out var ow) ? (float)ow.GetDouble() : 0,
                Height = obj.TryGetProperty("height", out var oh) ? (float)oh.GetDouble() : 0,
            };

            if (obj.TryGetProperty("properties", out var props))
            {
                foreach (var prop in props.EnumerateArray())
                {
                    string propName  = prop.GetProperty("name").GetString();
                    string propValue = prop.GetProperty("value").ToString();
                    tiledObj.Properties[propName] = propValue;
                }
            }

            objectLayer.Objects.Add(tiledObj);
        }

        return objectLayer;
    }

    private static List<GameObject> BuildTilemapObjects(
        Scene scene,
        TiledMap map,
        Texture2D tileset,
        float baseLayer,
        int[] solidTileIndices)
    {
        var objects = new List<GameObject>();
        float layer = baseLayer;

        for (int i = 0; i < map.TileLayers.Count; i++)
        {
            var tileLayer = map.TileLayers[i];

            var go = scene.AddGameObject<GameObject>($"tilemap_{tileLayer.Name}");

            if (tileLayer.OffsetX != 0 || tileLayer.OffsetY != 0)
                go.Position = new Vector2(tileLayer.OffsetX, tileLayer.OffsetY);

            var tilemap = go.AddComponent<Tilemap>();
            tilemap.Tileset             = tileset;
            tilemap.SourceTileSize      = map.TileWidth;
            tilemap.DestinationTileSize = map.TileWidth;
            tilemap.Layer               = layer;
            tilemap.MapData             = tileLayer.MapData;

            if (solidTileIndices != null && i == 0)
            {
                var collider = go.AddComponent<TileCollider>();
                collider.SetSolid(solidTileIndices);
            }

            objects.Add(go);
            layer -= 0.01f;
        }

        return objects;
    }

    // ── Result ────────────────────────────────────────────────────────────────

    public sealed class LoadResult
    {
        /// <summary>All tilemap GameObjects created from tile layers.</summary>
        public List<GameObject> Tilemaps { get; }

        /// <summary>All GameObjects spawned from object layers.</summary>
        public List<GameObject> SpawnedObjects { get; }

        /// <summary>The raw parsed map data.</summary>
        public TiledMap Map { get; }

        /// <summary>Object types present in the map that had no registered factory.</summary>
        public IReadOnlyCollection<string> UnregisteredTypes { get; }

        internal LoadResult(
            List<GameObject> tilemaps,
            List<GameObject> spawned,
            TiledMap map,
            IReadOnlyCollection<string> unregistered)
        {
            Tilemaps          = tilemaps;
            SpawnedObjects    = spawned;
            Map               = map;
            UnregisteredTypes = unregistered;
        }

        /// <summary>Enumerate all spawned objects of a specific type.</summary>
        public IEnumerable<T> GetSpawned<T>() where T : GameObject
        {
            foreach (var go in SpawnedObjects)
                if (go is T t) yield return t;
        }
    }
}

// ── Property helpers ──────────────────────────────────────────────────────────

/// <summary>
/// Convenience extensions for reading Tiled custom properties with defaults.
/// </summary>
public static class TiledObjectExtensions
{
    public static string StringProp(
        this MapLoader.TiledObject obj, string key, string defaultValue = "")
        => obj.Properties.TryGetValue(key, out var v) ? v : defaultValue;

    public static float FloatProp(
        this MapLoader.TiledObject obj, string key, float defaultValue = 0f)
        => obj.Properties.TryGetValue(key, out var v)
           && float.TryParse(v, System.Globalization.NumberStyles.Float,
                             System.Globalization.CultureInfo.InvariantCulture, out float f)
            ? f : defaultValue;

    public static int IntProp(
        this MapLoader.TiledObject obj, string key, int defaultValue = 0)
        => obj.Properties.TryGetValue(key, out var v)
           && int.TryParse(v, out int i) ? i : defaultValue;

    public static bool BoolProp(
        this MapLoader.TiledObject obj, string key, bool defaultValue = false)
        => obj.Properties.TryGetValue(key, out var v)
           && bool.TryParse(v, out bool b) ? b : defaultValue;

    public static T EnumProp<T>(
        this MapLoader.TiledObject obj, string key, T defaultValue = default)
        where T : struct, Enum
        => obj.Properties.TryGetValue(key, out var v)
           && Enum.TryParse<T>(v, ignoreCase: true, out var e) ? e : defaultValue;
}
