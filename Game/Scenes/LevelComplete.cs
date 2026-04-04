using System;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.UI;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace WaddleAndGrapple.Game.Scenes;

public class LevelComplete : Scene
{
    private const int MaxLevelCount = 3;

    private int _completedLevelIndex;

    public override void Setup()
    {
        GumService.Default.Root.Children.Clear();

        float screenWidth = ScreenManager.Instance.nativeWidth;
        float screenHeight = ScreenManager.Instance.nativeHeight;

        var background = AddGameObject<GameObject>("bg");
        var bgSprite = background.AddComponent<SpriteRenderer>();
        bgSprite.Texture = ResourceManager.Instance.GetTexture("UI/TabletScene");
        background.Position = new Vector2(screenWidth / 2f, screenHeight / 2f);
        if (bgSprite.Texture != null)
        {
            background.Scale = new Vector2(
                screenWidth / bgSprite.Texture.Width,
                screenHeight / bgSprite.Texture.Height
            );
        }

        ResolveCompletedLevelData(out var completionTime, out var collectedFish, out var totalFish);

        CreateText("LevelCompleteTitle", $"LEVEL {_completedLevelIndex} COMPLETED", new Vector2(510f, 160f));
        CreateText("CompletionTime", $"TIME: {FormatTime(completionTime)}", new Vector2(510f, 240f));

        string collectibleText = $"COLLECTIBLES: {collectedFish}/{totalFish}";

        CreateText("CompletionCollectibles", collectibleText, new Vector2(510f, 280f));

        CreateBottomButtons();
    }

    public override void Update(GameTime gameTime)
    {
        if (InputManager.Instance.IsKeyPressed(Keys.Escape))
        {
            GumService.Default.Root.Children.Clear();
            SceneManager.Instance.LoadScene("LevelSelect");
            return;
        }

        base.Update(gameTime);
    }

    private void ResolveCompletedLevelData(out TimeSpan completionTime, out int collectedFish, out int totalFish)
    {
        var progressionManager = ProgressionManager.Instance;

        _completedLevelIndex = progressionManager.LastCompletedLevelIndex;
        completionTime = progressionManager.LastCompletionTime;
        collectedFish = progressionManager.LastCompletionCollectedFishCount;
        totalFish = progressionManager.LastCompletionTotalFishCount;

        if (_completedLevelIndex > 0)
        {
            var levelProgression = progressionManager.GetLevelProgression(_completedLevelIndex);
            if (levelProgression != null && levelProgression.TotalFishCount > 0)
            {
                totalFish = levelProgression.TotalFishCount;
            }

            return;
        }

        _completedLevelIndex = 1;
    }

    private void CreateText(string gameObjectName, string content, Vector2 offset)
    {
        var textObject = AddGameObject<GameObject>(gameObjectName);
        var text = textObject.AddComponent<Text>();
        text.Font = ResourceManager.Instance.GetFont("Fonts/36Font");
        text.Content = content;
        text.Origin = text.MeasureText() / 2f;
        text.Offset = offset;
    }

    private void CreateBottomButtons()
    {
        var buttonStackPanel = new StackPanel();
        buttonStackPanel.Anchor(Gum.Wireframe.Anchor.CenterHorizontally);
        buttonStackPanel.X = 32;
        buttonStackPanel.Y = 340;
        buttonStackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        buttonStackPanel.Width = 10;
        buttonStackPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        buttonStackPanel.Height = 10;
        buttonStackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        buttonStackPanel.Spacing = 8;
        buttonStackPanel.AddToRoot();

        var retryButton = new Gum.Forms.Controls.Button();
        retryButton.Text = "Retry Level";
        retryButton.Click += (_, _) => RetryLevel();
        buttonStackPanel.AddChild(retryButton);

        var returnToSelectButton = new Gum.Forms.Controls.Button();
        returnToSelectButton.Text = "To Level Select";
        returnToSelectButton.Click += (_, _) => ReturnToLevelSelect();
        buttonStackPanel.AddChild(returnToSelectButton);

        var nextLevelButton = new Gum.Forms.Controls.Button();
        bool hasNextLevel = _completedLevelIndex < MaxLevelCount;
        nextLevelButton.Text = hasNextLevel ? "Next Level" : "No Next Level";
        nextLevelButton.IsEnabled = hasNextLevel;
        nextLevelButton.Click += (_, _) => GoToNextLevel();
        buttonStackPanel.AddChild(nextLevelButton);
    }

    private void RetryLevel()
    {
        ProgressionManager.Instance.ClearCheckpointProgress(_completedLevelIndex);
        LoadLevel(_completedLevelIndex);
    }

    private void ReturnToLevelSelect()
    {
        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene("LevelSelect");
    }

    private void GoToNextLevel()
    {
        int nextLevelIndex = _completedLevelIndex + 1;
        if (nextLevelIndex > MaxLevelCount)
        {
            ReturnToLevelSelect();
            return;
        }

        LoadLevel(nextLevelIndex);
    }

    private void LoadLevel(int levelIndex)
    {
        if (levelIndex <= 0)
        {
            ReturnToLevelSelect();
            return;
        }

        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene($"Level{levelIndex}IntroCutscene");
    }

    private static string FormatTime(TimeSpan time)
    {
        return $"{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}";
    }
}
