using Microsoft.Xna.Framework;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Managers;
using System;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using WaddleAndGrapple.Engine.Components;
using Microsoft.Xna.Framework.Graphics;
using WaddleAndGrapple.Engine.UI;
using Microsoft.Xna.Framework.Input;

namespace WaddleAndGrapple.Game.Scenes;

public class LevelSelect : Scene
{
    private const int PortraitCount = 3;
    private const float PortraitSpacing = 40f;

    private Panel _checkpointPrompt;
    private int _pendingLevelIndex;

    public override void Setup()
    {
        GumService.Default.Root.Children.Clear(); // Clear any existing Gum UI elements

        float screenWidth = ScreenManager.Instance.nativeWidth;
        float screenHeight = ScreenManager.Instance.nativeHeight;

        // Background
        var background = base.AddGameObject<GameObject>("bg");
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

        CreateLevelPortraits(screenWidth, screenHeight);

        GameObject levelSelectTextObj = base.AddGameObject<GameObject>("LevelSelectText");
        Text levelSelectText = levelSelectTextObj.AddComponent<Text>();
        levelSelectText.Font = ResourceManager.Instance.GetFont("Fonts/36Font");
        levelSelectText.Content = "LEVEL SELECT";
        levelSelectText.Origin = levelSelectText.MeasureText() / 2f;
        levelSelectText.Offset = new Vector2(510f, 120f);

        CreateCheckpointPrompt();
    }

    public override void Update(GameTime gameTime)
    {
        if (InputManager.Instance.IsKeyPressed(Keys.Escape))
        {
            if (_checkpointPrompt != null && _checkpointPrompt.IsVisible)
            {
                HideCheckpointPrompt();
                return;
            }

            GumService.Default.Root.Children.Clear();
            SceneManager.Instance.LoadScene("main");
        }

        base.Update(gameTime);
    }

    private void CreateLevelPortraits(float screenWidth, float screenHeight)
    {
        Texture2D lockedTexture = ResourceManager.Instance.GetTexture("UI/LockLevel");
        Texture2D[] levelTextures =
        [
            ResourceManager.Instance.GetTexture("UI/Level1"),
            ResourceManager.Instance.GetTexture("UI/Level2"),
            ResourceManager.Instance.GetTexture("UI/Level3")
        ];

        float portraitWidth = levelTextures[0]?.Width
            ?? lockedTexture?.Width
            ?? 0f;

        float totalWidth = (portraitWidth * PortraitCount) + (PortraitSpacing * (PortraitCount - 1));
        float startX = 215f;
        float y = 210f;

        for (int i = 0; i < PortraitCount; i++)
        {
            int levelIndex = i + 1;
            bool isLocked = !ProgressionManager.Instance.CanPlayLevel(levelIndex);
            string gameObjectName = $"LevelPortrait_{i + 1}";
            string levelName = $"LEVEL {i + 1}";

            Texture2D levelTexture = levelTextures[i] ?? lockedTexture;
            var portrait = new LevelPortrait(levelName, lockedTexture, levelTexture, isLocked);
            base.AddGameObject(gameObjectName, portrait);
            portrait.Position = new Vector2(startX + (i * (portraitWidth + PortraitSpacing)), y);

            var progression = ProgressionManager.Instance.GetLevelProgression(levelIndex);
            portrait.SetStats(
                progression?.BestCompletionTime,
                progression?.BestCollectedFishCount,
                progression != null && progression.TotalFishCount > 0
                    ? progression.TotalFishCount
                    : null);

            portrait.OnClick = () => OnLevelPortraitClick(levelIndex, portrait.IsLocked);
        }
    }

    private void CreateCheckpointPrompt()
    {
        _checkpointPrompt = new Panel();
        _checkpointPrompt.Dock(Gum.Wireframe.Dock.Fill);
        _checkpointPrompt.IsVisible = false;
        _checkpointPrompt.AddToRoot();

        var blackOverlay = new ColoredRectangleRuntime();
        blackOverlay.Dock(Gum.Wireframe.Dock.Fill);
        blackOverlay.Color = new Color(0, 0, 0, 160);
        _checkpointPrompt.AddChild(blackOverlay);

        var centeredPanel = new Panel();
        centeredPanel.Anchor(Gum.Wireframe.Anchor.Center);
        centeredPanel.Width = 150;
        centeredPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        centeredPanel.Height = 30;
        centeredPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        _checkpointPrompt.AddChild(centeredPanel);

        var background = new ColoredRectangleRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Color = Color.DarkBlue;
        centeredPanel.AddChild(background);

        var message = new TextRuntime();
        message.Text = "Resume from latest checkpoint?";
        message.Anchor(Gum.Wireframe.Anchor.Top);
        message.X = 0;
        message.Y = 0;
        centeredPanel.AddChild(message);

        var buttonStackPanel = new StackPanel();
        buttonStackPanel.Anchor(Gum.Wireframe.Anchor.CenterHorizontally);
        buttonStackPanel.X = 5;
        buttonStackPanel.Y = 30;
        buttonStackPanel.Visual.ChildrenLayout = Gum.Managers.ChildrenLayout.LeftToRightStack;
        buttonStackPanel.Width = 10;
        buttonStackPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        buttonStackPanel.Height = 10;
        buttonStackPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        buttonStackPanel.Spacing = 4;
        centeredPanel.AddChild(buttonStackPanel);

        var resumeButton = new Gum.Forms.Controls.Button();
        resumeButton.Text = "Yes";
        resumeButton.Click += (_, _) => ResumeFromCheckpoint();
        buttonStackPanel.AddChild(resumeButton);

        var newRunButton = new Gum.Forms.Controls.Button();
        newRunButton.Text = "Restart Level";
        newRunButton.Click += (_, _) => StartNewRun();
        buttonStackPanel.AddChild(newRunButton);

        var cancelButton = new Gum.Forms.Controls.Button();
        cancelButton.Text = "Cancel";
        cancelButton.Click += (_, _) => HideCheckpointPrompt();
        buttonStackPanel.AddChild(cancelButton);
    }

    private void ShowCheckpointPrompt(int levelIndex)
    {
        _pendingLevelIndex = levelIndex;
        _checkpointPrompt.IsVisible = true;
    }

    private void HideCheckpointPrompt()
    {
        _pendingLevelIndex = 0;
        _checkpointPrompt.IsVisible = false;
    }

    private void ResumeFromCheckpoint()
    {
        if (_pendingLevelIndex <= 0)
        {
            HideCheckpointPrompt();
            return;
        }

        int levelIndex = _pendingLevelIndex;
        HideCheckpointPrompt();
        LoadLevel(levelIndex, isNewRun: false);
    }

    private void StartNewRun()
    {
        if (_pendingLevelIndex <= 0)
        {
            HideCheckpointPrompt();
            return;
        }

        int levelIndex = _pendingLevelIndex;
        ProgressionManager.Instance.ClearCheckpointProgress(levelIndex);

        HideCheckpointPrompt();
        LoadLevel(levelIndex, isNewRun: true);
    }

    private void LoadLevel(int levelIndex, bool isNewRun = true)
    {
        string sceneName = "Level" + levelIndex;

        // Play the intro cutscene on every fresh Level 1 run.
        if (levelIndex < 3 && isNewRun)
        {
            sceneName = "Level" + levelIndex + "IntroCutscene";
        }

        Console.WriteLine($"Loading scene: {sceneName}");
        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene(sceneName);
    }

    private void OnLevelPortraitClick(int levelIndex, bool isLocked)
    {
        if (isLocked)
        {
            Console.WriteLine($"Level {levelIndex} is locked.");
            return;
        }

        if (ProgressionManager.Instance.HasCheckpointProgress(levelIndex))
        {
            ShowCheckpointPrompt(levelIndex);
            return;
        }

        LoadLevel(levelIndex, isNewRun: true);
    }

    
}
