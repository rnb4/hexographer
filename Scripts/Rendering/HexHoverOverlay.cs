using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Rendering;

/// <summary>
/// Lightweight overlay that only handles hover highlighting.
/// Separated from HexGridOverlay to reduce redraw frequency since hover changes every frame.
/// </summary>
public partial class HexHoverOverlay : Node2D
{
    private HexLayout _layout = null!;

    /// <summary>
    /// The currently hovered hex coordinate.
    /// </summary>
    public HexCoord? HoveredHex { get; private set; }

    /// <summary>
    /// Color for hover highlight fill.
    /// </summary>
    public Color HoverColor { get; set; } = new(1f, 1f, 0f, 0.3f);

    /// <summary>
    /// Width of hover outline.
    /// </summary>
    public float HighlightLineWidth { get; set; } = 2.0f;

    /// <summary>
    /// Initializes the overlay.
    /// </summary>
    public void Initialize(HexLayout layout)
    {
        _layout = layout;
        Name = "HoverOverlay";
    }

    /// <summary>
    /// Sets the currently hovered hex and triggers redraw.
    /// </summary>
    public void SetHoveredHex(HexCoord? coord)
    {
        if (HoveredHex != coord)
        {
            HoveredHex = coord;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        if (HoveredHex.HasValue)
        {
            DrawHexFilled(HoveredHex.Value, HoverColor);
            DrawHexOutline(HoveredHex.Value, HoverColor.Lightened(0.3f), HighlightLineWidth);
        }
    }

    /// <summary>
    /// Draws the outline of a hex.
    /// </summary>
    private void DrawHexOutline(HexCoord coord, Color color, float width)
    {
        var corners = GetHexCorners(coord);

        // Close the polygon by adding first point at end
        var closedCorners = new Vector2[7];
        for (int i = 0; i < 6; i++)
        {
            closedCorners[i] = corners[i];
        }
        closedCorners[6] = corners[0];

        DrawPolyline(closedCorners, color, width, true);
    }

    /// <summary>
    /// Draws a filled hex.
    /// </summary>
    private void DrawHexFilled(HexCoord coord, Color color)
    {
        var corners = GetHexCorners(coord);
        DrawColoredPolygon(corners, color);
    }

    /// <summary>
    /// Gets the corner points of a hex in pixel coordinates.
    /// </summary>
    private Vector2[] GetHexCorners(HexCoord coord)
    {
        var corners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = _layout.HexCorner(coord, i);
        }
        return corners;
    }

    /// <summary>
    /// Updates the layout reference (used when orientation changes).
    /// </summary>
    public void UpdateLayout(HexLayout layout)
    {
        _layout = layout;
        QueueRedraw();
    }
}
