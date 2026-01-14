using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Hexographer.Core.Serialization;

/// <summary>
/// Root object for the map file JSON structure.
/// </summary>
public class MapFile
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("metadata")]
    public MapMetadata? Metadata { get; set; }

    [JsonPropertyName("settings")]
    public GridSettings Settings { get; set; } = new();

    [JsonPropertyName("tiles")]
    public List<TileData> Tiles { get; set; } = new();

    [JsonPropertyName("customTileTypes")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TileTypeData>? CustomTileTypes { get; set; }
}

/// <summary>
/// Map metadata for author, description, and timestamps.
/// </summary>
public class MapMetadata
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Untitled Map";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("created")]
    public DateTime Created { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("modified")]
    public DateTime Modified { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Grid configuration settings.
/// </summary>
public class GridSettings
{
    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "PointyTop";

    [JsonPropertyName("hexSize")]
    public float HexSize { get; set; } = 64f;

    [JsonPropertyName("layerCount")]
    public int LayerCount { get; set; } = 3;
}

/// <summary>
/// Serialized representation of a single tile.
/// </summary>
public class TileData
{
    [JsonPropertyName("q")]
    public int Q { get; set; }

    [JsonPropertyName("r")]
    public int R { get; set; }

    [JsonPropertyName("layers")]
    public string?[] Layers { get; set; } = Array.Empty<string?>();

    [JsonPropertyName("elevation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Elevation { get; set; }

    [JsonPropertyName("metadata")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Serialized representation of a tile type definition.
/// </summary>
public class TileTypeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; set; } = "";

    [JsonPropertyName("layerIndex")]
    public int LayerIndex { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("sortOrder")]
    public int SortOrder { get; set; }

    [JsonPropertyName("previewColor")]
    public string PreviewColor { get; set; } = "#FFFFFFFF";

    [JsonPropertyName("texturePath")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TexturePath { get; set; }

    [JsonPropertyName("pixelSize")]
    public int PixelSize { get; set; } = 64;

    [JsonPropertyName("blocksMovement")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool BlocksMovement { get; set; }

    [JsonPropertyName("blocksVision")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool BlocksVision { get; set; }

    [JsonPropertyName("movementCost")]
    public float MovementCost { get; set; } = 1.0f;
}
