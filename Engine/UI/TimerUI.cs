using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;

namespace ComputerGameFinal.Engine.UI;

public class TimerUI : GameObject
{
    private Text _text;
    private float _elapsedTime;
    public bool IsRunning { get; private set; } = false;

    public TimerUI()
    {
        _elapsedTime = 0f;
    }

    public override void Initialize()
    {
        base.Initialize();
        
        _text = AddComponent<Text>();
        _text.Font = ResourceManager.Instance.GetFont("Fonts/36Font");
        _text.Content = "00:00.00";
        _text.Offset = new Vector2(ScreenManager.Instance.nativeWidth - 20, 20);
        _text.Color = Color.White;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        
        // Update elapsed time
        if (IsRunning)
        {
            _elapsedTime += (float)gameTime.ElapsedGameTime.TotalMilliseconds;
        }
        
        // Update text display
        if (_text != null)
        {
            _text.Content = GetFormattedTime();

            // draw the text from the right edge
            var textSize = _text.MeasureText();
            _text.Origin = new Vector2(textSize.X,  0);
        }
    }

    public float GetElapsedTime() => _elapsedTime;

    public void SetElapsedTime(float elapsedMilliseconds)
    {
        _elapsedTime = Math.Max(0f, elapsedMilliseconds);
    }
    
    public string GetFormattedTime()
    {
        int minutes = (int)(_elapsedTime / 60000);
        int seconds = (int)(_elapsedTime % 60000) / 1000;
        int milliseconds = (int)(_elapsedTime % 1000);
        return $"{minutes:D2}:{seconds:D2}:{milliseconds:D3}";
    }
    
    public void ResetTimer()
    {
        _elapsedTime = 0f;
        IsRunning = false;
    }

    public void StartTimer()
    {
        IsRunning = true;
    }
}
