using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Hex;
using Hexographer.Editor.UndoRedo;

namespace Hexographer.Editor.Brushes;

using Scripts.Editor.Actions;

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

            if (line.Count > 0)
            {
                // Capture before state
                int layer = Context.ActiveLayer;
                var beforeState = TilePaintAction.CaptureState(Context.Grid, layer, line);

                // Paint the line
                Context.PaintTiles(line);

                // Capture after state
                var afterState = TilePaintAction.CaptureState(Context.Grid, layer, line);

                // Record undo action
                var description = line.Count == 1
                    ? "Draw point"
                    : $"Draw line ({line.Count} tiles)";

                var action = new TilePaintAction(
                    Context.Grid,
                    layer,
                    beforeState,
                    afterState,
                    description);

                Context.RecordUndoAction(action);
            }

            _isDragging = false;
            _startCoord = null;
            Context.ClearPreview();

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
