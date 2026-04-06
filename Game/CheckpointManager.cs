using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Singleton that tracks which section the player is currently in and from which
/// direction they entered, so the correct spawn point is used on death/reset.
///
/// Usage:
///   1. Call RegisterSections() once during level setup.
///   2. Call UpdateSection(playerX) every frame from Player.Update().
///   3. Call GetRespawnPosition() when the player needs to respawn.
///   4. Call Reset() only for a full level restart (not on death).
/// </summary>
public class CheckpointManager
{
    private static CheckpointManager _instance;
    public static CheckpointManager Instance => _instance ??= new CheckpointManager();

    private readonly List<Section> _sections = new();

    /// <summary>The section the player is currently in.</summary>
    public Section ActiveSection { get; private set; }

    /// <summary>
    /// Which side the player entered the active section from.
    /// Left  = they walked right into it (spawn at left edge).
    /// Right = they walked left into it  (spawn at right edge).
    /// </summary>
    public EntryDirection LastEntryDirection { get; private set; } = EntryDirection.Left;

    private CheckpointManager() { }

    /// <summary>Register all sections for the current level. Call once during Setup.</summary>
    public void RegisterSections(IEnumerable<Section> sections)
    {
        _sections.Clear();
        _sections.AddRange(sections);
    }

    /// <summary>
    /// Call every frame with the player's world X position.
    /// Detects when the player crosses into a new section and saves the entry direction.
    /// </summary>
    public void UpdateSection(float playerX)
    {
        UpdateSection(playerX, float.NaN);
    }

    public void UpdateSection(float playerX, float playerY)
    {
        var newSection = FindSection(playerX);
        if (newSection == null) return;

        // First frame: initialise with no prior section (entered from the left / level start)
        if (ActiveSection == null)
        {
            ActiveSection = newSection;
            LastEntryDirection = EntryDirection.Left;

            if (!float.IsNaN(playerY))
            {
                ActiveSection.LeftSpawnPoint = new Vector2(ActiveSection.LeftSpawnPoint.X, playerY);
            }
            return;
        }

        if (newSection.Id == ActiveSection.Id) return;

        // Player moved right into a higher-ID section → entered from the left side of that section.
        // Player moved left into a lower-ID section  → entered from the right side of that section.
        LastEntryDirection = newSection.Id > ActiveSection.Id
            ? EntryDirection.Left
            : EntryDirection.Right;

        if (!float.IsNaN(playerY))
        {
            if (LastEntryDirection == EntryDirection.Left)
                newSection.LeftSpawnPoint = new Vector2(newSection.LeftSpawnPoint.X, playerY);
            else
                newSection.RightSpawnPoint = new Vector2(newSection.RightSpawnPoint.X, playerY);
        }

        ActiveSection = newSection;
    }

    /// <summary>
    /// Returns the world position where the player should respawn.
    /// Falls back to <paramref name="levelStart"/> if no section has been entered yet.
    /// </summary>
    public Vector2 GetRespawnPosition(Vector2 levelStart)
    {
        if (ActiveSection == null) return levelStart;

        return LastEntryDirection == EntryDirection.Left
            ? ActiveSection.LeftSpawnPoint
            : ActiveSection.RightSpawnPoint;
    }

    /// <summary>Full reset — use only when restarting the level from scratch.</summary>
    public void Reset()
    {
        ActiveSection = null;
        LastEntryDirection = EntryDirection.Left;
    }

    private Section FindSection(float x)
    {
        foreach (var s in _sections)
            if (s.Contains(x)) return s;
        return null;
    }
}

public enum EntryDirection { Left, Right }
