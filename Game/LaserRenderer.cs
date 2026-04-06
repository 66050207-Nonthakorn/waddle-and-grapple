using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

/// <summary>
/// วาด laser ด้วย tile-based rendering:
///   head (tile 0) + beam (tile 1, loop) + tail (tile 2, flip)
/// แต่ละ frame ใน spritesheet มีขนาด TileSize × (TileSize*3)
/// </summary>
public class LaserRenderer : Component
{
    // ขนาด tile ใน spritesheet (pixel)
    public const int TileSize = 16;

    private const float LayerBeam     = 0.55f;
    private const float LayerEndpoint = 0.56f;
    private const int   OnColumns     = 4;
    private const int   OffColumns    = 5;
    private const int   Rows          = 2;
    private const float AnimFrameDuration = 0.1f;

    private static readonly Color FallbackOnColor  = new Color(255, 50, 70);
    private static readonly Color FallbackOffColor = new Color(90, 20, 30);

    private Texture2D _pixel;
    private Texture2D _sheet;
    private LaserTrap _laser;

    private enum LaserAnimState { OnLoop, TurningOff, OffHold, TurningOn }
    private LaserAnimState _animState;
    private bool  _lastBeamOn;
    private float _animTimer;
    private int   _frame;

    public override void Initialize()
    {
        _pixel = ResourceManager.Instance.GetTexture("pixel");
        _laser = GameObject as LaserTrap;
        _sheet = ResourceManager.Instance.GetTexture(
            _laser != null && _laser.Style == LaserStyle.Floating
                ? "Traps/Laser/FloatingLaser"
                : "Traps/Laser/Laser"
        );

        _lastBeamOn = _laser?.BeamOn ?? true;
        if (_lastBeamOn)
        {
            _animState = LaserAnimState.OnLoop;
            _frame = 0;
        }
        else
        {
            _animState = LaserAnimState.OffHold;
            _frame = OffColumns - 1;
        }
    }

    public override void Update(GameTime gameTime)
    {
        if (_laser == null) return;

        bool beamOn = _laser.BeamOn;
        if (beamOn != _lastBeamOn)
        {
            _animTimer = 0f;
            if (beamOn)
            {
                _animState = LaserAnimState.TurningOn;
                _frame = OffColumns - 1;
            }
            else
            {
                _animState = LaserAnimState.TurningOff;
                _frame = 0;
            }
            _lastBeamOn = beamOn;
        }

        _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        while (_animTimer >= AnimFrameDuration)
        {
            _animTimer -= AnimFrameDuration;
            StepAnimation();
        }
    }

    private void StepAnimation()
    {
        switch (_animState)
        {
            case LaserAnimState.OnLoop:
                _frame = (_frame + 1) % OnColumns;
                break;

            case LaserAnimState.TurningOff:
                _frame++;
                if (_frame >= OffColumns - 1)
                {
                    _frame = OffColumns - 1;
                    _animState = LaserAnimState.OffHold;
                }
                break;

            case LaserAnimState.OffHold:
                _frame = OffColumns - 1;
                break;

            case LaserAnimState.TurningOn:
                _frame--;
                if (_frame <= 0)
                {
                    _frame = 0;
                    _animState = LaserAnimState.OnLoop;
                }
                break;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_laser == null) return;

        if (_sheet == null)
        {
            DrawFallback(spriteBatch);
            return;
        }

        // frame rect = (x, y, TileSize, TileSize*3)
        Rectangle frame  = GetCurrentFrameRect();
        int   srcTile    = frame.Width;                          // = TileSize (16)
        float dstTile    = srcTile * _laser.EndpointScale;      // rendered pixel size per tile

        // แยก 3 tile จาก frame เดียวกัน
        var headSrc = new Rectangle(frame.X, frame.Y,               srcTile, srcTile);
        var beamSrc = new Rectangle(frame.X, frame.Y + srcTile,     srcTile, srcTile);
        var tailSrc = new Rectangle(frame.X, frame.Y + srcTile * 2, srcTile, srcTile);

        Vector2 pos   = _laser.Position;
        bool    horiz = _laser.IsHorizontal;
        // rotate -90° สำหรับ horizontal เพื่อให้ sprite หันถูกทิศ
        float   rot   = horiz ? -MathF.PI / 2f : 0f;

        int totalTiles = Math.Max(3, (int)MathF.Round(_laser.BeamLength / dstTile));
        int beamTiles  = totalTiles - 2;

        // Head
        DrawTile(spriteBatch, headSrc,
            TileTopLeft(pos, 0, dstTile, horiz),
            dstTile, rot, SpriteEffects.None, LayerEndpoint);

        // Beam (loop)
        for (int i = 0; i < beamTiles; i++)
            DrawTile(spriteBatch, beamSrc,
                TileTopLeft(pos, i + 1, dstTile, horiz),
                dstTile, rot, SpriteEffects.None, LayerBeam);

        // Tail
        DrawTile(spriteBatch, tailSrc,
            TileTopLeft(pos, totalTiles - 1, dstTile, horiz),
            dstTile, rot, SpriteEffects.None, LayerEndpoint);
    }

    // คำนวณ top-left ของ tile ที่ index ตามทิศทาง laser
    private static Vector2 TileTopLeft(Vector2 origin, int index, float tileSize, bool horiz) =>
        horiz
            ? new Vector2(origin.X + tileSize * index, origin.Y)
            : new Vector2(origin.X, origin.Y + tileSize * index);

    // วาด tile เดียว โดยส่ง topLeft แล้วแปลงเป็น center สำหรับ rotation
    private void DrawTile(SpriteBatch sb, Rectangle src, Vector2 topLeft,
                          float dstSize, float rotation, SpriteEffects effects, float layer)
    {
        Vector2 center    = topLeft + new Vector2(dstSize * 0.5f, dstSize * 0.5f);
        Vector2 srcOrigin = new Vector2(src.Width * 0.5f, src.Height * 0.5f);
        float   scale     = dstSize / src.Width;
        sb.Draw(_sheet, center, src, Color.White, rotation, srcOrigin,
                new Vector2(scale), effects, layer);
    }

    // Fallback: วาด block สีเดียวเมื่อไม่มี texture
    private void DrawFallback(SpriteBatch spriteBatch)
    {
        if (_pixel == null) return;
        float dstTile  = TileSize * _laser.EndpointScale;
        int   n        = Math.Max(3, (int)MathF.Round(_laser.BeamLength / dstTile));
        float totalLen = n * dstTile;
        Color col      = _animState == LaserAnimState.OffHold ? FallbackOffColor : FallbackOnColor;
        bool  horiz    = _laser.IsHorizontal;

        DrawRect(spriteBatch,
            _laser.Position,
            horiz ? totalLen : dstTile,
            horiz ? dstTile  : totalLen,
            col, LayerBeam);
    }

    private void DrawRect(SpriteBatch sb, Vector2 topLeft, float w, float h, Color color, float layer)
    {
        if (w <= 0 || h <= 0) return;
        sb.Draw(_pixel, topLeft, null, color, 0f, Vector2.Zero, new Vector2(w, h),
                SpriteEffects.None, layer);
    }

    // คืน rectangle ของ frame ปัจจุบันในชีท (ขนาด TileSize × TileSize*3)
    private Rectangle GetCurrentFrameRect()
    {
        int columns    = _animState == LaserAnimState.OnLoop ? OnColumns : OffColumns;
        int row        = _animState == LaserAnimState.OnLoop ? 0 : 1;
        int cellW      = _sheet.Width  / OffColumns;   // fixed grid จาก row ที่มี col มากสุด
        int cellH      = _sheet.Height / Rows;
        int frameIndex = Math.Clamp(_frame, 0, columns - 1);
        return new Rectangle(frameIndex * cellW, row * cellH, cellW, cellH);
    }
}
