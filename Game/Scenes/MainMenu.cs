using Microsoft.Xna.Framework;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Managers;
using System;
using MonoGameGum;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using WaddleAndGrapple.Engine.Components;
using Gum.Managers;

namespace WaddleAndGrapple.Game.Scenes;

public class MainMenu : Scene
{
    private Panel _buttonPanel;
    private Panel _optionsPanel;

    public override void Setup()
    {
        GumService.Default.Root.Children.Clear(); // Clear any existing Gum UI elements

        if (AudioManager.Instance.CurrentSongName != "Song/MainMenu")
        {
            AudioManager.Instance.PlaySong("Song/MainMenu");
        }

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
        _optionsPanel = new OptionPanel(_buttonPanel);
        _optionsPanel.AddToRoot();
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
        startButton.Text = "START GAME";
        startButton.Click += OnStartGameClick;
        _buttonPanel.AddChild(startButton);

       Button optionButton = new Button();
       optionButton.Anchor(Gum.Wireframe.Anchor.Center);
       optionButton.Y = 150;
       optionButton.Width = 240;
       optionButton.Height = 30;
       optionButton.Text = "SETTINGS";
       optionButton.Click += OnSettingsClick;
        _buttonPanel.AddChild(optionButton);

        var quitButton = new Button();
        quitButton.Anchor(Gum.Wireframe.Anchor.Center);
        quitButton.Y = 220;
        quitButton.Width = 240;
        quitButton.Height = 30;
        quitButton.Text = "QUIT GAME";
        quitButton.Click += OnQuitGameClick;
        _buttonPanel.AddChild(quitButton);
    }

    public void OnStartGameClick(object sender, EventArgs e)
    {
        // Create a fresh LevelSelect instance each time
        GumService.Default.Root.Children.Clear(); // Clear Gum UI elements from the main menu
        SceneManager.Instance.AddScene<LevelSelect>("LevelSelect");
        SceneManager.Instance.LoadScene("LevelSelect");
    }

    public void OnSettingsClick(object sender, EventArgs e)
    {
        // // Open settings as an overlay
        _buttonPanel.IsVisible = false;
        _optionsPanel.IsVisible = true;
    }

    public void OnQuitGameClick(object sender, EventArgs e)
    {
        // // Exit the game
        // AudioManager.Instance.PlaySound("Button_Click");
        System.Environment.Exit(0);
    }
}
