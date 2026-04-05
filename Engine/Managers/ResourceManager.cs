using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;

namespace WaddleAndGrapple.Engine.Managers;

public class ResourceManager
{
    public static ResourceManager Instance { get; private set; } = new ResourceManager();

    private readonly Dictionary<string, Texture2D> _textures = [];
    private readonly Dictionary<string, SpriteFont> _fonts = [];
    private readonly Dictionary<string, SoundEffect> _sounds = [];
    private readonly Dictionary<string, Song> _songs = [];

    private ResourceManager() { }

    public void LoadTexture(string name, Texture2D texture) => _textures[name] = texture;
    public void LoadFont(string name, SpriteFont font) => _fonts[name] = font;
    public void LoadSound(string name, SoundEffect sound) => _sounds[name] = sound;
    public void LoadSong(string name, Song song) => _songs[name] = song;

    public Texture2D GetTexture(string name) => _textures.TryGetValue(name, out var texture) ? texture : null;
    public SpriteFont GetFont(string name) => _fonts.TryGetValue(name, out var font) ? font : null;
    public SoundEffect GetSound(string name) => _sounds.TryGetValue(name, out var sound) ? sound : null;
    public Song GetSong(string name) => _songs.TryGetValue(name, out var song) ? song : null;

    /// <summary>
    /// Scans the entire Content folder (or a sub-folder) and auto-loads every
    /// .xnb asset, detecting its type automatically.
    /// The key stored is the filename without extension (e.g. "player_idle").
    /// </summary>
    /// <example>
    ///   ResourceManager.Instance.LoadAll(Content);           // whole Content/
    ///   ResourceManager.Instance.LoadAll(Content, "Sprites"); // sub-folder
    /// </example>
    public void LoadAll(ContentManager content, string subFolder = "")
    {
        string searchRoot = string.IsNullOrEmpty(subFolder)
            ? content.RootDirectory
            : Path.Combine(content.RootDirectory, subFolder);

        if (!Directory.Exists(searchRoot))
            return;

        foreach (string filePath in Directory.GetFiles(searchRoot, "*.xnb", SearchOption.AllDirectories))
        {
            // assetName is relative to content.RootDirectory — used for content.Load<T>()
            string relative  = Path.GetRelativePath(content.RootDirectory, filePath).Replace('\\', '/');

            // key = path after the last "/Content/" segment, e.g.
            //   "bin/DesktopGL/Content/Sprites/player" → "Sprites/player"
            //   "bin/DesktopGL/Content/bird"           → "bird"
            int contentIdx = relative.LastIndexOf("Content/", StringComparison.OrdinalIgnoreCase);
            string keyPath = contentIdx >= 0
                ? relative[(contentIdx + "Content/".Length)..]
                : relative;
            string assetName = Path.ChangeExtension(keyPath, null);

            Console.WriteLine($"[ResourceManager] Loading asset: '{assetName}'");

            // Try each supported type in turn; skip on mismatch
            if (TryLoad<Texture2D>(content, assetName, out var tex))       { _textures[assetName] = tex; continue; }
            if (TryLoad<SpriteFont>(content, assetName, out var font))     { _fonts[assetName]    = font; continue; }
            if (TryLoad<SoundEffect>(content, assetName, out var sfx))     { _sounds[assetName]   = sfx; continue; }
            if (TryLoad<Song>(content, assetName, out var song))           { _songs[assetName]    = song; continue; }

            Console.WriteLine($"[ResourceManager] Unknown asset type, skipped: '{assetName}'");
        }
    }

    private static bool TryLoad<T>(ContentManager content, string assetName, out T result) where T : class
    {
        try
        {
            result = content.Load<T>(assetName);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }
}