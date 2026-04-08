using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using WaddleAndGrapple.Engine.Utils;

namespace WaddleAndGrapple.Engine.Managers;

public sealed class ProgressionManager
{
    public static ProgressionManager Instance { get; } = new ProgressionManager();

    private readonly Dictionary<int, LevelProgression> _levels = [];
    private readonly HashSet<string> _playedCutscenes = [];

    public int CurrentLevelIndex { get; private set; }
    public int LastCompletedLevelIndex { get; private set; }
    public TimeSpan LastCompletionTime { get; private set; }
    public int LastCompletionCollectedFishCount { get; private set; }
    public int LastCompletionTotalFishCount { get; private set; }

    public string SaveFilePath { get; set; } = Path.Combine(
        Directory.GetCurrentDirectory(),
        "Game",
        "Save",
        "Progression.json"
    );

    private ProgressionManager() { }

    public void RegisterLevel(int levelIndex, int totalFishCount = 0)
    {
        var progression = GetOrCreateLevel(levelIndex);
        if (totalFishCount > 0)
        {
            progression.TotalFishCount = totalFishCount;
        }
    }

    public LevelProgression GetLevelProgression(int levelIndex)
    {
        _levels.TryGetValue(levelIndex, out var progression);
        return progression;
    }

    public bool HasPlayedCutscene(string cutsceneKey)
    {
        if (string.IsNullOrWhiteSpace(cutsceneKey))
        {
            return false;
        }

        return _playedCutscenes.Contains(cutsceneKey);
    }

    public void MarkCutscenePlayed(string cutsceneKey)
    {
        if (string.IsNullOrWhiteSpace(cutsceneKey))
        {
            return;
        }

        _playedCutscenes.Add(cutsceneKey);
    }

    public LevelProgression GetOrCreateLevel(int levelIndex)
    {
        if (!_levels.TryGetValue(levelIndex, out var progression))
        {
            progression = new LevelProgression(levelIndex);
            _levels[levelIndex] = progression;
        }

        return progression;
    }

    public bool CanPlayLevel(int levelIndex)
    {
        if (levelIndex <= 1)
        {
            return true;
        }

        var previousLevel = GetLevelProgression(levelIndex - 1);
        return previousLevel?.IsCompleted == true;
    }

    public void StartLevel(int levelIndex)
    {
        foreach (var progression in _levels.Values)
        {
            progression.IsCurrentLevel = false;
        }

        CurrentLevelIndex = levelIndex;
        var current = GetOrCreateLevel(levelIndex);
        current.IsCurrentLevel = true;
    }

    public bool HasCheckpointProgress(int levelIndex)
    {
        var progression = GetLevelProgression(levelIndex);
        if (progression == null || progression.IsCompleted)
        {
            return false;
        }

        return progression.CheckpointPosition.HasValue
            || progression.CurrentLevelTime > TimeSpan.Zero
            || progression.CurrentCollectedFishCount > 0;
    }

    public void ClearCheckpointProgress(int levelIndex)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.CurrentLevelTime = TimeSpan.Zero;
        progression.CheckpointPosition = null;
        progression.CurrentCollectedFishCount = 0;
        progression.IsCurrentLevel = false;
    }

    public void UpdateCurrentLevelTime(int levelIndex, TimeSpan time)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.CurrentLevelTime = time;
    }

    public void UpdateCheckpoint(int levelIndex, Vector2 checkpointPosition)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.CheckpointPosition = checkpointPosition;
    }

    public void SaveCheckpoint(
        int levelIndex,
        Vector2 checkpointPosition,
        TimeSpan currentLevelTime,
        int collectedFishCount,
        int totalFishCount)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.CheckpointPosition = checkpointPosition;
        progression.CurrentLevelTime = currentLevelTime;
        progression.CurrentCollectedFishCount = Math.Max(0, collectedFishCount);

        if (progression.CurrentCollectedFishCount > progression.BestCollectedFishCount)
        {
            progression.BestCollectedFishCount = progression.CurrentCollectedFishCount;
        }

        if (totalFishCount > 0)
        {
            progression.TotalFishCount = totalFishCount;
        }

        SaveToFile(SaveFilePath);
    }

    public void UpdateFishProgress(int levelIndex, int collectedFishCount, int totalFishCount)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.CurrentCollectedFishCount = Math.Max(0, collectedFishCount);

        if (progression.CurrentCollectedFishCount > progression.BestCollectedFishCount)
        {
            progression.BestCollectedFishCount = progression.CurrentCollectedFishCount;
        }

        if (totalFishCount > 0)
        {
            progression.TotalFishCount = totalFishCount;
        }
    }

    public void MarkLevelAbandoned(
        int levelIndex,
        TimeSpan currentLevelTime,
        Vector2? checkpointPosition,
        int collectedFishCount,
        int totalFishCount)
    {
        var progression = GetOrCreateLevel(levelIndex);
        progression.IsCurrentLevel = false;
        progression.CurrentLevelTime = currentLevelTime;
        progression.CurrentCollectedFishCount = Math.Max(0, collectedFishCount);

        if (progression.CurrentCollectedFishCount > progression.BestCollectedFishCount)
        {
            progression.BestCollectedFishCount = progression.CurrentCollectedFishCount;
        }

        if (totalFishCount > 0)
        {
            progression.TotalFishCount = totalFishCount;
        }

        if (checkpointPosition.HasValue)
        {
            progression.CheckpointPosition = checkpointPosition.Value;
        }

        if (CurrentLevelIndex == levelIndex)
        {
            CurrentLevelIndex = 0;
        }

        SaveToFile(SaveFilePath);
    }

    public void CompleteLevel(
        int levelIndex,
        TimeSpan completionTime,
        int collectedFishCount,
        int totalFishCount,
        Vector2? checkpointPosition = null)
    {
        var progression = GetOrCreateLevel(levelIndex);

        progression.IsCompleted = true;
        progression.IsCurrentLevel = false;
        progression.CurrentLevelTime = TimeSpan.Zero;
        progression.CurrentCollectedFishCount = Math.Max(0, collectedFishCount);

        if (progression.CurrentCollectedFishCount > progression.BestCollectedFishCount)
        {
            progression.BestCollectedFishCount = progression.CurrentCollectedFishCount;
        }

        if (totalFishCount > 0)
        {
            progression.TotalFishCount = totalFishCount;
        }

        if (checkpointPosition.HasValue)
        {
            progression.CheckpointPosition = checkpointPosition.Value;
        }

        if (!progression.BestCompletionTime.HasValue || completionTime < progression.BestCompletionTime.Value)
        {
            progression.BestCompletionTime = completionTime;
        }

        LastCompletedLevelIndex = levelIndex;
        LastCompletionTime = completionTime;
        LastCompletionCollectedFishCount = progression.CurrentCollectedFishCount;
        LastCompletionTotalFishCount = progression.TotalFishCount;

        if (CurrentLevelIndex == levelIndex)
        {
            CurrentLevelIndex = 0;
        }

        SaveToFile(SaveFilePath);
    }

    public void SaveToFile(string filePath)
    {
        var saveData = new ProgressionSaveData
        {
            Levels = [.._levels.Values],
            PlayedCutscenes = [.._playedCutscenes],
            CurrentLevelIndex = CurrentLevelIndex,
            LastCompletedLevelIndex = LastCompletedLevelIndex,
            LastCompletionTime = LastCompletionTime,
            LastCompletionCollectedFishCount = LastCompletionCollectedFishCount,
            LastCompletionTotalFishCount = LastCompletionTotalFishCount
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new Vector2JsonConverter() }
        };

        string directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string json = JsonSerializer.Serialize(saveData, options);
        File.WriteAllText(filePath, json);
    }

    public void LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            SaveToFile(filePath);
            return;
        }

        var options = new JsonSerializerOptions
        {
            Converters = { new Vector2JsonConverter() }
        };

        string json = File.ReadAllText(filePath);
        var saveData = JsonSerializer.Deserialize<ProgressionSaveData>(json, options);

        if (saveData == null)
        {
            return;
        }

        _levels.Clear();
        _playedCutscenes.Clear();

        foreach (var level in saveData.Levels)
        {
            _levels[level.LevelIndex] = level;
        }

        foreach (var cutscene in saveData.PlayedCutscenes)
        {
            _playedCutscenes.Add(cutscene);
        }

        CurrentLevelIndex = saveData.CurrentLevelIndex;
        LastCompletedLevelIndex = saveData.LastCompletedLevelIndex;
        LastCompletionTime = saveData.LastCompletionTime;
        LastCompletionCollectedFishCount = saveData.LastCompletionCollectedFishCount;
        LastCompletionTotalFishCount = saveData.LastCompletionTotalFishCount;
    }
}

public sealed class ProgressionSaveData
{
    public List<LevelProgression> Levels { get; set; } = [];
    public List<string> PlayedCutscenes { get; set; } = [];
    public int CurrentLevelIndex { get; set; }
    public int LastCompletedLevelIndex { get; set; }
    public TimeSpan LastCompletionTime { get; set; }
    public int LastCompletionCollectedFishCount { get; set; }
    public int LastCompletionTotalFishCount { get; set; }
}

public sealed class LevelProgression
{
    public int LevelIndex { get; }
    public bool IsCompleted { get; set; }
    public TimeSpan? BestCompletionTime { get; set; }
    public bool IsCurrentLevel { get; set; }
    public TimeSpan CurrentLevelTime { get; set; }
    public Vector2? CheckpointPosition { get; set; }
    public int CurrentCollectedFishCount { get; set; }
    public int BestCollectedFishCount { get; set; }
    public int TotalFishCount { get; set; }

    public LevelProgression(int levelIndex)
    {
        LevelIndex = levelIndex;
    }
}
