using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.Managers;

public class ScreenManager
{
    public static ScreenManager Instance { get; private set; } = new ScreenManager();

    public int nativeWidth;
    public int nativeHeight;
    public int previousWidth = 0;
    public int previousHeight = 0;

    public bool IsFullScreen { get; private set; } = true;

    public GraphicsDeviceManager _graphics { get; set; }
    public Rectangle RenderDestination { get; private set; } = Rectangle.Empty;

    private ScreenManager() { }

    public void SetRenderDestination(Rectangle renderDestination)
    {
        RenderDestination = renderDestination;
    }

    public Vector2 WindowToNativePoint(int windowX, int windowY)
    {
        if (RenderDestination.Width <= 0 || RenderDestination.Height <= 0 || nativeWidth <= 0 || nativeHeight <= 0)
        {
            return new Vector2(windowX, windowY);
        }

        float normalizedX = (windowX - RenderDestination.X) / (float)RenderDestination.Width;
        float normalizedY = (windowY - RenderDestination.Y) / (float)RenderDestination.Height;

        float nativeX = MathHelper.Clamp(normalizedX, 0f, 1f) * nativeWidth;
        float nativeY = MathHelper.Clamp(normalizedY, 0f, 1f) * nativeHeight;

        return new Vector2(nativeX, nativeY);
    }

    public void ToggleFullscreen()
    {
        IsFullScreen = !IsFullScreen;
        ApplyCurrentMode();
    }

    public void ApplyCurrentMode()
    {
        if (IsFullScreen)
            SetFullsceen();
        else
            UnsetFullscreen();
    }

    private void SetFullsceen()
    {
        _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

        _graphics.IsFullScreen = true;
        _graphics.HardwareModeSwitch = false;
        _graphics.ApplyChanges();
    }

    private void UnsetFullscreen()
    {
        _graphics.PreferredBackBufferWidth = previousWidth;
        _graphics.PreferredBackBufferHeight = previousHeight;

        _graphics.IsFullScreen = false;
        _graphics.HardwareModeSwitch = true;
        _graphics.ApplyChanges();
    }
}