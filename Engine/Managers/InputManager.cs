using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace WaddleAndGrapple.Engine.Managers;

public class InputManager
{
    public static InputManager Instance { get; private set; } = new InputManager();

    private KeyboardState _currentKeyState;
    private KeyboardState _previousKeyState;

    private MouseState _currentMouseState;
    private MouseState _previousMouseState;

    private InputManager()
    {
        _currentKeyState = Keyboard.GetState();
        _previousKeyState = _currentKeyState;

        _currentMouseState = Mouse.GetState();
        _previousMouseState = _currentMouseState;
    }

    public bool IsKeyDown(Keys key)
    {
        return _currentKeyState.IsKeyDown(key);
    }
    
    public bool IsKeyPressed(Keys key)
    {
        return _currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key);
    }

    public bool IsKeyReleased(Keys key)
    {
        return _currentKeyState.IsKeyUp(key) && _previousKeyState.IsKeyDown(key);
    }
    
    /// <summary>true ถ้าเคอร์เซอร์อยู่ภายใน RenderDestination (viewport จริงของเกม)</summary>
    public bool IsMouseInViewport()
    {
        var vp = ScreenManager.Instance.RenderDestination;
        if (vp == Microsoft.Xna.Framework.Rectangle.Empty) return true; // fallback
        return _currentMouseState.X >= vp.Left && _currentMouseState.X <= vp.Right
            && _currentMouseState.Y >= vp.Top  && _currentMouseState.Y <= vp.Bottom;
    }

    public bool IsMouseButtonDown(int button)
    {
        if (!IsMouseInViewport()) return false;
        return button switch
        {
            0 => _currentMouseState.LeftButton == ButtonState.Pressed,
            1 => _currentMouseState.RightButton == ButtonState.Pressed,
            2 => _currentMouseState.MiddleButton == ButtonState.Pressed,
            _ => false
        };
    }

    public bool IsMouseButtonReleased(int button)
    {
        if (!IsMouseInViewport()) return false;
        return button switch
        {
            0 => _currentMouseState.LeftButton == ButtonState.Released && _previousMouseState.LeftButton == ButtonState.Pressed,
            1 => _currentMouseState.RightButton == ButtonState.Released && _previousMouseState.RightButton == ButtonState.Pressed,
            2 => _currentMouseState.MiddleButton == ButtonState.Released && _previousMouseState.MiddleButton == ButtonState.Pressed,
            _ => false
        };
    }

    public bool IsMouseButtonPressed(int button)
    {
        if (!IsMouseInViewport()) return false;
        return button switch
        {
            0 => _currentMouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released,
            1 => _currentMouseState.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released,
            2 => _currentMouseState.MiddleButton == ButtonState.Pressed && _previousMouseState.MiddleButton == ButtonState.Released,
            _ => false
        };
    }

    public Vector2 GetMousePosition()
    {
        return ScreenManager.Instance.WindowToNativePoint(_currentMouseState.X, _currentMouseState.Y);
    }
    
    public void Update()
    {
        _previousKeyState = _currentKeyState;
        _currentKeyState = Keyboard.GetState();

        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();
    }
}