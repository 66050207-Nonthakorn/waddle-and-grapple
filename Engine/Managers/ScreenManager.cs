using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Engine.Managers;

public class ScreenManager
{
    public static ScreenManager Instance { get; private set; } = new ScreenManager();

    public int nativeWidth;
    public int nativeHeight;
    public int previousWidth = 0;
    public int previousHeight = 0;

    private bool _isFullScreen = false;
    private bool _isBorderless = false;

    public GraphicsDeviceManager _graphics { get; set; }

    private ScreenManager() { }

    public void ToggleFullscreen()
    {
        bool prevFullscreen = _isFullScreen;

        if (_isBorderless)
        {
            _isBorderless = false;
        }
        else
        {
            _isFullScreen = !_isFullScreen;
        }
        
        ApplyFullscreen(prevFullscreen);
    }

    public void ToggleBorderless()
    {
        bool prevFullscreen = _isFullScreen;

        _isBorderless = !_isBorderless;
        _isFullScreen = _isBorderless;

        ApplyFullscreen(prevFullscreen);
    }

    private void ApplyFullscreen(bool prevFullscreen)
    {
        if (_isFullScreen)
        {
            if (prevFullscreen)
            {
                ApplyHardwareMode();
            }
            else
            {
                SetFullsceen();
            }
        }
        else
        {
            UnsetFullscreen();
        }
    }

    private void ApplyHardwareMode()
    {
        _graphics.HardwareModeSwitch = !_isBorderless;
        _graphics.ApplyChanges();
    }

    private void SetFullsceen()
    {
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.HardwareModeSwitch = !_isBorderless;

        _graphics.IsFullScreen = true;
        _graphics.ApplyChanges();
    }

    private void UnsetFullscreen()
    {
        _graphics.PreferredBackBufferWidth = previousWidth;
        _graphics.PreferredBackBufferHeight = previousHeight;

        _graphics.IsFullScreen = false;
        _graphics.ApplyChanges();
    }
}