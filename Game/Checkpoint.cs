using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Defines a section (room) within the level.
///
/// Sections are numbered left-to-right starting at 0.
/// Each section has two spawn points:
///   - LeftSpawnPoint:  player spawns here when they entered from the LEFT side
///                      (walked right from the previous section)
///   - RightSpawnPoint: player spawns here when they entered from the RIGHT side
///                      (walked left from the next section)
///
/// Example layout:
///   [Section 0] -------- [Section 1] -------- [Section 2]
///
///   Walk right into Section 1  → LeftSpawnPoint  of Section 1 saved.
///   Walk back left into Section 0 → RightSpawnPoint of Section 0 saved.
///   Die / reset → respawn at the last saved spawn point.
/// </summary>
public class Section
{
    /// <summary>Unique ID, ordered left-to-right (0, 1, 2, ...).</summary>
    public int Id { get; set; }

    /// <summary>Left X boundary of this section in world space.</summary>
    public float LeftBound { get; set; }

    /// <summary>Right X boundary of this section in world space.</summary>
    public float RightBound { get; set; }

    /// <summary>
    /// Spawn point at the LEFT edge of this section.
    /// Used when the player entered from the left (came from the previous section).
    /// </summary>
    public Vector2 LeftSpawnPoint { get; set; }

    /// <summary>
    /// Spawn point at the RIGHT edge of this section.
    /// Used when the player entered from the right (came from the next section).
    /// </summary>
    public Vector2 RightSpawnPoint { get; set; }

    public bool Contains(float x) => x >= LeftBound && x <= RightBound;
}