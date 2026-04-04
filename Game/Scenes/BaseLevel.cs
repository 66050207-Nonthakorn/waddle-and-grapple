using System;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Components.Physics;
using ComputerGameFinal.Engine.Managers;
using ComputerGameFinal.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

public abstract class BaseLevel : Scene
{
    private PausedPanel _pausedPanel;
    private bool isPaused = false;
    private TimerUI _timerUI;
    private bool _isLevelCompleted;
    private bool _skipAbandonSaveOnUnload;
    private GameObject _trackedPlayer;
    private Vector2 _latestCheckpoint;
    private bool _hasLatestCheckpoint;
    private int _collectedFishCount;
    private int _totalFishInLevel;
    public int LevelIndex;

    // called base update later so that the UI are on top of the game objects
    public override void Setup()
    {
        GumService.Default.Root.Children.Clear(); // Clear any existing Gum UI elements

        _pausedPanel = new PausedPanel(TogglePause, ResetLevel);
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

    private void UpdateRuntimeProgress()
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

    private Vector2? GetLatestCheckpoint()
    {
        return _hasLatestCheckpoint ? _latestCheckpoint : null;
    }

    private void QuickRestartFromLatestCheckpoint()
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
        ProgressionManager.Instance.ClearCheckpointProgress(LevelIndex);
        _skipAbandonSaveOnUnload = true;

        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene("Level" + LevelIndex);
    }

    private void TogglePause()
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