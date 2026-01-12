using System;
using System.Collections.Generic;
using System.Linq;
using Hexographer.Core.Hex;

namespace Hexographer.Core.Data;

/// <summary>
/// Manages a sparse hex grid using dictionary storage.
/// Supports infinite maps with efficient memory usage for scattered tiles.
/// </summary>
public class HexGrid
{
    private readonly Dictionary<HexCoord, HexTile> _tiles = new();

    /// <summary>
    /// The hex orientation for this grid.
    /// </summary>
    public HexOrientation Orientation { get; set; } = HexOrientation.PointyTop;

    /// <summary>
    /// The size of each hex (distance from center to corner).
    /// </summary>
    public float HexSize { get; set; } = 64f;

    /// <summary>
    /// Number of layers per tile.
    /// </summary>
    public int LayerCount { get; set; } = TileLayers.LayerCount;

    /// <summary>
    /// Cached minimum bounds coordinate. Null if grid is empty.
    /// </summary>
    public HexCoord? BoundsMin { get; private set; }

    /// <summary>
    /// Cached maximum bounds coordinate. Null if grid is empty.
    /// </summary>
    public HexCoord? BoundsMax { get; private set; }

    /// <summary>
    /// Event fired when a tile is added, modified, or removed.
    /// </summary>
    public event Action<HexCoord>? TileChanged;

    /// <summary>
    /// Event fired when multiple tiles change at once.
    /// </summary>
    public event Action<IEnumerable<HexCoord>>? TilesChanged;

    /// <summary>
    /// Event fired when the grid is cleared.
    /// </summary>
    public event Action? GridCleared;

    /// <summary>
    /// Gets the number of tiles in the grid.
    /// </summary>
    public int TileCount => _tiles.Count;

    /// <summary>
    /// Gets whether the grid is empty.
    /// </summary>
    public bool IsEmpty => _tiles.Count == 0;

    public HexGrid() { }

    public HexGrid(HexOrientation orientation, float hexSize = 64f, int layerCount = 3)
    {
        Orientation = orientation;
        HexSize = hexSize;
        LayerCount = layerCount;
    }

    /// <summary>
    /// Gets the tile at the specified coordinate, or null if none exists.
    /// </summary>
    public HexTile? GetTile(HexCoord coord)
    {
        return _tiles.GetValueOrDefault(coord);
    }

    /// <summary>
    /// Gets the tile at the specified coordinate, creating one if it doesn't exist.
    /// </summary>
    public HexTile GetOrCreateTile(HexCoord coord)
    {
        if (!_tiles.TryGetValue(coord, out var tile))
        {
            tile = new HexTile(coord, LayerCount);
            _tiles[coord] = tile;
            UpdateBoundsForCoord(coord);
        }
        return tile;
    }

    /// <summary>
    /// Sets the tile at the specified coordinate.
    /// </summary>
    public void SetTile(HexCoord coord, HexTile tile)
    {
        tile.Coord = coord;
        _tiles[coord] = tile;
        UpdateBoundsForCoord(coord);
        TileChanged?.Invoke(coord);
    }

    /// <summary>
    /// Removes the tile at the specified coordinate.
    /// </summary>
    public bool RemoveTile(HexCoord coord)
    {
        if (_tiles.Remove(coord))
        {
            InvalidateBounds();
            TileChanged?.Invoke(coord);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a tile exists at the specified coordinate.
    /// </summary>
    public bool HasTile(HexCoord coord)
    {
        return _tiles.ContainsKey(coord);
    }

    /// <summary>
    /// Sets the tile type on a specific layer at the given coordinate.
    /// Creates the tile if it doesn't exist.
    /// </summary>
    public void SetTileLayer(HexCoord coord, int layerIndex, string? tileTypeId)
    {
        var tile = GetOrCreateTile(coord);
        tile.SetLayer(layerIndex, tileTypeId);
        TileChanged?.Invoke(coord);
    }

    /// <summary>
    /// Gets the tile type ID at a specific layer and coordinate.
    /// </summary>
    public string? GetTileLayer(HexCoord coord, int layerIndex)
    {
        return GetTile(coord)?.GetLayer(layerIndex);
    }

    /// <summary>
    /// Clears a specific layer at the given coordinate.
    /// Removes the tile entirely if all layers become empty.
    /// </summary>
    public void ClearTileLayer(HexCoord coord, int layerIndex)
    {
        var tile = GetTile(coord);
        if (tile == null) return;

        tile.ClearLayer(layerIndex);

        if (!tile.HasAnyContent())
        {
            RemoveTile(coord);
        }
        else
        {
            TileChanged?.Invoke(coord);
        }
    }

    /// <summary>
    /// Returns all tiles in the grid.
    /// </summary>
    public IEnumerable<HexTile> GetAllTiles()
    {
        return _tiles.Values;
    }

    /// <summary>
    /// Returns all tile coordinates in the grid.
    /// </summary>
    public IEnumerable<HexCoord> GetAllCoords()
    {
        return _tiles.Keys;
    }

    /// <summary>
    /// Returns tiles within the specified range of a center coordinate.
    /// </summary>
    public IEnumerable<HexTile> GetTilesInRange(HexCoord center, int radius)
    {
        foreach (var coord in center.GetHexesInRange(radius))
        {
            if (_tiles.TryGetValue(coord, out var tile))
            {
                yield return tile;
            }
        }
    }

    /// <summary>
    /// Returns tiles that have content on the specified layer.
    /// </summary>
    public IEnumerable<HexTile> GetTilesWithLayer(int layerIndex)
    {
        return _tiles.Values.Where(t => t.HasContent(layerIndex));
    }

    /// <summary>
    /// Returns tiles that have a specific tile type.
    /// </summary>
    public IEnumerable<HexTile> GetTilesOfType(string tileTypeId)
    {
        return _tiles.Values.Where(t => t.Layers.Contains(tileTypeId));
    }

    /// <summary>
    /// Clears all tiles from the grid.
    /// </summary>
    public void Clear()
    {
        _tiles.Clear();
        BoundsMin = null;
        BoundsMax = null;
        GridCleared?.Invoke();
    }

    /// <summary>
    /// Fills a region with a specific tile type on the given layer.
    /// </summary>
    public void FillRegion(IEnumerable<HexCoord> coords, int layerIndex, string? tileTypeId)
    {
        var coordList = coords.ToList();

        foreach (var coord in coordList)
        {
            var tile = GetOrCreateTile(coord);
            tile.SetLayer(layerIndex, tileTypeId);
        }

        TilesChanged?.Invoke(coordList);
    }

    /// <summary>
    /// Fills a rectangular region with the specified tile type.
    /// </summary>
    public void FillRectangle(HexCoord corner1, HexCoord corner2, int layerIndex, string? tileTypeId)
    {
        FillRegion(HexMath.Rectangle(corner1, corner2), layerIndex, tileTypeId);
    }

    /// <summary>
    /// Fills a hexagonal region (circle) with the specified tile type.
    /// </summary>
    public void FillHexagon(HexCoord center, int radius, int layerIndex, string? tileTypeId)
    {
        FillRegion(center.GetHexesInRange(radius), layerIndex, tileTypeId);
    }

    /// <summary>
    /// Performs flood fill starting from a coordinate, replacing tiles of the same type.
    /// </summary>
    public IEnumerable<HexCoord> FloodFill(HexCoord start, int layerIndex, string? newTileTypeId, int maxDistance = 1000)
    {
        var startType = GetTileLayer(start, layerIndex);

        // Don't fill if the new type is the same as existing
        if (startType == newTileTypeId)
            return Enumerable.Empty<HexCoord>();

        var filled = HexMath.FloodFill(
            start,
            coord => GetTileLayer(coord, layerIndex) == startType,
            maxDistance
        ).ToList();

        foreach (var coord in filled)
        {
            var tile = GetOrCreateTile(coord);
            tile.SetLayer(layerIndex, newTileTypeId);
        }

        TilesChanged?.Invoke(filled);
        return filled;
    }

    /// <summary>
    /// Creates a deep copy of this grid.
    /// </summary>
    public HexGrid Clone()
    {
        var clone = new HexGrid(Orientation, HexSize, LayerCount);

        foreach (var (coord, tile) in _tiles)
        {
            clone._tiles[coord] = tile.Clone();
        }

        clone.RecalculateBounds();
        return clone;
    }

    /// <summary>
    /// Recalculates the bounding box of all tiles.
    /// </summary>
    public void RecalculateBounds()
    {
        if (_tiles.Count == 0)
        {
            BoundsMin = null;
            BoundsMax = null;
            return;
        }

        int minQ = int.MaxValue, maxQ = int.MinValue;
        int minR = int.MaxValue, maxR = int.MinValue;

        foreach (var coord in _tiles.Keys)
        {
            minQ = Math.Min(minQ, coord.Q);
            maxQ = Math.Max(maxQ, coord.Q);
            minR = Math.Min(minR, coord.R);
            maxR = Math.Max(maxR, coord.R);
        }

        BoundsMin = new HexCoord(minQ, minR);
        BoundsMax = new HexCoord(maxQ, maxR);
    }

    private void UpdateBoundsForCoord(HexCoord coord)
    {
        if (BoundsMin == null || BoundsMax == null)
        {
            BoundsMin = coord;
            BoundsMax = coord;
            return;
        }

        var min = BoundsMin.Value;
        var max = BoundsMax.Value;

        BoundsMin = new HexCoord(
            Math.Min(min.Q, coord.Q),
            Math.Min(min.R, coord.R)
        );

        BoundsMax = new HexCoord(
            Math.Max(max.Q, coord.Q),
            Math.Max(max.R, coord.R)
        );
    }

    private void InvalidateBounds()
    {
        // Lazy recalculation - will be recalculated on next access if needed
        if (_tiles.Count == 0)
        {
            BoundsMin = null;
            BoundsMax = null;
        }
        else
        {
            RecalculateBounds();
        }
    }

    /// <summary>
    /// Gets the approximate size of the grid in hex coordinates.
    /// </summary>
    public (int Width, int Height) GetSize()
    {
        if (BoundsMin == null || BoundsMax == null)
            return (0, 0);

        return (
            BoundsMax.Value.Q - BoundsMin.Value.Q + 1,
            BoundsMax.Value.R - BoundsMin.Value.R + 1
        );
    }

    /// <summary>
    /// Checks if a coordinate is within the current bounds.
    /// </summary>
    public bool IsInBounds(HexCoord coord)
    {
        if (BoundsMin == null || BoundsMax == null)
            return false;

        return coord.Q >= BoundsMin.Value.Q && coord.Q <= BoundsMax.Value.Q &&
               coord.R >= BoundsMin.Value.R && coord.R <= BoundsMax.Value.R;
    }

    /// <summary>
    /// Returns all coordinates within the bounding hexagonal region of existing tiles.
    /// Uses all three cube coordinates (Q, R, S) to create a proper hexagonal bound,
    /// avoiding the parallelogram that results from only using Q and R bounds.
    /// </summary>
    public IEnumerable<HexCoord> GetBoundingRegion()
    {
        if (_tiles.Count == 0)
            yield break;

        int minQ = int.MaxValue, maxQ = int.MinValue;
        int minR = int.MaxValue, maxR = int.MinValue;
        int minS = int.MaxValue, maxS = int.MinValue;

        foreach (var coord in _tiles.Keys)
        {
            minQ = Math.Min(minQ, coord.Q);
            maxQ = Math.Max(maxQ, coord.Q);
            minR = Math.Min(minR, coord.R);
            maxR = Math.Max(maxR, coord.R);
            minS = Math.Min(minS, coord.S);
            maxS = Math.Max(maxS, coord.S);
        }

        for (int q = minQ; q <= maxQ; q++)
        {
            for (int r = minR; r <= maxR; r++)
            {
                int s = -q - r;
                if (s >= minS && s <= maxS)
                {
                    yield return new HexCoord(q, r);
                }
            }
        }
    }

    /// <summary>
    /// Returns coordinates within the bounding region that do not have tiles.
    /// Useful for drawing empty hex outlines in the grid overlay.
    /// </summary>
    public IEnumerable<HexCoord> GetEmptyCoordsInBoundingRegion()
    {
        foreach (var coord in GetBoundingRegion())
        {
            if (!_tiles.ContainsKey(coord))
            {
                yield return coord;
            }
        }
    }
}
