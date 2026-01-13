using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;
using Hexographer.Editor.UndoRedo;

namespace Hexographer.Editor.Brushes;

using Scripts.Editor.Actions;

/// <summary>
/// Brush that erases tiles on the active layer, with support for click-drag continuous erasing.
/// </summary>
public class EraserBrush : BaseBrush
{
    private bool _isErasing;
    private readonly HashSet<HexCoord> _erasedThisStroke = new();
    private readonly Dictionary<HexCoord, string?> _beforeState = new();
    private int _strokeLayer;

    public override string Name => "Eraser";
    public override string Description => "Erase tiles on active layer (drag to erase continuously)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _isErasing = true;
            _erasedThisStroke.Clear();
            _beforeState.Clear();
            _strokeLayer = Context.ActiveLayer;
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
            RecordUndoAction();
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _isErasing = false;
        _erasedThisStroke.Clear();
        _beforeState.Clear();
        base.OnDeactivate();
    }

    private void EraseAt(HexCoord coord)
    {
        // Capture before state if not already captured for this coord
        if (!_beforeState.ContainsKey(coord))
        {
            _beforeState[coord] = Context.Grid.GetTileLayer(coord, _strokeLayer);
        }

        Context.EraseTile(coord);
        _erasedThisStroke.Add(coord);
    }

    private void RecordUndoAction()
    {
        if (_erasedThisStroke.Count == 0)
            return;

        // Capture after state (should all be null since we erased)
        var afterState = new Dictionary<HexCoord, string?>();
        foreach (var coord in _erasedThisStroke)
        {
            afterState[coord] = Context.Grid.GetTileLayer(coord, _strokeLayer);
        }

        // Check if any changes were made
        bool hasChanges = false;
        foreach (var coord in _beforeState.Keys)
        {
            if (_beforeState[coord] != afterState.GetValueOrDefault(coord))
            {
                hasChanges = true;
                break;
            }
        }

        if (!hasChanges)
            return;

        var description = _erasedThisStroke.Count == 1
            ? "Erase tile"
            : $"Erase {_erasedThisStroke.Count} tiles";

        var action = new TilePaintAction(
            Context.Grid,
            _strokeLayer,
            new Dictionary<HexCoord, string?>(_beforeState),
            afterState,
            description);

        Context.RecordUndoAction(action);
    }
}
