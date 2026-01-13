using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;
using Hexographer.Rendering;

namespace Hexographer.Core.Serialization;

/// <summary>
/// Handles serialization and deserialization of hex maps to/from JSON.
/// </summary>
public static class MapSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new ColorJsonConverter() }
    };

    /// <summary>
    /// Saves a hex grid to a JSON file.
    /// </summary>
    /// <param name="grid">The grid to save.</param>
    /// <param name="filePath">The path to save to.</param>
    /// <param name="metadata">Optional metadata to include.</param>
    public static void Save(HexGrid grid, string filePath, MapMetadata? metadata = null)
    {
        var json = Serialize(grid, metadata);

        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            var error = FileAccess.GetOpenError();
            throw new InvalidOperationException($"Failed to open file for writing: {filePath} (Error: {error})");
        }

        file.StoreString(json);
        GD.Print($"Map saved to: {filePath}");
    }

    /// <summary>
    /// Serializes a hex grid to a JSON string.
    /// </summary>
    /// <param name="grid">The grid to serialize.</param>
    /// <param name="metadata">Optional metadata to include.</param>
    /// <returns>JSON string representation of the map.</returns>
    public static string Serialize(HexGrid grid, MapMetadata? metadata = null)
    {
        var mapFile = new MapFile
        {
            Version = "1.0",
            Metadata = metadata ?? new MapMetadata
            {
                Name = "Untitled Map",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            },
            Settings = new GridSettings
            {
                Orientation = grid.Orientation.ToString(),
                HexSize = grid.HexSize,
                LayerCount = grid.LayerCount
            },
            Tiles = SerializeTiles(grid)
        };

        // Update modified timestamp
        if (mapFile.Metadata != null)
        {
            mapFile.Metadata.Modified = DateTime.UtcNow;
        }

        return JsonSerializer.Serialize(mapFile, SerializerOptions);
    }

    /// <summary>
    /// Loads a hex grid from a JSON file.
    /// </summary>
    /// <param name="filePath">The path to load from (supports Godot paths like user://).</param>
    /// <param name="metadata">Output parameter for the loaded metadata.</param>
    /// <returns>The loaded hex grid.</returns>
    public static HexGrid Load(string filePath, out MapMetadata? metadata)
    {
        using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            var error = FileAccess.GetOpenError();
            throw new InvalidOperationException($"Failed to open file for reading: {filePath} (Error: {error})");
        }

        var json = file.GetAsText();
        var grid = Deserialize(json, out metadata);
        GD.Print($"Map loaded from: {filePath}");
        return grid;
    }

    /// <summary>
    /// Deserializes a hex grid from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="metadata">Output parameter for the loaded metadata.</param>
    /// <returns>The deserialized hex grid.</returns>
    public static HexGrid Deserialize(string json, out MapMetadata? metadata)
    {
        MapFile? mapFile;
        try
        {
            mapFile = JsonSerializer.Deserialize<MapFile>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to parse map file JSON: {ex.Message}", ex);
        }

        if (mapFile == null)
        {
            throw new InvalidOperationException("Failed to deserialize map file: result was null.");
        }

        // Parse orientation (case-insensitive, supports snake_case)
        var orientation = ParseOrientation(mapFile.Settings.Orientation);

        var grid = new HexGrid(orientation, mapFile.Settings.HexSize, mapFile.Settings.LayerCount);

        // Deserialize tiles
        foreach (var tileData in mapFile.Tiles)
        {
            var coord = new HexCoord(tileData.Q, tileData.R);
            var tile = grid.GetOrCreateTile(coord);

            // Set layers
            for (int i = 0; i < tileData.Layers.Length && i < grid.LayerCount; i++)
            {
                tile.SetLayer(i, tileData.Layers[i]);
            }

            // Set elevation
            tile.Elevation = tileData.Elevation;

            // Set metadata if present
            if (tileData.Metadata != null)
            {
                foreach (var (key, value) in tileData.Metadata)
                {
                    tile.SetMetadata(key, value);
                }
            }
        }

        grid.RecalculateBounds();
        metadata = mapFile.Metadata;
        return grid;
    }

    /// <summary>
    /// Exports tile type definitions to JSON.
    /// </summary>
    /// <param name="registry">The tile registry to export from.</param>
    /// <returns>JSON string of tile type definitions.</returns>
    public static string ExportTileTypes(TileRegistry registry)
    {
        var tileTypes = new List<TileTypeData>();

        foreach (var tileType in registry.GetAllTileTypes())
        {
            tileTypes.Add(new TileTypeData
            {
                Id = tileType.Id,
                DisplayName = tileType.DisplayName,
                LayerIndex = tileType.LayerIndex,
                Category = tileType.Category,
                SortOrder = tileType.SortOrder,
                PreviewColor = ColorJsonConverter.ToHexString(tileType.PreviewColor),
                TexturePath = tileType.TexturePath,
                BlocksMovement = tileType.BlocksMovement,
                BlocksVision = tileType.BlocksVision,
                MovementCost = tileType.MovementCost
            });
        }

        return JsonSerializer.Serialize(tileTypes, SerializerOptions);
    }

    /// <summary>
    /// Imports tile type definitions from JSON into a registry.
    /// </summary>
    /// <param name="json">The JSON string to import.</param>
    /// <param name="registry">The registry to import into.</param>
    public static void ImportTileTypes(string json, TileRegistry registry)
    {
        var tileTypes = JsonSerializer.Deserialize<List<TileTypeData>>(json, SerializerOptions);

        if (tileTypes == null)
            return;

        foreach (var data in tileTypes)
        {
            var tileType = new TileType
            {
                Id = data.Id,
                DisplayName = data.DisplayName,
                LayerIndex = data.LayerIndex,
                Category = data.Category,
                SortOrder = data.SortOrder,
                PreviewColor = ColorJsonConverter.ParseHexColor(data.PreviewColor),
                TexturePath = data.TexturePath ?? string.Empty,
                BlocksMovement = data.BlocksMovement,
                BlocksVision = data.BlocksVision,
                MovementCost = data.MovementCost
            };

            registry.RegisterTileType(tileType);
        }
    }

    /// <summary>
    /// Converts grid tiles to serializable tile data.
    /// </summary>
    private static List<TileData> SerializeTiles(HexGrid grid)
    {
        var tiles = new List<TileData>();

        foreach (var tile in grid.GetAllTiles())
        {
            // Skip tiles with no content
            if (!tile.HasAnyContent())
                continue;

            var tileData = new TileData
            {
                Q = tile.Coord.Q,
                R = tile.Coord.R,
                Layers = tile.Layers.ToArray(),
                Elevation = tile.Elevation
            };

            // Only include metadata if non-empty
            if (tile.Metadata.Count > 0)
            {
                tileData.Metadata = new Dictionary<string, object>(tile.Metadata);
            }

            tiles.Add(tileData);
        }

        // Sort by coordinates for consistent output
        return tiles.OrderBy(t => t.R).ThenBy(t => t.Q).ToList();
    }

    /// <summary>
    /// Parses orientation string to HexOrientation enum.
    /// Supports multiple formats: "FlatTop", "flat_top", "flattop" (case-insensitive).
    /// </summary>
    private static HexOrientation ParseOrientation(string? orientation)
    {
        if (string.IsNullOrEmpty(orientation))
            return HexOrientation.PointyTop;

        // Normalize: lowercase and remove underscores
        var normalized = orientation.ToLowerInvariant().Replace("_", "");

        return normalized switch
        {
            "flattop" => HexOrientation.FlatTop,
            "pointytop" => HexOrientation.PointyTop,
            _ => HexOrientation.PointyTop
        };
    }
}
