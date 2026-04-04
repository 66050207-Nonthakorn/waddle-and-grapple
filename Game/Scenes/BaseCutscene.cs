using System;
using System.Collections.Generic;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum;

namespace WaddleAndGrapple.Game.Scenes;

public abstract class BaseCutscene : Scene
{
    private const float TypingCharsPerSecond = 32f;
    private const float ContinueBlinkInterval = 0.6f;

    private readonly List<string> _sections = [];

    private int _currentSectionIndex;
    private int _visibleCharacters;
    private float _typingAccumulator;
    private float _blinkTimer;

    private readonly List<Text> _dialogueLineTexts = [];
    private SpriteFont _dialogueFont;
    private SpriteRenderer _continueIconSprite;

    private string _currentWrappedFullText = string.Empty;
    private int _cachedWrappedSectionIndex = -1;

    private float centerX;
    private const int MaxDialogueLines = 8;
    private const float DialogueFirstLineY = 320f;
    private const float DialogueLineSpacing = 34f;

    public override void Setup()
    {
        GumService.Default.Root.Children.Clear();

        CreateBackground();
        CreateBossPortrait();
        CreateDialogueTextLines();
        CreateContinueIcon();

        _sections.Clear();
        _sections.AddRange(GetSections());

        _currentSectionIndex = 0;
        _visibleCharacters = 0;
        _typingAccumulator = 0f;
        _blinkTimer = 0f;
        _cachedWrappedSectionIndex = -1;

        if (_sections.Count == 0)
        {
            CompleteCutscene();
            return;
        }

        UpdateDialogueText();
        SetContinueIconVisible(false);
    }

    public override void Update(GameTime gameTime)
    {
        if (InputManager.Instance.IsKeyPressed(Keys.Escape))
        {
            CompleteCutscene();
            return;
        }
        
        bool nextPressed = InputManager.Instance.IsMouseButtonPressed(0)
            || InputManager.Instance.IsKeyPressed(Keys.Space)
            || InputManager.Instance.IsKeyPressed(Keys.Enter);

        if (nextPressed)
        {
            HandleAdvanceInput();
        }

        UpdateTyping(gameTime);
        UpdateContinueBlink(gameTime);

        base.Update(gameTime);
    }

    protected abstract IReadOnlyList<string> GetSections();
    protected abstract string GetBossPortraitTextureName();
    protected abstract string GetNextSceneName();

    protected virtual float GetDialogueMaxWidth() => 820f;

    protected virtual void OnCutsceneCompleted() { }

    private void CreateBackground()
    {
        float screenWidth = ScreenManager.Instance.nativeWidth;
        float screenHeight = ScreenManager.Instance.nativeHeight;

        var background = AddGameObject<GameObject>("CutsceneBackground");
        var bgSprite = background.AddComponent<SpriteRenderer>();
        bgSprite.Texture = ResourceManager.Instance.GetTexture("UI/TabletScene");
        background.Position = new Vector2(screenWidth / 2f, screenHeight / 2f);
        if (bgSprite.Texture != null)
        {
            background.Scale = new Vector2(
                screenWidth / bgSprite.Texture.Width,
                screenHeight / bgSprite.Texture.Height);
        }
    }

    private void CreateBossPortrait()
    {
        var bossPortrait = AddGameObject<GameObject>("CutsceneBossPortrait");
        var sprite = bossPortrait.AddComponent<SpriteRenderer>();
        sprite.Texture = ResourceManager.Instance.GetTexture(GetBossPortraitTextureName());
        sprite.LayerDepth = 0.7f;

        centerX = 420f + (sprite.Texture?.Width ?? 0f) / 2f;
        float topY = 92f + (sprite.Texture?.Height ?? 0f) / 2f;
        bossPortrait.Position = new Vector2(centerX, topY);
    }

    private void CreateDialogueTextLines()
    {
        _dialogueFont = ResourceManager.Instance.GetFont("Fonts/File");
        _dialogueLineTexts.Clear();

        for (int i = 0; i < MaxDialogueLines; i++)
        {
            var lineObject = AddGameObject<GameObject>($"CutsceneDialogueLine_{i}");
            var lineText = lineObject.AddComponent<Text>();
            lineText.Font = _dialogueFont;
            lineText.Color = Color.White;
            lineText.Offset = new Vector2(centerX, DialogueFirstLineY + (i * DialogueLineSpacing));
            lineText.LayerDepth = 0.95f;
            _dialogueLineTexts.Add(lineText);
        }
    }

    private void CreateContinueIcon()
    {
        var iconObject = AddGameObject<GameObject>("CutsceneContinueIcon");
        iconObject.Position = new Vector2(810f, 400f);

        _continueIconSprite = iconObject.AddComponent<SpriteRenderer>();
        _continueIconSprite.Texture = ResourceManager.Instance.GetTexture("UI/Continue");
        _continueIconSprite.LayerDepth = 0.98f;
    }

    private void HandleAdvanceInput()
    {
        string wrappedSection = GetWrappedCurrentSection();

        if (_visibleCharacters < wrappedSection.Length)
        {
            _visibleCharacters = wrappedSection.Length;
            UpdateDialogueText();
            SetContinueIconVisible(true);
            return;
        }

        if (_currentSectionIndex >= _sections.Count - 1)
        {
            CompleteCutscene();
            return;
        }

        _currentSectionIndex++;
        _visibleCharacters = 0;
        _typingAccumulator = 0f;
        _blinkTimer = 0f;
        SetContinueIconVisible(false);
        UpdateDialogueText();
    }

    private void UpdateTyping(GameTime gameTime)
    {
        string wrappedSection = GetWrappedCurrentSection();
        if (_visibleCharacters >= wrappedSection.Length)
        {
            return;
        }

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _typingAccumulator += TypingCharsPerSecond * dt;

        int charsToAdd = (int)_typingAccumulator;
        if (charsToAdd <= 0)
        {
            return;
        }

        _typingAccumulator -= charsToAdd;
        _visibleCharacters = Math.Min(_visibleCharacters + charsToAdd, wrappedSection.Length);

        UpdateDialogueText();

        if (_visibleCharacters >= wrappedSection.Length)
        {
            SetContinueIconVisible(true);
        }
    }

    private void UpdateContinueBlink(GameTime gameTime)
    {
        if (!IsCurrentSectionFullyVisible())
        {
            SetContinueIconVisible(false);
            return;
        }

        _blinkTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_blinkTimer >= ContinueBlinkInterval)
        {
            _blinkTimer = 0f;
            bool currentlyVisible = _continueIconSprite.Enabled;
            SetContinueIconVisible(!currentlyVisible);
        }
    }

    private bool IsCurrentSectionFullyVisible()
    {
        return _visibleCharacters >= GetWrappedCurrentSection().Length;
    }

    private void UpdateDialogueText()
    {
        string wrappedSection = GetWrappedCurrentSection();
        int visibleCount = Math.Min(_visibleCharacters, wrappedSection.Length);
        string visibleText = wrappedSection[..visibleCount];
        string[] lines = visibleText.Split('\n');

        for (int i = 0; i < _dialogueLineTexts.Count; i++)
        {
            string line = i < lines.Length ? lines[i] : string.Empty;
            _dialogueLineTexts[i].Content = line;
            _dialogueLineTexts[i].Origin = _dialogueLineTexts[i].MeasureText() / 2f;
        }
    }

    private string GetWrappedCurrentSection()
    {
        string section = _sections[_currentSectionIndex] ?? string.Empty;
        if (_cachedWrappedSectionIndex != _currentSectionIndex)
        {
            _currentWrappedFullText = WrapText(_dialogueFont, section, GetDialogueMaxWidth());
            _cachedWrappedSectionIndex = _currentSectionIndex;
        }

        return _currentWrappedFullText;
    }

    private static string WrapText(SpriteFont font, string text, float maxLineWidth)
    {
        if (font == null || string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        string[] words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        string wrapped = words[0];
        for (int i = 1; i < words.Length; i++)
        {
            string testLine = wrapped[(wrapped.LastIndexOf('\n') + 1)..] + " " + words[i];
            if (font.MeasureString(testLine).X <= maxLineWidth)
            {
                wrapped += " " + words[i];
            }
            else
            {
                wrapped += "\n" + words[i];
            }
        }

        return wrapped;
    }

    private void SetContinueIconVisible(bool visible)
    {
        if (_continueIconSprite != null)
        {
            _continueIconSprite.Enabled = visible && _continueIconSprite.Texture != null;
        }

    }

    private void CompleteCutscene()
    {
        OnCutsceneCompleted();
        GumService.Default.Root.Children.Clear();
        SceneManager.Instance.LoadScene(GetNextSceneName());
    }
}
