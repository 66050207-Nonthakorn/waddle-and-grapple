using Microsoft.Xna.Framework;
using ComputerGameFinal.Engine;
using ComputerGameFinal.Engine.Managers;
using System;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using ComputerGameFinal.Engine.Components;

namespace ComputerGameFinal.Game.Scenes;

public class MainMenu : Scene
{
    private Panel _buttonPanel;

    public override void Setup()
    {
        GumService.Default.Root.Children.Clear(); // Clear any existing Gum UI elements

        float screenWidth = ScreenManager.Instance.nativeWidth;
        float screenHeight = ScreenManager.Instance.nativeHeight;

        // Background
        var background = base.AddGameObject<GameObject>("bg");
        var bgSprite = background.AddComponent<SpriteRenderer>();
        bgSprite.Texture = ResourceManager.Instance.GetTexture("UI/MainScreen");
        background.Position = new Vector2(screenWidth / 2f, screenHeight / 2f);
        if (bgSprite.Texture != null)
        {
            background.Scale = new Vector2(
                screenWidth / bgSprite.Texture.Width,
                screenHeight / bgSprite.Texture.Height
            );
        }

        CreateButtonsPanel();
    }

    private void CreateButtonsPanel()
    {
        // check the size for Gum
        Console.WriteLine($"Creating button panel with screen size: {GumService.Default.CanvasWidth}x{GumService.Default.CanvasHeight}");
        _buttonPanel = new Panel();
        _buttonPanel.Dock(Gum.Wireframe.Dock.Fill);
        _buttonPanel.AddToRoot();

        var startButton = new Button();
        startButton.Anchor(Gum.Wireframe.Anchor.Center);
        startButton.Y = 80;
        startButton.Width = 240;
        startButton.Height = 30;
        startButton.Text = "Start Game";
        startButton.Click += OnStartGameClick;
        _buttonPanel.AddChild(startButton);

        var settingsButton = new Button();
        settingsButton.Anchor(Gum.Wireframe.Anchor.Center);
        settingsButton.Y = 150;
        settingsButton.Width = 240;
        settingsButton.Height = 30;
        settingsButton.Text = "Settings";
        settingsButton.Click += OnSettingsClick;
        _buttonPanel.AddChild(settingsButton);

        var quitButton = new Button();
        quitButton.Anchor(Gum.Wireframe.Anchor.Center);
        quitButton.Y = 220;
        quitButton.Width = 240;
        quitButton.Height = 30;
        quitButton.Text = "Quit Game";
        quitButton.Click += OnQuitGameClick;
        _buttonPanel.AddChild(quitButton);
    }

    private void OnStartGameClick(object sender, EventArgs e)
    {
        // Create a fresh GameScene instance each time
        GumService.Default.Root.Children.Clear(); // Clear Gum UI elements from the main menu
        SceneManager.Instance.LoadScene("GameScene");
    }

    private void OnSettingsClick(object sender, EventArgs e)
    {
        // // Open settings as an overlay
        // AudioManager.Instance.PlaySound("Button_Click");
        // SceneManager.Instance.PushOverlay(new SettingsScene());
        Console.WriteLine("Settings button clicked - functionality not implemented yet.");
    }

    private void OnQuitGameClick(object sender, EventArgs e)
    {
        // // Exit the game
        // AudioManager.Instance.PlaySound("Button_Click");
        System.Environment.Exit(0);
    }
}
