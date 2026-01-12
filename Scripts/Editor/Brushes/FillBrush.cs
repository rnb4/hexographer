using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Brush that flood fills a contiguous area of the same tile type.
/// </summary>
public class FillBrush : BaseBrush
{
    private const int MaxFillDistance = 100;

    private HexCoord? _lastPreviewCoord;
    private List<HexCoord> _cachedFillArea = new();

    public override string Name => "Fill";
    public override string Description => "Flood fill contiguous area of same tile type";

    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        if (button == MouseButton.Left)
        {
            // Use cached fill area if available and still valid
            if (_lastPreviewCoord == coord && _cachedFillArea.Count > 0)
            {
                Context.PaintTiles(_cachedFillArea);
            }
            else
            {
                var fillCoords = GetFillArea(coord).ToList();
                Context.PaintTiles(fillCoords);
            }

            // Clear cache since the grid has changed
            _cachedFillArea.Clear();
            _lastPreviewCoord = null;

            // TODO: Record undo action (Phase 5)
            return true;
        }
        return false;
    }

    public override bool OnMouseMove(HexCoord? coord)
    {
        if (coord.HasValue && coord != _lastPreviewCoord)
        {
            _lastPreviewCoord = coord;
            _cachedFillArea = GetFillArea(coord.Value).ToList();
            Context.SetPreview(_cachedFillArea);
            return true;
        }
        return false;
    }

    public override void OnDeactivate()
    {
        _lastPreviewCoord = null;
        _cachedFillArea.Clear();
        base.OnDeactivate();
    }

    public override IEnumerable<HexCoord> GetPreviewCoords(HexCoord coord)
    {
        // Return cached if still valid
        if (_lastPreviewCoord == coord && _cachedFillArea.Count > 0)
        {
            return _cachedFillArea;
        }

        return GetFillArea(coord);
    }

    private IEnumerable<HexCoord> GetFillArea(HexCoord start)
    {
        var startType = Context.GetTileAt(start);

        // Don't fill if we're painting the same type
        if (startType == Context.SelectedTileTypeId)
        {
            return Enumerable.Empty<HexCoord>();
        }

        return HexMath.FloodFill(
            start,
            coord => Context.GetTileAt(coord) == startType,
            MaxFillDistance
        );
    }
}
