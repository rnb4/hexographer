using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;
using Hexographer.Editor.UndoRedo;

namespace Hexographer.Editor.Brushes;

using Scripts.Editor.Actions;

/// <summary>
/// Brush that paints one tile at a time, with support for click-drag continuous painting.
/// </summary>
public class SingleTileBrush : BaseBrush
{
    private bool _isPainting;
    private readonly HashSet<HexCoord> _paintedThisStroke = new();
    private readonly Dictionary<HexCoord, string?> _beforeState = new();
    private int _strokeLayer;

    public override string Name => "Brush";
    public override string Description => "Paint single tiles (drag to paint continuously)";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            _isPainting = true;
            _paintedThisStroke.Clear();
            _beforeState.Clear();
            _strokeLayer = Context.ActiveLayer;
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
            RecordUndoAction();
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _isPainting = false;
        _paintedThisStroke.Clear();
        _beforeState.Clear();
        base.OnDeactivate();
    }

    private void PaintAt(HexCoord coord)
    {
        // Capture before state if not already captured for this coord
        if (!_beforeState.ContainsKey(coord))
        {
            _beforeState[coord] = Context.Grid.GetTileLayer(coord, _strokeLayer);
        }

        Context.PaintTile(coord);
        _paintedThisStroke.Add(coord);
    }

    private void RecordUndoAction()
    {
        if (_paintedThisStroke.Count == 0)
            return;

        // Capture after state
        var afterState = new Dictionary<HexCoord, string?>();
        foreach (var coord in _paintedThisStroke)
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

        var description = _paintedThisStroke.Count == 1
            ? "Paint tile"
            : $"Paint {_paintedThisStroke.Count} tiles";

        var action = new TilePaintAction(
            Context.Grid,
            _strokeLayer,
            new Dictionary<HexCoord, string?>(_beforeState),
            afterState,
            description);

        Context.RecordUndoAction(action);
    }
}
