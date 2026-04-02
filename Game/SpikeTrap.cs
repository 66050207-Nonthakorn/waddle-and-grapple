using System;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;

namespace WaddleAndGrapple.Game;

public enum SpikeOrigin { Floor, Ceiling, LeftWall, RightWall }

/// <summary>
/// หนามที่ผุดจากพื้น/เพดาน/กำแพง แล้วหุบกลับ
/// Visual: block สีเขียว ขยาย-หดตาม progress
/// Collision: active ทุก state ยกเว้น Paused
/// </summary>
public class SpikeTrap : Trap
{
    public SpikeOrigin Origin          { get; set; } = SpikeOrigin.Floor;
    public float       SpikeLength     { get; set; } = 45f;
    public float       ExtendDuration  { get; set; } = 0.2f;
    public float       HoldDuration    { get; set; } = 1.0f;
    public float       RetractDuration { get; set; } = 0.2f;
    public float       PauseDuration   { get; set; } = 1.5f;
    // ชดเชย phase ให้หนามหลายตัวไม่ผุดพร้อมกัน
    public float       PhaseOffset     { get; set; } = 0f;

    private const int SpikeThickness = 20;
    private static readonly Color SpikeColor = new Color(50, 220, 80); // lime green

    private enum SpikeState { Paused, Extending, Extended, Retracting }
    private SpikeState _spikeState = SpikeState.Paused;
    private float      _stateTimer;
    private Vector2    _basePosition;
    private bool       _usingPixelTexture = true;

    protected override void OnInitialize()
    {
        Damage        = 1;
        _basePosition = Position;
        _stateTimer   = PhaseOffset;

        SpriteTextureName = SpriteTextureName ?? "pixel";
        var texture = ResourceManager.Instance.GetTexture(SpriteTextureName);
        _usingPixelTexture = texture == ResourceManager.Instance.GetTexture("pixel");
        if (_usingPixelTexture)
            SpriteTint = SpikeColor;

        ApplySpriteTexture();

        UpdateVisual(0f);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = WorldTime.Dt((float)gameTime.ElapsedGameTime.TotalSeconds);
        _stateTimer += dt;

        switch (_spikeState)
        {
            case SpikeState.Paused:
                UpdateVisual(0f);
                if (_stateTimer >= PauseDuration) { _spikeState = SpikeState.Extending; _stateTimer = 0f; }
                break;

            case SpikeState.Extending:
                UpdateVisual(Math.Min(_stateTimer / ExtendDuration, 1f));
                if (_stateTimer >= ExtendDuration) { _spikeState = SpikeState.Extended; _stateTimer = 0f; }
                break;

            case SpikeState.Extended:
                UpdateVisual(1f);
                if (_stateTimer >= HoldDuration) { _spikeState = SpikeState.Retracting; _stateTimer = 0f; }
                break;

            case SpikeState.Retracting:
                UpdateVisual(Math.Max(1f - _stateTimer / RetractDuration, 0f));
                if (_stateTimer >= RetractDuration) { _spikeState = SpikeState.Paused; _stateTimer = 0f; }
                break;
        }
    }

    // SpikeLength  = ความกว้างของ block ขนานกับพื้นผิว (คงที่)
    // SpikeThickness = ระยะที่ block โผล่ออกจากพื้นผิว (max extension)
    private void UpdateVisual(float progress)
    {
        float ext = SpikeThickness * progress; // ระยะที่โผล่ออกมาตาม progress
        Vector2 drawPos;
        Vector2 drawSize;

        switch (Origin)
        {
            case SpikeOrigin.Floor:
                drawPos = new Vector2(_basePosition.X, _basePosition.Y - ext);
                drawSize = new Vector2(SpikeLength, ext);
                break;
            case SpikeOrigin.Ceiling:
                drawPos = new Vector2(_basePosition.X, _basePosition.Y);
                drawSize = new Vector2(SpikeLength, ext);
                break;
            case SpikeOrigin.LeftWall:
                drawPos = new Vector2(_basePosition.X, _basePosition.Y);
                drawSize = new Vector2(ext, SpikeLength);
                break;
            case SpikeOrigin.RightWall:
                drawPos = new Vector2(_basePosition.X - ext, _basePosition.Y);
                drawSize = new Vector2(ext, SpikeLength);
                break;
            default:
                drawPos = _basePosition;
                drawSize = Vector2.Zero;
                break;
        }

        if (_usingPixelTexture)
        {
            Rotation = Vector3.Zero;
            Position = drawPos;
            SetSpikeScale(drawSize);
            return;
        }

        // Keep unrotated size as horizontal strip, then rotate for wall-mounted spikes.
        SetSpikeScale(new Vector2(SpikeLength, ext));

        // For sprite textures, draw from center and rotate so the same sprite works on all surfaces.
        _spriteRenderer.Origin = new Vector2(_spriteRenderer.Texture.Bounds.Width * 0.5f,
                                             _spriteRenderer.Texture.Bounds.Height * 0.5f);
        Position = drawPos + (drawSize * 0.5f);

        Rotation = Origin switch
        {
            SpikeOrigin.Floor     => new Vector3(0f, 0f, 0f),
            SpikeOrigin.Ceiling   => new Vector3(0f, 0f, MathF.PI),
            SpikeOrigin.LeftWall  => new Vector3(0f, 0f, MathF.PI / 2f),
            SpikeOrigin.RightWall => new Vector3(0f, 0f, -MathF.PI / 2f),
            _                     => Vector3.Zero
        };
    }

    private void SetSpikeScale(Vector2 targetSize)
    {
        if (_spriteRenderer?.Texture == null)
        {
            Scale = targetSize;
            return;
        }

        var pixelTexture = ResourceManager.Instance.GetTexture("pixel");
        if (_spriteRenderer.Texture == pixelTexture)
        {
            Scale = targetSize;
            return;
        }

        Scale = new Vector2(targetSize.X / _spriteRenderer.Texture.Bounds.Width,
                            targetSize.Y / _spriteRenderer.Texture.Bounds.Height);
    }

    // Fixed hitbox matching the fully-extended block dimensions
    protected override Rectangle GetCollisionBounds()
    {
        int cx   = (int)_basePosition.X;
        int cy   = (int)_basePosition.Y;
        int ext  = SpikeThickness;
        int span = (int)SpikeLength;
        return Origin switch
        {
            SpikeOrigin.Floor     => new Rectangle(cx,       cy - ext, span, ext),
            SpikeOrigin.Ceiling   => new Rectangle(cx,       cy,       span, ext),
            SpikeOrigin.LeftWall  => new Rectangle(cx,       cy,       ext,  span),
            SpikeOrigin.RightWall => new Rectangle(cx - ext, cy,       ext,  span),
            _                     => base.GetCollisionBounds(),
        };
    }

    protected override void OnPlayerEnter(Player player)
    {
        if (_spikeState == SpikeState.Paused) return;
        player.Die();
    }
}
