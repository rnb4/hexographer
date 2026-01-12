using System;
using Godot;

namespace Hexographer.Core.Hex;

/// <summary>
/// Defines the orientation of hexagons in the grid.
/// </summary>
public enum HexOrientation
{
    /// <summary>
    /// Flat edges on top and bottom, pointy sides left and right.
    /// </summary>
    FlatTop,

    /// <summary>
    /// Pointy edges on top and bottom, flat sides left and right.
    /// </summary>
    PointyTop
}

/// <summary>
/// Provides layout calculations for converting between hex and pixel coordinates.
/// </summary>
public class HexLayout
{
    /// <summary>The orientation of hexes in this layout.</summary>
    public HexOrientation Orientation { get; }

    /// <summary>The size of a hex (distance from center to corner).</summary>
    public float Size { get; }

    /// <summary>The origin point of the grid in pixel coordinates.</summary>
    public Vector2 Origin { get; }

    // Orientation matrices for hex-to-pixel conversion
    private readonly float _f0, _f1, _f2, _f3;
    // Orientation matrices for pixel-to-hex conversion
    private readonly float _b0, _b1, _b2, _b3;
    // Starting angle for corner calculations
    private readonly float _startAngle;

    public HexLayout(HexOrientation orientation, float size, Vector2 origin)
    {
        Orientation = orientation;
        Size = size;
        Origin = origin;

        if (orientation == HexOrientation.PointyTop)
        {
            // Pointy-top orientation matrices
            _f0 = MathF.Sqrt(3f);
            _f1 = MathF.Sqrt(3f) / 2f;
            _f2 = 0f;
            _f3 = 3f / 2f;

            _b0 = MathF.Sqrt(3f) / 3f;
            _b1 = -1f / 3f;
            _b2 = 0f;
            _b3 = 2f / 3f;

            _startAngle = 0.5f; // 30 degrees in units of 60 degrees
        }
        else
        {
            // Flat-top orientation matrices
            _f0 = 3f / 2f;
            _f1 = 0f;
            _f2 = MathF.Sqrt(3f) / 2f;
            _f3 = MathF.Sqrt(3f);

            _b0 = 2f / 3f;
            _b1 = 0f;
            _b2 = -1f / 3f;
            _b3 = MathF.Sqrt(3f) / 3f;

            _startAngle = 0f; // 0 degrees
        }
    }

    /// <summary>
    /// Converts a hex coordinate to pixel position (center of hex).
    /// </summary>
    public Vector2 HexToPixel(HexCoord hex)
    {
        float x = (_f0 * hex.Q + _f1 * hex.R) * Size;
        float y = (_f2 * hex.Q + _f3 * hex.R) * Size;
        return new Vector2(x + Origin.X, y + Origin.Y);
    }

    /// <summary>
    /// Converts a pixel position to the hex coordinate containing that point.
    /// </summary>
    public HexCoord PixelToHex(Vector2 pixel)
    {
        Vector2 pt = new((pixel.X - Origin.X) / Size, (pixel.Y - Origin.Y) / Size);
        float q = _b0 * pt.X + _b1 * pt.Y;
        float r = _b2 * pt.X + _b3 * pt.Y;
        return HexRound(q, r);
    }

    /// <summary>
    /// Gets the pixel position of a corner of a hex.
    /// Corner 0 is at the start angle, incrementing counter-clockwise.
    /// </summary>
    public Vector2 HexCorner(HexCoord hex, int corner)
    {
        Vector2 center = HexToPixel(hex);
        float angle = 2f * MathF.PI * (_startAngle + corner) / 6f;
        return new Vector2(
            center.X + Size * MathF.Cos(angle),
            center.Y + Size * MathF.Sin(angle)
        );
    }

    /// <summary>
    /// Gets all 6 corners of a hex in order.
    /// </summary>
    public Vector2[] GetHexCorners(HexCoord hex)
    {
        var corners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = HexCorner(hex, i);
        }
        return corners;
    }

    /// <summary>
    /// Gets the width of a single hex.
    /// </summary>
    public float HexWidth => Orientation == HexOrientation.FlatTop
        ? Size * 2f
        : Size * MathF.Sqrt(3f);

    /// <summary>
    /// Gets the height of a single hex.
    /// </summary>
    public float HexHeight => Orientation == HexOrientation.FlatTop
        ? Size * MathF.Sqrt(3f)
        : Size * 2f;

    /// <summary>
    /// Gets the horizontal spacing between hex centers.
    /// </summary>
    public float HorizontalSpacing => Orientation == HexOrientation.FlatTop
        ? Size * 1.5f
        : Size * MathF.Sqrt(3f);

    /// <summary>
    /// Gets the vertical spacing between hex centers.
    /// </summary>
    public float VerticalSpacing => Orientation == HexOrientation.FlatTop
        ? Size * MathF.Sqrt(3f)
        : Size * 1.5f;

    /// <summary>
    /// Rounds fractional hex coordinates to the nearest hex.
    /// Uses cube coordinate rounding for accuracy.
    /// </summary>
    public static HexCoord HexRound(float q, float r)
    {
        float s = -q - r;

        int qi = (int)MathF.Round(q);
        int ri = (int)MathF.Round(r);
        int si = (int)MathF.Round(s);

        float qDiff = MathF.Abs(qi - q);
        float rDiff = MathF.Abs(ri - r);
        float sDiff = MathF.Abs(si - s);

        // Reset the component with largest rounding error
        if (qDiff > rDiff && qDiff > sDiff)
        {
            qi = -ri - si;
        }
        else if (rDiff > sDiff)
        {
            ri = -qi - si;
        }
        // else si would be reset, but we don't store it

        return new HexCoord(qi, ri);
    }

    /// <summary>
    /// Creates a default layout with pointy-top orientation.
    /// </summary>
    public static HexLayout CreateDefault(float size = 64f)
    {
        return new HexLayout(HexOrientation.PointyTop, size, Vector2.Zero);
    }
}
