using System.Collections.Generic;
using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;

namespace Hexographer.Rendering;

/// <summary>
/// Renders a single layer of the hex grid using Polygon2D nodes (colored) or Sprite2D nodes (textured).
/// </summary>
public partial class HexLayerRenderer : Node2D
{
    private int _layerIndex;
    private HexLayout _layout = null!;
    private TileRegistry _registry = null!;
    private HexGrid _grid = null!;
    private readonly Dictionary<HexCoord, Node2D> _tileNodes = new();

    /// <summary>
    /// The layer index this renderer handles.
    /// </summary>
    public int LayerIndex => _layerIndex;

    /// <summary>
    /// Initializes the layer renderer.
    /// </summary>
    public void Initialize(int layerIndex, HexLayout layout, TileRegistry registry, HexGrid grid)
    {
        _layerIndex = layerIndex;
        _layout = layout;
        _registry = registry;
        _grid = grid;
        Name = $"Layer{layerIndex}_{TileLayers.GetLayerName(layerIndex)}";
        YSortEnabled = true;
    }

    /// <summary>
    /// Renders or updates a tile at the specified coordinate.
    /// </summary>
    public void RenderTile(HexCoord coord, string? tileTypeId)
    {
        // Remove existing tile visual if present
        if (_tileNodes.TryGetValue(coord, out var existingNode))
        {
            existingNode.QueueFree();
            _tileNodes.Remove(coord);
        }

        // If no tile type, we're done (tile was cleared)
        if (string.IsNullOrEmpty(tileTypeId))
            return;

        // Get tile type definition
        var tileType = _registry.GetTileType(tileTypeId);
        if (tileType == null)
            return;

        // Only render tiles for this layer
        if (tileType.LayerIndex != _layerIndex)
            return;

        // Create visual node
        var tileNode = CreateTileVisual(coord, tileType);
        if (tileNode != null)
        {
            AddChild(tileNode);
            _tileNodes[coord] = tileNode;
        }
    }

    /// <summary>
    /// Clears the tile visual at the specified coordinate.
    /// </summary>
    public void ClearTile(HexCoord coord)
    {
        if (_tileNodes.TryGetValue(coord, out var node))
        {
            node.QueueFree();
            _tileNodes.Remove(coord);
        }
    }

    /// <summary>
    /// Renders all tiles from the grid for this layer.
    /// </summary>
    public void RenderAll(HexGrid grid)
    {
        Clear();

        foreach (var tile in grid.GetTilesWithLayer(_layerIndex))
        {
            var tileTypeId = tile.GetLayer(_layerIndex);
            if (!string.IsNullOrEmpty(tileTypeId))
            {
                RenderTile(tile.Coord, tileTypeId);
            }
        }
    }

    /// <summary>
    /// Clears all tile visuals from this layer.
    /// </summary>
    public void Clear()
    {
        foreach (var node in _tileNodes.Values)
        {
            node.QueueFree();
        }
        _tileNodes.Clear();
    }

    /// <summary>
    /// Sets the visibility of this layer.
    /// </summary>
    public void SetLayerVisible(bool visible)
    {
        Visible = visible;
    }

    /// <summary>
    /// Sets the opacity of this layer (0.0 to 1.0).
    /// </summary>
    public void SetLayerOpacity(float opacity)
    {
        Modulate = new Color(1, 1, 1, Mathf.Clamp(opacity, 0f, 1f));
    }

    /// <summary>
    /// Creates a visual node for a tile (either textured or colored).
    /// </summary>
    private Node2D? CreateTileVisual(HexCoord coord, TileType tileType)
    {
        // Try to get texture first
        var texture = _registry.GetTexture(tileType.Id);

        if (texture != null)
        {
            return CreateTexturedHex(coord, texture, tileType.PixelSize);
        }
        else
        {
            return CreateColoredHex(coord, tileType.PreviewColor);
        }
    }

    /// <summary>
    /// Creates a colored hex polygon at the specified coordinate.
    /// </summary>
    private Polygon2D CreateColoredHex(HexCoord coord, Color color)
    {
        var polygon = new Polygon2D();
        polygon.Position = _layout.HexToPixel(coord);
        polygon.Color = color;

        // Get hex corners relative to center (0,0)
        var corners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            corners[i] = _layout.HexCorner(new HexCoord(0, 0), i);
        }
        polygon.Polygon = corners;

        return polygon;
    }

    /// <summary>
    /// Creates a textured hex sprite at the specified coordinate.
    /// </summary>
    /// <param name="coord">The hex coordinate to place the sprite.</param>
    /// <param name="texture">The texture to display.</param>
    /// <param name="pixelSize">The width of the hex in the texture (in pixels).</param>
    private Sprite2D CreateTexturedHex(HexCoord coord, Texture2D texture, int pixelSize = 64)
    {
        var sprite = new Sprite2D();
        sprite.Position = _layout.HexToPixel(coord);
        sprite.Texture = texture;

        var rotation = _grid.GetTileRotation(coord, _layerIndex);
        sprite.Rotation = rotation;

        // Scale based on the hex width in the texture vs the grid's hex width
        // pixelSize is how wide the hex is in the source texture
        // This ensures the hex in the texture matches the hex size on screen
        float scale = _layout.HexWidth / pixelSize;
        sprite.Scale = new Vector2(scale, scale);

        return sprite;
    }

    /// <summary>
    /// Gets the number of rendered tiles in this layer.
    /// </summary>
    public int RenderedTileCount => _tileNodes.Count;

    /// <summary>
    /// Checks if a tile is rendered at the specified coordinate.
    /// </summary>
    public bool HasRenderedTile(HexCoord coord)
    {
        return _tileNodes.ContainsKey(coord);
    }

    /// <summary>
    /// Updates the layout reference (used when orientation changes).
    /// </summary>
    public void UpdateLayout(HexLayout layout)
    {
        _layout = layout;
    }
}
