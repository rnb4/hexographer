using System;
using System.Collections.Generic;
using Hexographer.Core.Hex;

namespace Hexographer.Core.Data;

/// <summary>
/// Represents a single hex tile on the map with multiple layers and metadata.
/// </summary>
[Serializable]
public class HexTile
{
    /// <summary>
    /// The coordinate of this tile on the hex grid.
    /// </summary>
    public HexCoord Coord { get; set; }

    /// <summary>
    /// Tile type IDs for each layer. Null means empty/transparent for that layer.
    /// Index 0 = Ground, 1 = Features, 2 = Objects
    /// </summary>
    public string?[] Layers { get; set; }

    /// <summary>
    /// Elevation value for terrain height visualization.
    /// </summary>
    public int Elevation { get; set; }
    
    /// <summary>
    /// Rotation value in radians for each layer.
    /// </summary>
    public float[] Rotations { get; set; }

    /// <summary>
    /// Custom metadata for game-specific data.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a new hex tile at the specified coordinate.
    /// </summary>
    public HexTile(HexCoord coord, int layerCount = TileLayers.LayerCount)
    {
        Coord = coord;
        Layers = new string?[layerCount];
        Rotations = new float[layerCount];
    }

    /// <summary>
    /// Creates a new hex tile with default layer count.
    /// </summary>
    public HexTile() : this(HexCoord.Zero) { }

    /// <summary>
    /// Gets the tile type ID at the specified layer.
    /// </summary>
    public string? GetLayer(int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= Layers.Length)
            return null;
        return Layers[layerIndex];
    }

    /// <summary>
    /// Sets the tile type ID at the specified layer.
    /// </summary>
    public void SetLayer(int layerIndex, string? tileTypeId)
    {
        if (layerIndex >= 0 && layerIndex < Layers.Length)
        {
            Layers[layerIndex] = tileTypeId;
        }
    }

    /// <summary>
    /// Clears the specified layer (sets to null).
    /// </summary>
    public void ClearLayer(int layerIndex)
    {
        SetLayer(layerIndex, null);
    }

    /// <summary>
    /// Clears all layers on this tile.
    /// </summary>
    public void ClearAllLayers()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            Layers[i] = null;
        }
    }

    /// <summary>
    /// Checks if this tile has any content on any layer.
    /// </summary>
    public bool HasAnyContent()
    {
        foreach (var layer in Layers)
        {
            if (!string.IsNullOrEmpty(layer))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a specific layer has content.
    /// </summary>
    public bool HasContent(int layerIndex)
    {
        return !string.IsNullOrEmpty(GetLayer(layerIndex));
    }

    /// <summary>
    /// Gets the number of layers with content.
    /// </summary>
    public int GetFilledLayerCount()
    {
        int count = 0;
        foreach (var layer in Layers)
        {
            if (!string.IsNullOrEmpty(layer))
                count++;
        }
        return count;
    }

    public void Rotate(int layerIndex, float degrees)
    {
        if (layerIndex < 0 || layerIndex >= Rotations.Length)
            return;
        Rotations[layerIndex] += float.DegreesToRadians(degrees);
    }

    /// <summary>
    /// Creates a deep copy of this tile.
    /// </summary>
    public HexTile Clone()
    {
        var clone = new HexTile(Coord, Layers.Length)
        {
            Elevation = Elevation,
            Metadata = new Dictionary<string, object>(Metadata)
        };

        for (int i = 0; i < Layers.Length; i++)
        {
            clone.Layers[i] = Layers[i];
        }

        return clone;
    }

    /// <summary>
    /// Gets a metadata value with type conversion, or default if not found.
    /// </summary>
    public T GetMetadata<T>(string key, T defaultValue = default!)
    {
        if (Metadata.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Sets a metadata value.
    /// </summary>
    public void SetMetadata<T>(string key, T value)
    {
        Metadata[key] = value!;
    }

    /// <summary>
    /// Removes a metadata entry.
    /// </summary>
    public bool RemoveMetadata(string key)
    {
        return Metadata.Remove(key);
    }

    public override string ToString()
    {
        var layerInfo = string.Join(", ", Layers);
        return $"HexTile({Coord}, Layers=[{layerInfo}], Elevation={Elevation})";
    }
}
