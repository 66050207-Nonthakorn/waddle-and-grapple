using System;
using WaddleAndGrapple.Engine.Components;
using WaddleAndGrapple.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace WaddleAndGrapple.Game;

/// <summary>
/// Renders a spinning saw blade.
/// All saw spritesheets have 2 rows:
///   row 0 = full saw (used for Full / Ceiling / Wall placements)
///   row 1 = top-half saw only (used for FloorMounted)
///
/// Large : 256x128, 4 cols → cell 64x64 (4x4 tiles)
/// Medium: 128x64,  4 cols → cell 32x32 (2x2 tiles)
/// Small :  48x32,  3 cols → cell 16x16 (1x1 tile) — always Full
///
/// Position is the surface contact / centre point depending on Placement.
/// </summary>
public class SawRenderer : Component
{
    private const float LayerDepth = 0.6f;

    private Texture2D _pixel;
    private Texture2D _sheet;
    private string    _loadedTextureName;
    private SawTrap   _saw;
    private float     _animTimer;
    private int       _frame;

    public override void Initialize()
    {
        _pixel = ResourceManager.Instance.GetTexture("pixel");
        _saw   = GameObject as SawTrap;
        if (_saw != null)
            _sheet = ResourceManager.Instance.GetTexture(_saw.SpriteTextureName);
    }

    public override void Update(GameTime gameTime)
    {
        if (_saw == null) return;
        _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (_animTimer >= _saw.AnimationFrameDuration)
        {
            _animTimer -= _saw.AnimationFrameDuration;
            _frame = (_frame + 1) % _saw.AnimationColumns;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_saw == null) return;

        // Reload texture if SpriteTextureName was set after Initialize (common in scene setup)
        if (_loadedTextureName != _saw.SpriteTextureName)
        {
            _loadedTextureName = _saw.SpriteTextureName;
            _sheet = string.IsNullOrEmpty(_loadedTextureName)
                ? null
                : ResourceManager.Instance.GetTexture(_loadedTextureName);
        }

        if (_sheet == null) { DrawFallback(spriteBatch); return; }

        int cols  = _saw.AnimationColumns;
        int cellW = _sheet.Width / cols;
        int cellH = cellW;              // cells are square (N×N sprite-tiles)
        if (cellW < 1) { DrawFallback(spriteBatch); return; }

        // row 0 = full saw (y=0)
        // row 1 = mounted half-saw (y = cellH + cellH/2, gap = cellH/2 between rows)
        int frameX   = _frame * cellW;
        int row1Y    = cellH + cellH / 2;
        int mountedH = cellH / 2;

        // Small saw (3 cols) always uses Full.
        var   placement = cols == 3 ? SawPlacement.Full : _saw.Placement;
        float scale     = _saw.BladeSize / cellW;

        Rectangle src;
        Vector2   origin;
        float     rotation = _saw.Rotation.Z;

        if (placement == SawPlacement.Full)
        {
            src    = new Rectangle(frameX, 0, cellW, cellH);
            origin = new Vector2(cellW * 0.5f, cellH * 0.5f);
        }
        else
        {
            // All mounted variants use row 1; rotation sets the surface direction.
            // Origin = center of contact edge (bottom of row-1 sprite) → sits at Position.
            src    = new Rectangle(frameX, row1Y, cellW, mountedH);
            origin = new Vector2(cellW * 0.5f, mountedH);
            rotation += placement switch
            {
                SawPlacement.CeilingMounted   =>  MathF.PI,
                SawPlacement.LeftWallMounted  =>  MathF.PI / 2f,
                SawPlacement.RightWallMounted => -MathF.PI / 2f,
                _                             =>  0f,   // FloorMounted — no extra rotation
            };
        }

        spriteBatch.Draw(_sheet, _saw.Position, src, _saw.SpriteTint,
                         rotation, origin, scale, SpriteEffects.None, LayerDepth);
    }

    private void DrawFallback(SpriteBatch spriteBatch)
    {
        if (_pixel == null || _saw == null) return;
        float half = _saw.BladeSize * 0.5f;
        spriteBatch.Draw(_pixel,
            new Vector2(_saw.Position.X - half, _saw.Position.Y - half),
            null, Color.Red, 0f, Vector2.Zero,
            new Vector2(_saw.BladeSize), SpriteEffects.None, LayerDepth);
    }
}
