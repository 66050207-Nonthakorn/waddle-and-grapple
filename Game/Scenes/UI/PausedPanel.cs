using System;
using ComputerGameFinal.Engine.Managers;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameGum;
using MonoGameGum.GueDeriving;

public class PausedPanel : Panel
{
    private Panel optionPanel;

    public PausedPanel(Action onResume, Action onRestartLevel)
    {
        this.Dock(Gum.Wireframe.Dock.Fill);
        this.IsVisible = false; // Start hidden

        var blackOverlay = new ColoredRectangleRuntime();
        blackOverlay.Dock(Gum.Wireframe.Dock.Fill);
        blackOverlay.Color = new Color(0, 0, 0, 150); // Semi-transparent black
        this.AddChild(blackOverlay);

        var pausedPanel = new Panel();
        pausedPanel.Anchor(Gum.Wireframe.Anchor.Center);
        pausedPanel.Width = 20;
        pausedPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        pausedPanel.Height = 100;
        pausedPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.AddChild(pausedPanel);

        var buttonStackPannel = new StackPanel();
        buttonStackPannel.Dock(Gum.Wireframe.Dock.Fill);
        buttonStackPannel.Width = 300;
        buttonStackPannel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        buttonStackPannel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        buttonStackPannel.Height = 30;
        buttonStackPannel.Spacing = 8;
        pausedPanel.AddChild(buttonStackPannel);

        var pausedText = new TextRuntime();
        pausedText.Text = "PAUSED";
        pausedText.Anchor(Gum.Wireframe.Anchor.Top);
        pausedText.Y = 10;
        pausedPanel.AddChild(pausedText);

        Button returnToGameButton = new Button();
        returnToGameButton.Text = "Return to Game";
        returnToGameButton.Anchor(Gum.Wireframe.Anchor.Top);
        returnToGameButton.Click += (_, _) => HandleReturnToGameButtonClicked(onResume);
        buttonStackPannel.AddChild(returnToGameButton);

        Button restartLevelButton = new Button();
        restartLevelButton.Text = "Restart Level";
        restartLevelButton.Anchor(Gum.Wireframe.Anchor.Top);
        restartLevelButton.Click += (_, _) => HandleRestartLevelButtonClicked(onRestartLevel);
        buttonStackPannel.AddChild(restartLevelButton);

        optionPanel = new OptionPanel(this);
        optionPanel.AddToRoot();

        Button optionsButton = new Button();
        optionsButton.Text = "Settings";
        optionsButton.Anchor(Gum.Wireframe.Anchor.Top);
        optionsButton.Click += (_, _) => HandleOptionsButtonClicked(optionPanel);
        buttonStackPannel.AddChild(optionsButton);

        Button returnToLevelSelectButton = new Button();
        returnToLevelSelectButton.Text = "Return to Level Select";
        returnToLevelSelectButton.Anchor(Gum.Wireframe.Anchor.Top);
        returnToLevelSelectButton.Click += (_, _) => HandleReturnToLevelSelectButtonClicked();
        buttonStackPannel.AddChild(returnToLevelSelectButton);

        Button returnToMainMenuButton = new Button();
        returnToMainMenuButton.Text = "Return to Main Menu";
        returnToMainMenuButton.Anchor(Gum.Wireframe.Anchor.Top);
        returnToMainMenuButton.Click += (_, _) => HandleReturnToMainMenuButtonClicked();
        buttonStackPannel.AddChild(returnToMainMenuButton);
    }

    private void HandleReturnToGameButtonClicked(Action onResume)
    {
        this.IsVisible = false;
        optionPanel.IsVisible = false;
        onResume?.Invoke();
    }

    private void HandleOptionsButtonClicked(Panel other)
    {
        this.IsVisible = false;
        optionPanel.IsVisible = true;
    }

    private void HandleRestartLevelButtonClicked(Action onRestartLevel)
    {
        this.IsVisible = false;
        optionPanel.IsVisible = false;
        onRestartLevel?.Invoke();
    }

    private void HandleReturnToLevelSelectButtonClicked()
    {
        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene("LevelSelect");
    }

    private void HandleReturnToMainMenuButtonClicked()
    {
        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene("main");
    }

    public void TogglePause(bool isPaused)
    {
        if (optionPanel.IsVisible)
        {
            optionPanel.IsVisible = false;
        }
        this.IsVisible = isPaused;
    }
}