using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Brush that draws a line between two points.
/// Click to set start point, drag to preview, release to paint.
/// </summary>
public class LineBrush : BaseBrush
{
    private HexCoord? _startCoord;
    private bool _isDragging;

    public override string Name => "Line";
    public override string Description => "Draw line between two points (click-drag-release)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _startCoord = coord;
            _isDragging = true;
            Context.SetPreview(new[] { coord });
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(HexCoord? coord)
    {
        if (_isDragging && coord.HasValue && _startCoord.HasValue)
        {
            var line = HexMath.Line(_startCoord.Value, coord.Value);
            Context.SetPreview(line);
            return true;
        }
        return false;
    }

    public override bool OnMouseUp(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left && _isDragging && _startCoord.HasValue)
        {
            var line = HexMath.Line(_startCoord.Value, coord).ToList();
            Context.PaintTiles(line);

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
            return HexMath.Line(_startCoord.Value, coord);
        }
        return new[] { coord };
    }
}
