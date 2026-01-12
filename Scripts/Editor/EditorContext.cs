using System;
using System.Collections.Generic;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;
using Hexographer.Rendering;

namespace Hexographer.Editor;

/// <summary>
/// Shared state container passed to brushes, providing access to grid, registry, and editor state.
/// </summary>
public class EditorContext
{
    /// <summary>
    /// The hex grid being edited.
    /// </summary>
    public HexGrid Grid { get; set; } = null!;

    /// <summary>
    /// The tile type registry.
    /// </summary>
    public TileRegistry Registry { get; set; } = null!;

    /// <summary>
    /// The grid renderer for preview and coordinate conversion.
    /// </summary>
    public HexGridRenderer Renderer { get; set; } = null!;

    /// <summary>
    /// The currently selected tile type ID for painting.
    /// </summary>
    public string? SelectedTileTypeId { get; set; }

    /// <summary>
    /// The currently active layer for editing.
    /// </summary>
    public int ActiveLayer { get; set; } = TileLayers.Ground;

    /// <summary>
    /// Callback for recording undoable actions (connected in Phase 5).
    /// </summary>
    public Action<object>? RecordAction { get; set; }

    /// <summary>
    /// Event fired when the active layer changes.
    /// </summary>
    public event Action<int>? ActiveLayerChanged;

    /// <summary>
    /// Event fired when the selected tile type changes.
    /// </summary>
    public event Action<string?>? SelectedTileTypeChanged;

    /// <summary>
    /// Sets the active layer and fires the change event.
    /// </summary>
    public void SetActiveLayer(int layer)
    {
        if (layer >= 0 && layer < Grid.LayerCount && layer != ActiveLayer)
        {
            ActiveLayer = layer;
            ActiveLayerChanged?.Invoke(layer);
        }
    }

    /// <summary>
    /// Sets the selected tile type and fires the change event.
    /// </summary>
    public void SetSelectedTileType(string? tileTypeId)
    {
        if (SelectedTileTypeId != tileTypeId)
        {
            SelectedTileTypeId = tileTypeId;
            SelectedTileTypeChanged?.Invoke(tileTypeId);
        }
    }

    /// <summary>
    /// Paints the selected tile type at the specified coordinate on the active layer.
    /// </summary>
    public void PaintTile(HexCoord coord)
    {
        if (string.IsNullOrEmpty(SelectedTileTypeId))
            return;

        Grid.SetTileLayer(coord, ActiveLayer, SelectedTileTypeId);
    }

    /// <summary>
    /// Paints multiple tiles with the selected tile type.
    /// </summary>
    public void PaintTiles(IEnumerable<HexCoord> coords)
    {
        if (string.IsNullOrEmpty(SelectedTileTypeId))
            return;

        foreach (var coord in coords)
        {
            Grid.SetTileLayer(coord, ActiveLayer, SelectedTileTypeId);
        }
    }

    /// <summary>
    /// Erases the tile at the specified coordinate on the active layer.
    /// </summary>
    public void EraseTile(HexCoord coord)
    {
        Grid.ClearTileLayer(coord, ActiveLayer);
    }

    /// <summary>
    /// Erases multiple tiles on the active layer.
    /// </summary>
    public void EraseTiles(IEnumerable<HexCoord> coords)
    {
        foreach (var coord in coords)
        {
            Grid.ClearTileLayer(coord, ActiveLayer);
        }
    }

    /// <summary>
    /// Sets the preview highlight for the given coordinates.
    /// </summary>
    public void SetPreview(IEnumerable<HexCoord> coords)
    {
        Renderer.SetPreview(coords);
    }

    /// <summary>
    /// Clears the preview highlight.
    /// </summary>
    public void ClearPreview()
    {
        Renderer.ClearPreview();
    }

    /// <summary>
    /// Gets the tile type ID at the specified coordinate on the active layer.
    /// </summary>
    public string? GetTileAt(HexCoord coord)
    {
        return Grid.GetTileLayer(coord, ActiveLayer);
    }

    /// <summary>
    /// Gets the tile type ID at the specified coordinate on a specific layer.
    /// </summary>
    public string? GetTileAt(HexCoord coord, int layer)
    {
        return Grid.GetTileLayer(coord, layer);
    }
}
