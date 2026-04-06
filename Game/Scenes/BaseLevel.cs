using System;
using System.Linq;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Game;
using WaddleAndGrapple.Engine.Components.Physics;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

public abstract class BaseLevel : Scene
{
    public override bool IsPlayScene => true;
    public bool IsPaused => isPaused;

    protected PausedPanel _pausedPanel;
    protected bool isPaused = false;
    protected TimerUI _timerUI;
    protected bool _isLevelCompleted;
    protected bool _skipAbandonSaveOnUnload;
    protected GameObject _trackedPlayer;
    protected Vector2 _latestCheckpoint;
    protected bool _hasLatestCheckpoint;
    protected int _collectedFishCount;
    protected int _totalFishInLevel;
    public int LevelIndex;
    protected int MapWidth { get; set; }
    protected int MapHeight { get; set; }

    // called base update later so that the UI are on top of the game objects
    public override void Setup()
    {
        GumService.Default.Root.Children.Clear(); // Clear any existing Gum UI elements
        WorldTime.Reset();

        _pausedPanel = new PausedPanel(TogglePause, ResetLevel, ReturnToLevelSelect);
        _pausedPanel.AddToRoot();

        _timerUI = base.AddGameObject<TimerUI>("timerUI");
        _timerUI.StartTimer();

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.StartLevel(LevelIndex);
            var progression = ProgressionManager.Instance.GetLevelProgression(LevelIndex);
            if (progression != null && !progression.IsCompleted && progression.CurrentLevelTime > TimeSpan.Zero)
            {
                _timerUI.SetElapsedTime((float)progression.CurrentLevelTime.TotalMilliseconds);
            }
        }

        isPaused = false;
        _isLevelCompleted = false;
        _skipAbandonSaveOnUnload = false;
    }

    public override void Update(GameTime gameTime)
    {
        if (InputManager.Instance.IsKeyPressed(Keys.Escape))
        {
            TogglePause();
        }

        if (InputManager.Instance.IsKeyPressed(Keys.F9))
        {
            SaveCheckpointAtPlayer();
        }

        if (InputManager.Instance.IsKeyPressed(Keys.R))
        {
            QuickRestartFromLatestCheckpoint();
        }

        if (InputManager.Instance.IsKeyPressed(Keys.F10))
        {
            CompleteLevel();
            return;
        }

        if (isPaused)
        {
            return; // Skip updating game objects when paused
        }

        base.Update(gameTime);

        // Stop camera following when player falls off the map,
        // and restore follow when the player is back in playable bounds.
        if (_trackedPlayer != null && MapHeight > 0)
        {
            if (_trackedPlayer.Position.Y > MapHeight)
            {
                Camera.FollowTarget = null;
            }
            else if (Camera != null && Camera.FollowTarget == null)
            {
                Camera.FollowTarget = _trackedPlayer;
            }
        }

        // Apply section-based camera clamp (Celeste-style: camera stops at section boundary)
        if (Camera != null)
        {
            var section = CheckpointManager.Instance.ActiveSection;
            if (section != null)
            {
                float halfX = Camera.Origin.X / Camera.Zoom;
                float halfY = Camera.Origin.Y / Camera.Zoom;
                Camera.ClampMinX = section.LeftBound + halfX;
                Camera.ClampMaxX = section.RightBound - halfX;
                Camera.ClampMinY = section.TopBound + halfY;
                Camera.ClampMaxY = section.BottomBound - halfY;
            }
            else
            {
                Camera.ClampMinX = null;
                Camera.ClampMaxX = null;
                Camera.ClampMinY = null;
                Camera.ClampMaxY = null;
            }

            // Apply map bounds for top/bottom clamping
            if (MapWidth > 0 && MapHeight > 0)
            {
                Camera.Bounds = new Rectangle(0, 0, MapWidth, MapHeight);
            }
        }

        // Update timer position after camera movement has been processed for this frame
        if (Camera != null)
        {
            _timerUI.Position = Camera.Position - (Camera.Origin / Camera.Zoom);
        }
        else
        {
            _timerUI.Position = Vector2.Zero;
        }

        UpdateRuntimeProgress();
    }

    public override void Unload()
    {
        if (LevelIndex > 0 && !_isLevelCompleted && !_skipAbandonSaveOnUnload)
        {
            ProgressionManager.Instance.MarkLevelAbandoned(
                LevelIndex,
                TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()),
                GetLatestCheckpoint(),
                _collectedFishCount,
                _totalFishInLevel);
        }

        base.Unload();
    }

    protected void RegisterPlayerForProgression(GameObject player)
    {
        _trackedPlayer = player;
        _latestCheckpoint = player.Position;
        _hasLatestCheckpoint = true;

        var progression = ProgressionManager.Instance.GetLevelProgression(LevelIndex);
        if (progression != null && progression.CheckpointPosition.HasValue)
        {
            _trackedPlayer.Position = progression.CheckpointPosition.Value;
            _latestCheckpoint = progression.CheckpointPosition.Value;
        }

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.UpdateCheckpoint(LevelIndex, _latestCheckpoint);
        }
    }

    protected void SetFishProgress(int collectedFishCount, int totalFishInLevel)
    {
        _collectedFishCount = Math.Max(0, collectedFishCount);
        _totalFishInLevel = Math.Max(0, totalFishInLevel);

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.UpdateFishProgress(LevelIndex, _collectedFishCount, _totalFishInLevel);
        }
    }

    protected void AddCollectedFish(int amount = 1)
    {
        SetFishProgress(_collectedFishCount + amount, _totalFishInLevel);
    }

    protected void SetTotalFish(int totalFishInLevel)
    {
        SetFishProgress(_collectedFishCount, totalFishInLevel);
    }

    protected void UpdateCheckpoint(Vector2 checkpointPosition)
    {
        _latestCheckpoint = checkpointPosition;
        _hasLatestCheckpoint = true;

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.UpdateCheckpoint(LevelIndex, checkpointPosition);
        }
    }

    // Call this from checkpoint triggers later to persist a real checkpoint state.
    protected virtual void SaveCheckpoint(Vector2 checkpointPosition)
    {
        if (LevelIndex <= 0)
        {
            return;
        }

        _latestCheckpoint = checkpointPosition;
        _hasLatestCheckpoint = true;

        ProgressionManager.Instance.SaveCheckpoint(
            LevelIndex,
            checkpointPosition,
            TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()),
            _collectedFishCount,
            _totalFishInLevel);

        Console.WriteLine($"Checkpoint saved at {checkpointPosition}.");
    }

    protected void SaveCheckpointAtPlayer()
    {
        if (_trackedPlayer == null)
        {
            return;
        }

        SaveCheckpoint(_trackedPlayer.Position);
    }

    protected void UpdateRuntimeProgress()
    {
        if (LevelIndex <= 0)
        {
            return;
        }

        ProgressionManager.Instance.UpdateCurrentLevelTime(
            LevelIndex,
            TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()));

        ProgressionManager.Instance.UpdateFishProgress(LevelIndex, _collectedFishCount, _totalFishInLevel);

        var checkpointPosition = GetLatestCheckpoint();
        if (checkpointPosition.HasValue)
        {
            ProgressionManager.Instance.UpdateCheckpoint(LevelIndex, checkpointPosition.Value);
        }
    }

    protected Vector2? GetLatestCheckpoint()
    {
        return _hasLatestCheckpoint ? _latestCheckpoint : null;
    }

    protected void QuickRestartFromLatestCheckpoint()
    {
        if (_trackedPlayer == null || !_hasLatestCheckpoint)
        {
            return;
        }

        _trackedPlayer.Position = _latestCheckpoint;

        var rb = _trackedPlayer.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.Velocity = Vector2.Zero;
        }
    }

    // override this if needed for object in the level
    protected virtual void ResetLevel()
    {
        if (LevelIndex <= 0)
        {
            return;
        }

        isPaused = false;
        _pausedPanel.TogglePause(false);

        // Reset player to last checkpoint
        if (_trackedPlayer is Player player)
        {
            player.Position = _latestCheckpoint;
            player.VelocityX = 0f;
            player.VelocityY = 0f;

            if (player.Pickaxe?.IsHooked == true)
                player.Pickaxe.Recall();
        }

        // Reset all enemies to their spawn positions
        foreach (var enemy in GameObjects.Values.OfType<Enemy>())
            enemy.ResetToSpawn();
    }

    protected virtual void ReturnToLevelSelect()
    {
        isPaused = false;
        SaveCheckpointAtPlayer();

        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene("LevelSelect");
    }

    protected void TogglePause()
    {
        isPaused = !isPaused;
        _pausedPanel.TogglePause(isPaused);
        Console.WriteLine(isPaused ? "Game Paused" : "Game Resumed");
    }

    protected virtual void CompleteLevel() 
    {
        _isLevelCompleted = true;

        if (LevelIndex > 0)
        {
            ProgressionManager.Instance.CompleteLevel(
                LevelIndex,
                TimeSpan.FromMilliseconds(_timerUI.GetElapsedTime()),
                _collectedFishCount,
                _totalFishInLevel,
                GetLatestCheckpoint());
        }

        Console.WriteLine($"Level {LevelIndex} completed!");

        // Open the level complete scene
        SceneManager.Instance.LoadScene("levelcomplete");
    }
}
