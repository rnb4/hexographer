using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// The shape mode for area selection.
/// </summary>
public enum AreaShape
{
    Rectangle,
    HexCircle
}

/// <summary>
/// Brush that fills rectangular or hexagonal circle areas.
/// Click to set start, drag to preview, release to fill.
/// Right-click to toggle between rectangle and hex circle modes.
/// </summary>
public class AreaBrush : BaseBrush
{
    private HexCoord? _startCoord;
    private bool _isDragging;

    /// <summary>
    /// The current area shape mode.
    /// </summary>
    public AreaShape Shape { get; set; } = AreaShape.Rectangle;

    public override string Name => "Area";
    public override string Description => "Fill area (click-drag, right-click to toggle shape)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _startCoord = coord;
            _isDragging = true;
            Context.SetPreview(new[] { coord });
            return true;
        }
        else if (button == MouseButton.Right)
        {
            // Toggle shape mode
            Shape = Shape == AreaShape.Rectangle ? AreaShape.HexCircle : AreaShape.Rectangle;
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(HexCoord? coord)
    {
        if (_isDragging && coord.HasValue && _startCoord.HasValue)
        {
            var area = GetArea(_startCoord.Value, coord.Value);
            Context.SetPreview(area);
            return true;
        }
        return false;
    }

    public override bool OnMouseUp(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left && _isDragging && _startCoord.HasValue)
        {
            var area = GetArea(_startCoord.Value, coord).ToList();
            Context.PaintTiles(area);

            _isDragging = false;
            _startCoord = null;
            Context.ClearPreview();

            // TODO: Record undo action (Phase 5)
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _isDragging = false;
        _startCoord = null;
        base.OnDeactivate();
    }

    public override IEnumerable<HexCoord> GetPreviewCoords(HexCoord coord)
    {
        if (_isDragging && _startCoord.HasValue)
        {
            return GetArea(_startCoord.Value, coord);
        }
        return new[] { coord };
    }

    private IEnumerable<HexCoord> GetArea(HexCoord start, HexCoord end)
    {
        if (Shape == AreaShape.Rectangle)
        {
            return HexMath.Rectangle(start, end);
        }
        else
        {
            // Hex circle: start is center, radius is distance to end
            int radius = start.DistanceTo(end);
            return start.GetHexesInRange(radius);
        }
    }
}
