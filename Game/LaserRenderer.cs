using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

/// <summary>
/// วาด laser 3 ส่วน: head (เหลือง) + beam (ม่วง) + tail (ส้ม)
/// ใช้ 1×1 pixel texture ขยาย scale เหมือน PickaxeRenderer
/// </summary>
public class LaserRenderer : Component
{
    public const float EndpointSize = 14f;
    public const float BeamThickness = 8f;

    private static readonly Color BeamColor = new Color(255, 50, 70);     // red (on)
    private static readonly Color BeamOffColor = new Color(90, 20, 30);   // dark red (off)

    private const float LayerBeam     = 0.55f;
    private const float LayerEndpoint = 0.56f;
    private const int OnColumns = 4;
    private const int OffColumns = 5;
    private const int Rows = 2;
    private const float AnimFrameDuration = 0.1f;

    private Texture2D  _pixel;
    private Texture2D  _sheet;
    private LaserTrap  _laser;

    private enum LaserAnimState { OnLoop, TurningOff, OffHold, TurningOn }
    private LaserAnimState _animState;
    private bool _lastBeamOn;
    private float _animTimer;
    private int _frame;

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
                // Opening animation = reverse of the closing row.
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
        if (_pixel == null || _laser == null) return;

        Vector2 pos   = _laser.Position;
        float   len   = _laser.BeamLength;
        float   E     = EndpointSize * _laser.EndpointScale;
        float   T     = BeamThickness * _laser.BeamThicknessScale;
        bool    horiz = _laser.IsHorizontal;

        DrawBeam(spriteBatch, pos, len, E, T, horiz);
        DrawAnimatedEndpoints(spriteBatch, pos, len, E, horiz);
    }

    private void DrawBeam(SpriteBatch spriteBatch, Vector2 pos, float len, float E, float T, bool horiz)
    {
        if (_sheet == null)
        {
            Color beamCol = _animState == LaserAnimState.OffHold ? BeamOffColor : BeamColor;
            DrawBeamFallback(spriteBatch, pos, len, E, T, horiz, beamCol);
            return;
        }

        Rectangle frameRect = GetCurrentFrameRect();
        Rectangle beamSrc = GetBeamSource(frameRect);

        int Round(float v) => (int)MathF.Round(v);

        int e = Math.Max(0, Round(E));
        int t = Math.Max(1, Round(T));
        int x0 = Round(pos.X);
        int y0 = Round(pos.Y);
        int L  = Math.Max(0, Round(len));
        int overlap = Math.Clamp(e / 12, 1, 2); // hide transparent seams between cap & beam

        if (_laser.Style == LaserStyle.Floating)
        {
            int beamOffset = Round((E - T) * 0.5f);
            if (horiz)
            {
                // Beam runs between caps to avoid gaps.
                int headLeft = x0;
                int tailLeft = x0 + L - e;
                int beamLeft = headLeft + e - overlap;
                int beamW = Math.Max(0, (tailLeft + overlap) - beamLeft);
                DrawBeamStrip(spriteBatch, beamSrc, new Rectangle(beamLeft, y0 + beamOffset, beamW, t));
            }
            else
            {
                int headTop = y0;
                int tailTop = y0 + L - e;
                int beamTop = headTop + e - overlap;
                int beamH = Math.Max(0, (tailTop + overlap) - beamTop);
                DrawBeamStrip(spriteBatch, beamSrc, new Rectangle(x0 + beamOffset, beamTop, t, beamH));
            }
            return;
        }

        // Wall-mounted: caps at ends, beam in-between.
        int offset = Round((E - T) * 0.5f);
        if (horiz)
        {
            int headLeft = x0;
            int tailLeft = x0 + L - e;
            int beamLeft = headLeft + e - overlap;
            int beamW = Math.Max(0, (tailLeft + overlap) - beamLeft);
            DrawBeamStrip(spriteBatch, beamSrc, new Rectangle(beamLeft, y0 + offset, beamW, t));
        }
        else
        {
            int headTop = y0;
            int tailTop = y0 + L - e;
            int beamTop = headTop + e - overlap;
            int beamH = Math.Max(0, (tailTop + overlap) - beamTop);
            DrawBeamStrip(spriteBatch, beamSrc, new Rectangle(x0 + offset, beamTop, t, beamH));
        }
    }

    private void DrawBeamStrip(SpriteBatch sb, Rectangle beamSrc, Rectangle destRect)
    {
        if (destRect.Width <= 0 || destRect.Height <= 0) return;

        sb.Draw(_sheet, destRect, beamSrc, Color.White, 0f, Vector2.Zero, SpriteEffects.None, LayerBeam);
    }

    private void DrawBeamFallback(SpriteBatch spriteBatch, Vector2 pos, float len, float E, float T, bool horiz, Color beamCol)
    {
        if (_laser.Style == LaserStyle.Floating)
        {
            float beamOffset = (E - T) / 2f;
            if (horiz)
                DrawRect(spriteBatch, new Vector2(pos.X, pos.Y + beamOffset), len, T, beamCol, LayerBeam);
            else
                DrawRect(spriteBatch, new Vector2(pos.X + beamOffset, pos.Y), T, len, beamCol, LayerBeam);
            return;
        }

        if (horiz)
        {
            float beamY = pos.Y + (E - T) / 2f;
            DrawRect(spriteBatch, new Vector2(pos.X + E, beamY), len - E * 2, T, beamCol, LayerBeam);
        }
        else
        {
            float beamX = pos.X + (E - T) / 2f;
            DrawRect(spriteBatch, new Vector2(beamX, pos.Y + E), T, len - E * 2, beamCol, LayerBeam);
        }
    }

    private void DrawAnimatedEndpoints(SpriteBatch spriteBatch, Vector2 pos, float len, float E, bool horiz)
    {
        if (_sheet == null)
        {
            // Fallback to simple blocks if texture is missing.
            DrawRect(spriteBatch, new Vector2(pos.X, pos.Y), E, E, Color.Cyan, LayerEndpoint);
            Vector2 tailFallback = horiz
                ? new Vector2(pos.X + len - E, pos.Y)
                : new Vector2(pos.X, pos.Y + len - E);
            DrawRect(spriteBatch, tailFallback, E, E, Color.Cyan, LayerEndpoint);
            return;
        }

        Rectangle frameRect = GetCurrentFrameRect();

        // Each frame contains: [head cap (square)] + [beam] + [tail cap (square)] vertically.
        // Cap size is the frame width (caps are square).
        int capSize = Math.Min(frameRect.Width, frameRect.Height);
        if (capSize <= 0) return;

        // Prevent overlap if the frame is unexpectedly short.
        capSize = Math.Min(capSize, frameRect.Height / 2);

        var headSrc = new Rectangle(frameRect.X, frameRect.Y, frameRect.Width, capSize);

        int Round(float v) => (int)MathF.Round(v);
        int x0 = Round(pos.X);
        int y0 = Round(pos.Y);
        int e  = Math.Max(0, Round(E));
        int L  = Math.Max(0, Round(len));

        Vector2 headPos = new Vector2(x0, y0);
        Vector2 tailPos = horiz
            ? new Vector2(x0 + L - e, y0)
            : new Vector2(x0, y0 + L - e);

        if (horiz)
        {
            DrawCap(spriteBatch, headPos, headSrc, E, -MathF.PI / 2f, SpriteEffects.None);
            DrawCap(spriteBatch, tailPos, headSrc, E, -MathF.PI / 2f, SpriteEffects.FlipVertically);
        }
        else
        {
            DrawCap(spriteBatch, headPos, headSrc, E, 0f, SpriteEffects.None);
            DrawCap(spriteBatch, tailPos, headSrc, E, 0f, SpriteEffects.FlipVertically);
        }
    }

    private Rectangle GetCurrentFrameRect()
    {
        int columns = _animState == LaserAnimState.OnLoop ? OnColumns : OffColumns;
        int row = _animState == LaserAnimState.OnLoop ? 0 : 1;
        int cellW = _sheet.Width / OffColumns; // fixed grid width from max-column row
        int cellH = _sheet.Height / Rows;
        int frameIndex = Math.Clamp(_frame, 0, columns - 1);
        return new Rectangle(frameIndex * cellW, row * cellH, cellW, cellH);
    }

    private Rectangle GetBeamSource(Rectangle frameRect)
    {
        // Frame layout is vertical: [head][beam][tail].
        // Keep full beam strip from the middle section.
        int capSize = Math.Min(frameRect.Width, frameRect.Height / 2);
        int beamY = frameRect.Y + capSize;
        int beamH = Math.Max(1, frameRect.Height - (capSize * 2));
        return new Rectangle(frameRect.X, beamY, frameRect.Width, beamH);
    }

    private void DrawCap(SpriteBatch sb, Vector2 topLeft, Rectangle src, float size, float rotation, SpriteEffects effects)
    {
        Vector2 center = topLeft + new Vector2(size * 0.5f, size * 0.5f);
        Vector2 origin = new Vector2(src.Width * 0.5f, src.Height * 0.5f);
        Vector2 scale = new Vector2(size / src.Width, size / src.Height);
        sb.Draw(_sheet, center, src, Color.White, rotation, origin, scale, effects, LayerEndpoint);
    }

    private void DrawRect(SpriteBatch sb, Vector2 topLeft, float w, float h, Color color, float layer)
    {
        if (w <= 0 || h <= 0) return;
        sb.Draw(_pixel, topLeft, null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, layer);
    }
}
