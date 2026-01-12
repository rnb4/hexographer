using System;
using System.Collections.Generic;
using Godot;

namespace Hexographer.Core.Data;

/// <summary>
/// Defines a type of tile that can be placed on the hex grid.
/// Each tile type belongs to a specific layer and has associated visual properties.
/// </summary>
[Serializable]
public class TileType
{
    /// <summary>
    /// Unique identifier for this tile type (e.g., "grass", "water_deep", "forest").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for display in UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The layer index this tile type belongs to.
    /// 0 = Ground, 1 = Features, 2 = Objects
    /// </summary>
    public int LayerIndex { get; set; }

    /// <summary>
    /// Preview color used for minimap or simple rendering when textures are unavailable.
    /// </summary>
    public Color PreviewColor { get; set; } = Colors.Magenta;

    /// <summary>
    /// Path to the texture resource (relative to Assets/Textures/).
    /// </summary>
    public string TexturePath { get; set; } = string.Empty;

    /// <summary>
    /// Optional texture region for sprite atlas usage.
    /// </summary>
    public Rect2? AtlasRegion { get; set; }

    /// <summary>
    /// Category for organizing tiles in the palette (e.g., "Terrain", "Water", "Vegetation").
    /// </summary>
    public string Category { get; set; } = "Uncategorized";

    /// <summary>
    /// Sort order within the category for palette display.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Custom properties for game-specific data (e.g., movement cost, elevation modifier).
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Whether this tile blocks movement (for pathfinding).
    /// </summary>
    public bool BlocksMovement { get; set; }

    /// <summary>
    /// Whether this tile blocks line of sight.
    /// </summary>
    public bool BlocksVision { get; set; }

    /// <summary>
    /// Movement cost modifier for pathfinding (1.0 = normal).
    /// </summary>
    public float MovementCost { get; set; } = 1.0f;

    public TileType() { }

    public TileType(string id, string displayName, int layerIndex)
    {
        Id = id;
        DisplayName = displayName;
        LayerIndex = layerIndex;
    }

    /// <summary>
    /// Gets a property value with type conversion, or default if not found.
    /// </summary>
    public T GetProperty<T>(string key, T defaultValue = default!)
    {
        if (Properties.TryGetValue(key, out var value))
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
    /// Sets a custom property value.
    /// </summary>
    public void SetProperty<T>(string key, T value)
    {
        Properties[key] = value!;
    }

    public override string ToString() => $"TileType({Id}, Layer={LayerIndex})";
}

/// <summary>
/// Defines the standard layer indices used in the editor.
/// </summary>
public static class TileLayers
{
    public const int Ground = 0;
    public const int Features = 1;
    public const int Objects = 2;

    public const int LayerCount = 3;

    public static readonly string[] LayerNames = { "Ground", "Features", "Objects" };

    public static string GetLayerName(int index)
    {
        return index >= 0 && index < LayerNames.Length ? LayerNames[index] : $"Layer {index}";
    }
}
