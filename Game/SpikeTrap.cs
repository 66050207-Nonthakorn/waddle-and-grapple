using System;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;

namespace ComputerGameFinal.Game;

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

    protected override void OnInitialize()
    {
        Damage        = 1;
        _basePosition = Position;
        _stateTimer   = PhaseOffset;

        _spriteRenderer.Texture    = ResourceManager.Instance.GetTexture("pixel");
        _spriteRenderer.Tint       = SpikeColor;
        _spriteRenderer.LayerDepth = 0.6f;

        UpdateVisual(0f);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
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
        switch (Origin)
        {
            case SpikeOrigin.Floor:       // block แนวนอน โผล่ขึ้นจากพื้น
                Position = new Vector2(_basePosition.X, _basePosition.Y - ext);
                Scale    = new Vector2(SpikeLength, ext);
                break;
            case SpikeOrigin.Ceiling:     // block แนวนอน หย่อนลงจากเพดาน
                Position = new Vector2(_basePosition.X, _basePosition.Y);
                Scale    = new Vector2(SpikeLength, ext);
                break;
            case SpikeOrigin.LeftWall:    // block แนวตั้ง ยื่นออกจากกำแพงซ้าย
                Position = new Vector2(_basePosition.X, _basePosition.Y);
                Scale    = new Vector2(ext, SpikeLength);
                break;
            case SpikeOrigin.RightWall:   // block แนวตั้ง ยื่นออกจากกำแพงขวา
                Position = new Vector2(_basePosition.X - ext, _basePosition.Y);
                Scale    = new Vector2(ext, SpikeLength);
                break;
        }
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
