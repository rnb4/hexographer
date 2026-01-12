using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Brush that erases tiles on the active layer, with support for click-drag continuous erasing.
/// </summary>
public class EraserBrush : BaseBrush
{
    private bool _isErasing;
    private readonly HashSet<HexCoord> _erasedThisStroke = new();

    public override string Name => "Eraser";
    public override string Description => "Erase tiles on active layer (drag to erase continuously)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _isErasing = true;
            _erasedThisStroke.Clear();
            EraseAt(coord);
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(HexCoord? coord)
    {
        if (_isErasing && coord.HasValue && !_erasedThisStroke.Contains(coord.Value))
        {
            EraseAt(coord.Value);
            return true;
        }
        return false;
    }

    public override bool OnMouseUp(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left && _isErasing)
        {
            _isErasing = false;
            // TODO: Record undo action for entire stroke (Phase 5)
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _isErasing = false;
        _erasedThisStroke.Clear();
        base.OnDeactivate();
    }

    private void EraseAt(HexCoord coord)
    {
        Context.EraseTile(coord);
        _erasedThisStroke.Add(coord);
    }
}
