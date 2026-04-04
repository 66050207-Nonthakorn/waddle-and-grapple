using System;
using ComputerGameFinal.Engine.Managers;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework;
using MonoGameGum;
using MonoGameGum.GueDeriving;

public class OptionPanel : Panel
{
    private Panel previousPanel;

    public OptionPanel(Panel previousPanel)
    {
        this.previousPanel = previousPanel;
        this.Dock(Gum.Wireframe.Dock.Fill);
        this.IsVisible = false; // Start hidden

        var blackOverlay = new ColoredRectangleRuntime();
        blackOverlay.Dock(Gum.Wireframe.Dock.Fill);
        blackOverlay.Color = new Color(0, 0, 0, 150); // Semi-transparent black
        this.AddChild(blackOverlay);


        var optPanel = new Panel();
        optPanel.Anchor(Gum.Wireframe.Anchor.Center);
        optPanel.Width = 20;
        optPanel.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        optPanel.Height = 100;
        optPanel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        this.AddChild(optPanel);

        var background = new ColoredRectangleRuntime();
        optPanel.AddChild(background);
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Color = Color.DarkBlue;

        var settingStackPannel = new StackPanel();
        settingStackPannel.Dock(Gum.Wireframe.Dock.Fill);
        settingStackPannel.Width = 300;
        settingStackPannel.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        settingStackPannel.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToChildren;
        settingStackPannel.Height = 30;
        settingStackPannel.Spacing = 8;
        optPanel.AddChild(settingStackPannel);

        var optionsText = new TextRuntime();
        optionsText.Text = "OPTIONS";
        optionsText.Anchor(Gum.Wireframe.Anchor.TopLeft);
        optionsText.X = 10;
        optionsText.Y = 10;
        optPanel.AddChild(optionsText);

        var musicLabel = new Label();
        
        settingStackPannel.AddChild(musicLabel);

        var musicSlider = new Slider();
        musicSlider.Anchor(Gum.Wireframe.Anchor.Top);
        musicSlider.Width = 0;
        musicSlider.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        musicSlider.Minimum = 0;
        musicSlider.Maximum = 1;
        musicSlider.Value = AudioManager.SongVolume;
        musicSlider.SmallChange = .1;
        musicSlider.LargeChange = .2;
        musicSlider.ValueChanged += (_, _) => HandleMusicSliderValueChanged(musicLabel, (float)musicSlider.Value);
        musicSlider.ValueChangeCompleted += HandleMusicSliderValueChangeCompleted;
        settingStackPannel.AddChild(musicSlider);

        musicLabel.Text = $"Music: {(int)(musicSlider.Value * 100)}%";

        var sfxLabel = new Label();
        settingStackPannel.AddChild(sfxLabel);

        var sfxSlider = new Slider();
        sfxSlider.Anchor(Gum.Wireframe.Anchor.Top);
        sfxSlider.Width = 0;
        sfxSlider.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent;
        sfxSlider.Minimum = 0;
        sfxSlider.Maximum = 1;
        sfxSlider.Value = AudioManager.SFXVolume;
        sfxSlider.SmallChange = .1;
        sfxSlider.LargeChange = .2;
        sfxSlider.ValueChanged += (_,_) => HandleSfxSliderChanged(sfxLabel, (float)sfxSlider.Value);
        sfxSlider.ValueChangeCompleted += HandleSfxSliderChangeCompleted;
        settingStackPannel.AddChild(sfxSlider);

        sfxLabel.Text = $"SFX: {(int)(sfxSlider.Value * 100)}%";

        var fullscreenCheckbox = new CheckBox();
        fullscreenCheckbox.Text = "Fullscreen";
        fullscreenCheckbox.IsChecked = ScreenManager.Instance.isFullScreen;
        fullscreenCheckbox.Checked += (_, _) => ScreenManager.Instance.ToggleFullscreen();
        fullscreenCheckbox.Unchecked += (_, _) => ScreenManager.Instance.ToggleFullscreen();
        settingStackPannel.AddChild(fullscreenCheckbox);


        Button optionsBackButton = new Button();
        optionsBackButton.Text = "BACK";
        optionsBackButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
        optionsBackButton.X = -20;
        optionsBackButton.Y = -20;
        optionsBackButton.Click += (_, _) => HandleOptionsButtonBack(previousPanel);
        optPanel.AddChild(optionsBackButton);
    }

    private void HandleMusicSliderValueChanged(Label musicLabel, float value)
    {
        AudioManager.SongVolume = value;
        musicLabel.Text = $"Music: {(int)(value * 100)}%";
    }

    private void HandleMusicSliderValueChangeCompleted(object sender, EventArgs e)
    {
        // play a sound to indicate the change
    }

    private void HandleSfxSliderChanged(Label sfxLabel, float value)
    {
        
        AudioManager.SFXVolume = value;
        sfxLabel.Text = $"SFX: {(int)(value * 100)}%";
    }

    private void HandleSfxSliderChangeCompleted(object sender, EventArgs e)
    {
        // play a sound to indicate the change
    }

    private void HandleOptionsButtonBack(Panel other)
    {
        this.IsVisible = false;
        other.IsVisible = true;
    }
}