using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Brush that paints one tile at a time, with support for click-drag continuous painting.
/// </summary>
public class SingleTileBrush : BaseBrush
{
    private bool _isPainting;
    private readonly HashSet<HexCoord> _paintedThisStroke = new();

    public override string Name => "Brush";
    public override string Description => "Paint single tiles (drag to paint continuously)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _isPainting = true;
            _paintedThisStroke.Clear();
            PaintAt(coord);
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(HexCoord? coord)
    {
        if (_isPainting && coord.HasValue && !_paintedThisStroke.Contains(coord.Value))
        {
            PaintAt(coord.Value);
            return true;
        }
        return false;
    }

    public override bool OnMouseUp(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left && _isPainting)
        {
            _isPainting = false;
            // TODO: Record undo action for entire stroke (Phase 5)
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _isPainting = false;
        _paintedThisStroke.Clear();
        base.OnDeactivate();
    }

    private void PaintAt(HexCoord coord)
    {
        Context.PaintTile(coord);
        _paintedThisStroke.Add(coord);
    }
}
