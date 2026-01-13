using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;

namespace Hexographer.Rendering;

/// <summary>
/// Draws grid lines, selection indicators, and brush preview using Godot's custom drawing.
/// Hover highlighting is handled separately by HexHoverOverlay for better performance.
/// </summary>
public partial class HexGridOverlay : Node2D
{
    private HexLayout _layout = null!;
    private HexGrid _grid = null!;

    /// <summary>
    /// Whether to show grid lines for all tiles.
    /// </summary>
    public bool ShowGrid { get; set; } = true;

    /// <summary>
    /// Whether to show grid lines for empty hexes within bounds.
    /// </summary>
    public bool ShowEmptyHexes { get; set; } = false;

    /// <summary>
    /// Selected hex coordinates.
    /// </summary>
    public HashSet<HexCoord> SelectedHexes { get; } = new();

    /// <summary>
    /// Preview hex coordinates (for brush preview).
    /// </summary>
    public HashSet<HexCoord> PreviewHexes { get; } = new();

    /// <summary>
    /// Color for grid lines.
    /// </summary>
    public Color GridColor { get; set; } = new(0.3f, 0.3f, 0.3f, 0.5f);

    /// <summary>
    /// Color for selection highlight.
    /// </summary>
    public Color SelectionColor { get; set; } = new(0f, 0.5f, 1f, 0.4f);

    /// <summary>
    /// Color for brush preview.
    /// </summary>
    public Color PreviewColor { get; set; } = new(1f, 1f, 1f, 0.25f);

    /// <summary>
    /// Width of grid lines.
    /// </summary>
    public float GridLineWidth { get; set; } = 1.0f;

    /// <summary>
    /// Width of highlight outlines.
    /// </summary>
    public float HighlightLineWidth { get; set; } = 2.0f;

    /// <summary>
    /// Initializes the overlay.
    /// </summary>
    public void Initialize(HexLayout layout, HexGrid grid)
    {
        _layout = layout;
        _grid = grid;
        Name = "Overlay";
    }

    /// <summary>
    /// Sets the selection and triggers redraw.
    /// </summary>
    public void SetSelection(IEnumerable<HexCoord> coords)
    {
        SelectedHexes.Clear();
        foreach (var coord in coords)
        {
            SelectedHexes.Add(coord);
        }
        QueueRedraw();
    }

    /// <summary>
    /// Adds a hex to the selection.
    /// </summary>
    public void AddToSelection(HexCoord coord)
    {
        if (SelectedHexes.Add(coord))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Removes a hex from the selection.
    /// </summary>
    public void RemoveFromSelection(HexCoord coord)
    {
        if (SelectedHexes.Remove(coord))
        {
            QueueRedraw();
        }
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        if (SelectedHexes.Count > 0)
        {
            SelectedHexes.Clear();
            QueueRedraw();
        }
    }

    /// <summary>
    /// Sets the preview hexes (for brush preview) and triggers redraw.
    /// </summary>
    public void SetPreview(IEnumerable<HexCoord> coords)
    {
        PreviewHexes.Clear();
        foreach (var coord in coords)
        {
            PreviewHexes.Add(coord);
        }
        QueueRedraw();
    }

    /// <summary>
    /// Clears the preview.
    /// </summary>
    public void ClearPreview()
    {
        if (PreviewHexes.Count > 0)
        {
            PreviewHexes.Clear();
            QueueRedraw();
        }
    }

    /// <summary>
    /// Forces a redraw of the overlay.
    /// </summary>
    public void Refresh()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        // Draw grid lines for existing tiles
        if (ShowGrid)
        {
            DrawGridLines();
        }

        // Draw preview highlights (lowest priority)
        foreach (var coord in PreviewHexes)
        {
            DrawHexFilled(coord, PreviewColor);
        }

        // Draw selection highlights
        foreach (var coord in SelectedHexes)
        {
            DrawHexFilled(coord, SelectionColor);
            DrawHexOutline(coord, SelectionColor.Lightened(0.3f), HighlightLineWidth);
        }
    }

    /// <summary>
    /// Draws grid lines for all tiles in the grid.
    /// </summary>
    private void DrawGridLines()
    {
        foreach (var coord in _grid.GetAllCoords())
        {
            DrawHexOutline(coord, GridColor, GridLineWidth);
        }

        // Optionally draw empty hexes within the bounding hexagonal region
        if (ShowEmptyHexes)
        {
            foreach (var coord in _grid.GetEmptyCoordsInBoundingRegion())
            {
                DrawHexOutline(coord, GridColor.Darkened(0.5f), GridLineWidth * 0.5f);
            }
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
}
