using System.Collections.Generic;
using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;

namespace Hexographer.Rendering;

/// <summary>
/// Main renderer that coordinates layer renderers and overlay, connects to HexGrid events.
/// </summary>
public partial class HexGridRenderer : Node2D
{
    private HexGrid _grid = null!;
    private TileRegistry _registry = null!;
    private HexLayout _layout = null!;
    private HexLayerRenderer[] _layerRenderers = null!;
    private HexGridOverlay _overlay = null!;
    private bool _initialized;

    /// <summary>
    /// The hex layout used for coordinate conversion.
    /// </summary>
    public HexLayout Layout => _layout;

    /// <summary>
    /// The grid being rendered.
    /// </summary>
    public HexGrid Grid => _grid;

    /// <summary>
    /// The tile registry.
    /// </summary>
    public TileRegistry Registry => _registry;

    /// <summary>
    /// Whether to show grid lines.
    /// </summary>
    public bool ShowGrid
    {
        get => _overlay?.ShowGrid ?? true;
        set
        {
            if (_overlay != null)
            {
                _overlay.ShowGrid = value;
                _overlay.Refresh();
            }
        }
    }

    /// <summary>
    /// Initializes the renderer with a grid and registry.
    /// </summary>
    public void Initialize(HexGrid grid, TileRegistry registry)
    {
        if (_initialized)
        {
            Cleanup();
        }

        _grid = grid;
        _registry = registry;
        _layout = new HexLayout(grid.Orientation, grid.HexSize, Vector2.Zero);

        // Subscribe to grid events
        _grid.TileChanged += OnTileChanged;
        _grid.TilesChanged += OnTilesChanged;
        _grid.GridCleared += OnGridCleared;

        // Create layer renderers
        _layerRenderers = new HexLayerRenderer[grid.LayerCount];
        for (int i = 0; i < grid.LayerCount; i++)
        {
            var layerRenderer = new HexLayerRenderer();
            layerRenderer.Initialize(i, _layout, _registry);
            AddChild(layerRenderer);
            _layerRenderers[i] = layerRenderer;
        }

        // Create overlay (on top of all layers)
        _overlay = new HexGridOverlay();
        _overlay.Initialize(_layout, _grid);
        _overlay.ShowEmptyHexes = true;
        AddChild(_overlay);

        _initialized = true;

        // Initial render
        RenderAll();
    }

    /// <summary>
    /// Cleans up event subscriptions and child nodes.
    /// </summary>
    public void Cleanup()
    {
        if (!_initialized)
            return;

        // Unsubscribe from grid events
        if (_grid != null)
        {
            _grid.TileChanged -= OnTileChanged;
            _grid.TilesChanged -= OnTilesChanged;
            _grid.GridCleared -= OnGridCleared;
        }

        // Remove all children
        foreach (var child in GetChildren())
        {
            child.QueueFree();
        }

        _layerRenderers = null!;
        _overlay = null!;
        _initialized = false;
    }

    /// <summary>
    /// Renders all tiles from the grid.
    /// </summary>
    public void RenderAll()
    {
        if (!_initialized)
            return;

        foreach (var layerRenderer in _layerRenderers)
        {
            layerRenderer.RenderAll(_grid);
        }

        _overlay.Refresh();
    }

    /// <summary>
    /// Refreshes a single tile.
    /// </summary>
    public void RefreshTile(HexCoord coord)
    {
        if (!_initialized)
            return;

        var tile = _grid.GetTile(coord);

        for (int i = 0; i < _layerRenderers.Length; i++)
        {
            var tileTypeId = tile?.GetLayer(i);
            _layerRenderers[i].RenderTile(coord, tileTypeId);
        }

        _overlay.Refresh();
    }

    /// <summary>
    /// Refreshes multiple tiles.
    /// </summary>
    public void RefreshTiles(IEnumerable<HexCoord> coords)
    {
        if (!_initialized)
            return;

        foreach (var coord in coords)
        {
            var tile = _grid.GetTile(coord);

            for (int i = 0; i < _layerRenderers.Length; i++)
            {
                var tileTypeId = tile?.GetLayer(i);
                _layerRenderers[i].RenderTile(coord, tileTypeId);
            }
        }

        _overlay.Refresh();
    }

    /// <summary>
    /// Sets the visibility of a layer.
    /// </summary>
    public void SetLayerVisible(int layerIndex, bool visible)
    {
        if (_initialized && layerIndex >= 0 && layerIndex < _layerRenderers.Length)
        {
            _layerRenderers[layerIndex].SetLayerVisible(visible);
        }
    }

    /// <summary>
    /// Gets the visibility of a layer.
    /// </summary>
    public bool IsLayerVisible(int layerIndex)
    {
        if (_initialized && layerIndex >= 0 && layerIndex < _layerRenderers.Length)
        {
            return _layerRenderers[layerIndex].Visible;
        }
        return false;
    }

    /// <summary>
    /// Sets the opacity of a layer.
    /// </summary>
    public void SetLayerOpacity(int layerIndex, float opacity)
    {
        if (_initialized && layerIndex >= 0 && layerIndex < _layerRenderers.Length)
        {
            _layerRenderers[layerIndex].SetLayerOpacity(opacity);
        }
    }

    /// <summary>
    /// Sets the hovered hex coordinate.
    /// </summary>
    public void SetHoveredHex(HexCoord? coord)
    {
        _overlay?.SetHoveredHex(coord);
    }

    /// <summary>
    /// Sets the selected hex coordinates.
    /// </summary>
    public void SetSelection(IEnumerable<HexCoord> coords)
    {
        _overlay?.SetSelection(coords);
    }

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        _overlay?.ClearSelection();
    }

    /// <summary>
    /// Sets the preview hex coordinates (for brush preview).
    /// </summary>
    public void SetPreview(IEnumerable<HexCoord> coords)
    {
        _overlay?.SetPreview(coords);
    }

    /// <summary>
    /// Clears the preview.
    /// </summary>
    public void ClearPreview()
    {
        _overlay?.ClearPreview();
    }

    /// <summary>
    /// Converts a pixel position to hex coordinate.
    /// </summary>
    public HexCoord PixelToHex(Vector2 pixel)
    {
        // Account for this node's global position
        var localPixel = pixel - GlobalPosition;
        return _layout.PixelToHex(localPixel);
    }

    /// <summary>
    /// Converts a hex coordinate to pixel position.
    /// </summary>
    public Vector2 HexToPixel(HexCoord hex)
    {
        return _layout.HexToPixel(hex) + GlobalPosition;
    }

    /// <summary>
    /// Gets the hex coordinate at the given global position, accounting for any camera transforms.
    /// </summary>
    public HexCoord GetHexAtGlobalPosition(Vector2 globalPosition)
    {
        var localPosition = ToLocal(globalPosition);
        return _layout.PixelToHex(localPosition);
    }

    // Event handlers

    private void OnTileChanged(HexCoord coord)
    {
        RefreshTile(coord);
    }

    private void OnTilesChanged(IEnumerable<HexCoord> coords)
    {
        RefreshTiles(coords);
    }

    private void OnGridCleared()
    {
        if (!_initialized)
            return;

        foreach (var layerRenderer in _layerRenderers)
        {
            layerRenderer.Clear();
        }

        _overlay.ClearSelection();
        _overlay.ClearPreview();
        _overlay.Refresh();
    }

    public override void _ExitTree()
    {
        Cleanup();
        base._ExitTree();
    }
}
