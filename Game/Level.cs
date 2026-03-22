using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Components.Tile;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace ComputerGameFinal.Game;

/// <summary>
/// Base class for a game level. Extend this class for each level (Level1, Level2, etc.).
/// Responsible for setting up tilemap, traps, CCTVs, and checkpoints.
/// </summary>
public abstract class Level : Scene
{
    protected Player Player { get; private set; }

    // Starting position for the player in this level
    protected virtual Vector2 PlayerStartPosition => new Vector2(100, 100);

    public override void Setup()
    {
        CheckpointManager.Instance.Reset();
        SetupCamera();
        SetupTilemap();
        SetupTraps();
        SetupCCTVs();
        SetupSections();  // must run before SetupPlayer so spawn position is ready
        SetupPlayer();
    }

    private void SetupCamera()
    {
        var cameraObject = AddGameObject<GameObject>("camera");
        var camera = cameraObject.AddComponent<Camera2D>();
        camera.SetViewport(new Viewport(0, 0, 800, 600));
        camera.Zoom = 1f;
        Camera = camera;
    }

    private void SetupPlayer()
    {
        Player = AddGameObject<Player>("player");
        Player.StartPosition = PlayerStartPosition;
        // On a fresh level load the manager was just reset, so this returns PlayerStartPosition.
        // After a scene reload that preserves checkpoint state it returns the correct spawn point.
        Player.Position = CheckpointManager.Instance.GetRespawnPosition(PlayerStartPosition);
        Player.Scale = new Vector2(0.1f, 0.1f);

        Camera.FollowTarget = Player;
    }

    // --- Override these in each Level subclass ---

    /// <summary>Load and place tilemap for this level.</summary>
    protected abstract void SetupTilemap();

    /// <summary>Place all traps (LaserTrap, SawTrap) in this level.</summary>
    protected virtual void SetupTraps() { }

    /// <summary>Place all CCTV cameras in this level.</summary>
    protected virtual void SetupCCTVs() { }

    /// <summary>
    /// Define all sections (rooms) for this level and register them with CheckpointManager.
    /// Override in each Level subclass. Use AddSection() to build each section.
    /// </summary>
    protected virtual void SetupSections() { }

    // --- Helper methods for subclasses ---

    /// <summary>Add a trap to the scene at a given position.</summary>
    protected T AddTrap<T>(string name, Vector2 position) where T : Trap, new()
    {
        var trap = AddGameObject<T>(name);
        trap.Position = position;
        return trap;
    }

    /// <summary>Add a CCTV to the scene at a given position.</summary>
    protected CCTV AddCCTV(string name, Vector2 position)
    {
        var cctv = AddGameObject<CCTV>(name);
        cctv.Position = position;
        return cctv;
    }

    /// <summary>
    /// Register all sections at once. Call from SetupSections() in your level subclass.
    /// Sections must be ordered left-to-right by Id (0, 1, 2, ...).
    /// </summary>
    protected void RegisterSections(params Section[] sections)
    {
        CheckpointManager.Instance.RegisterSections(sections);
    }
}
