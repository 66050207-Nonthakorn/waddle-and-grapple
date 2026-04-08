using System;
using WaddleAndGrapple.Engine;
using WaddleAndGrapple.Engine.Managers;
using WaddleAndGrapple.Engine.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Engine.UI;

public class LevelPortrait : GameObject
{
    public Text LevelName { get; private set; }
    public Text BestTimeStats { get; private set; }
    public Text CollectedFishStats { get; private set; }
    public ClickableSprite PortraitSprite { get; private set; }

    public bool IsLocked { get; private set; }
    public Action OnClick
    {
        get => PortraitSprite.OnClick;
        set => PortraitSprite.OnClick = value;
    }

    private readonly Texture2D _lockedTexture;
    private readonly Texture2D _levelTexture;
    private readonly SpriteFont _portraitFont;

    private const float TopPaddingRatio = 0.005f;
    private const float BottomPaddingRatio = 0.005f;
    private const float StatsGapRatio = 0.001f;

    public LevelPortrait(string levelName, Texture2D texture)
        : this(levelName, texture, texture, false)
    {
    }

    public LevelPortrait(string levelName, Texture2D lockedTexture, Texture2D levelTexture, bool isLocked = true)
    {
        _lockedTexture = lockedTexture;
        _levelTexture = levelTexture;
        _portraitFont = ResourceManager.Instance.GetFont("Fonts/File");

        PortraitSprite = AddComponent<ClickableSprite>();
        PortraitSprite.UseTextureSizeForHitbox = true;

        LevelName = AddComponent<Text>();
        LevelName.Content = levelName;
        LevelName.Font = _portraitFont;
        LevelName.Color = Color.White;

        BestTimeStats = AddComponent<Text>();
        BestTimeStats.Font = _portraitFont;
        BestTimeStats.Color = Color.White;

        CollectedFishStats = AddComponent<Text>();
        CollectedFishStats.Font = _portraitFont;
        CollectedFishStats.Color = Color.White;

        SetStats(null, null, null);
        SetLocked(isLocked);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    public void SetLocked(bool isLocked)
    {
        IsLocked = isLocked;
        PortraitSprite.Texture = IsLocked ? _lockedTexture : _levelTexture;

        if (PortraitSprite.Texture != null)
        {
            PortraitSprite.Size = new Vector2(PortraitSprite.Texture.Width, PortraitSprite.Texture.Height);
        }

        if (IsLocked)
        {
            SetStats(null, null, null);
        }

        UpdateTextLayout();
    }

    public void SetStats(TimeSpan? bestTime, int? collectedCount, int? totalCollectibles)
    {
        string bestTimeText = bestTime.HasValue
            ? $"{bestTime.Value.Minutes:00}:{bestTime.Value.Seconds:00}.{bestTime.Value.Milliseconds / 10:00}"
            : "Not Completed";

        string collectedText = collectedCount.HasValue && totalCollectibles.HasValue
            ? $"{collectedCount.Value}/{totalCollectibles.Value}"
            : "??/??";

        BestTimeStats.Content = bestTimeText;
        CollectedFishStats.Content = collectedText;

        UpdateTextLayout();
    }

    private void UpdateTextLayout()
    {
        if (PortraitSprite.Texture == null)
        {
            return;
        }

        float portraitWidth = PortraitSprite.Texture.Width;
        float portraitHeight = PortraitSprite.Texture.Height;

        Vector2 levelSize = LevelName.MeasureText();
        Vector2 timeSize = BestTimeStats.MeasureText();
        Vector2 collectibleSize = CollectedFishStats.MeasureText();

        float topPadding = portraitHeight * TopPaddingRatio;
        float bottomPadding = portraitHeight * BottomPaddingRatio;
        float statsGap = portraitHeight * StatsGapRatio;

        // Top label
        LevelName.Origin = levelSize / 2f;
        float levelNameLocalY = topPadding + (levelSize.Y * 0.5f);
        LevelName.Offset = new Vector2(portraitWidth * 0.5f, -levelNameLocalY);

        // Bottom stacked stats
        BestTimeStats.Origin = timeSize / 2f;
        float bestTimeLocalY = portraitHeight + bottomPadding + (timeSize.Y * 0.5f);
        BestTimeStats.Offset = new Vector2(portraitWidth * 0.5f, bestTimeLocalY);

        CollectedFishStats.Origin = collectibleSize / 2f;
        float collectiblesLocalY = bestTimeLocalY + (timeSize.Y * 0.5f) + statsGap + (collectibleSize.Y * 0.5f);
        CollectedFishStats.Offset = new Vector2(portraitWidth * 0.5f, collectiblesLocalY);
    }
}