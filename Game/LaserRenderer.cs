using ComputerGameFinal.Engine.Components;
using ComputerGameFinal.Engine.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ComputerGameFinal.Game;

/// <summary>
/// วาด laser 3 ส่วน: head (เหลือง) + beam (ม่วง) + tail (ส้ม)
/// ใช้ 1×1 pixel texture ขยาย scale เหมือน PickaxeRenderer
/// </summary>
public class LaserRenderer : Component
{
    public const float EndpointSize = 14f;
    private const float BeamThickness = 8f;

    private static readonly Color BeamColor = new Color(220,  40, 220);  // ม่วง
    private static readonly Color HeadColor = new Color(255, 220,   0);  // เหลือง
    private static readonly Color TailColor = new Color(255, 140,   0);  // ส้ม
    private static readonly Color BeamOffColor = new Color(60, 10, 60);  // ม่วงเข้ม (ดับ)

    private const float LayerBeam     = 0.55f;
    private const float LayerEndpoint = 0.56f;

    private Texture2D  _pixel;
    private LaserTrap  _laser;

    public override void Initialize()
    {
        _pixel = ResourceManager.Instance.GetTexture("pixel");
        _laser = GameObject as LaserTrap;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (_pixel == null || _laser == null) return;

        Vector2 pos   = _laser.Position;
        float   len   = _laser.BeamLength;
        float   E     = EndpointSize;
        float   T     = BeamThickness;
        bool    horiz = _laser.IsHorizontal;

        // Head — วาดเสมอ
        DrawRect(spriteBatch, pos, E, E, HeadColor, LayerEndpoint);

        // Beam — สีต่างกันเมื่อ on/off
        Color beamCol = _laser.BeamOn ? BeamColor : BeamOffColor;
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

        // Tail — วาดเสมอ
        Vector2 tailPos = horiz
            ? new Vector2(pos.X + len - E, pos.Y)
            : new Vector2(pos.X,           pos.Y + len - E);
        DrawRect(spriteBatch, tailPos, E, E, TailColor, LayerEndpoint);
    }

    private void DrawRect(SpriteBatch sb, Vector2 topLeft, float w, float h, Color color, float layer)
    {
        if (w <= 0 || h <= 0) return;
        sb.Draw(_pixel, topLeft, null, color, 0f, Vector2.Zero, new Vector2(w, h), SpriteEffects.None, layer);
    }
}
