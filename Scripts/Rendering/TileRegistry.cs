using System.Collections.Generic;
using System.Linq;
using Godot;
using Hexographer.Core.Data;

namespace Hexographer.Rendering;

/// <summary>
/// Central repository for tile type definitions and texture caching.
/// </summary>
public class TileRegistry
{
    private readonly Dictionary<string, TileType> _tileTypes = new();
    private readonly Dictionary<string, Texture2D?> _textureCache = new();

    /// <summary>
    /// Registers a tile type in the registry.
    /// </summary>
    public void RegisterTileType(TileType type)
    {
        _tileTypes[type.Id] = type;
    }

    /// <summary>
    /// Unregisters a tile type from the registry.
    /// </summary>
    public bool UnregisterTileType(string id)
    {
        _textureCache.Remove(id);
        return _tileTypes.Remove(id);
    }

    /// <summary>
    /// Gets a tile type by ID, or null if not found.
    /// </summary>
    public TileType? GetTileType(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return null;
        return _tileTypes.GetValueOrDefault(id);
    }

    /// <summary>
    /// Checks if a tile type is registered.
    /// </summary>
    public bool HasTileType(string id)
    {
        return _tileTypes.ContainsKey(id);
    }

    /// <summary>
    /// Gets the texture for a tile type, loading and caching it if needed.
    /// Returns null if no texture path is set or loading fails.
    /// </summary>
    public Texture2D? GetTexture(string? tileTypeId)
    {
        if (string.IsNullOrEmpty(tileTypeId))
            return null;

        // Check cache first
        if (_textureCache.TryGetValue(tileTypeId, out var cached))
            return cached;

        // Get tile type
        var tileType = GetTileType(tileTypeId);
        if (tileType == null || string.IsNullOrEmpty(tileType.TexturePath))
        {
            _textureCache[tileTypeId] = null;
            return null;
        }

        // Try to load texture
        var texture = GD.Load<Texture2D>(tileType.TexturePath);
        _textureCache[tileTypeId] = texture;
        return texture;
    }

    /// <summary>
    /// Gets all tile types for a specific layer.
    /// </summary>
    public IEnumerable<TileType> GetTileTypesForLayer(int layerIndex)
    {
        return _tileTypes.Values
            .Where(t => t.LayerIndex == layerIndex)
            .OrderBy(t => t.Category)
            .ThenBy(t => t.SortOrder)
            .ThenBy(t => t.DisplayName);
    }

    /// <summary>
    /// Gets all registered tile types.
    /// </summary>
    public IEnumerable<TileType> GetAllTileTypes()
    {
        return _tileTypes.Values
            .OrderBy(t => t.LayerIndex)
            .ThenBy(t => t.Category)
            .ThenBy(t => t.SortOrder);
    }

    /// <summary>
    /// Gets all unique categories across all tile types.
    /// </summary>
    public IEnumerable<string> GetCategories()
    {
        return _tileTypes.Values
            .Select(t => t.Category)
            .Distinct()
            .OrderBy(c => c);
    }

    /// <summary>
    /// Gets tile types by category.
    /// </summary>
    public IEnumerable<TileType> GetTileTypesByCategory(string category)
    {
        return _tileTypes.Values
            .Where(t => t.Category == category)
            .OrderBy(t => t.LayerIndex)
            .ThenBy(t => t.SortOrder);
    }

    /// <summary>
    /// Clears the texture cache, forcing textures to be reloaded.
    /// </summary>
    public void ClearTextureCache()
    {
        _textureCache.Clear();
    }

    /// <summary>
    /// Clears all tile types and cached textures.
    /// </summary>
    public void Clear()
    {
        _tileTypes.Clear();
        _textureCache.Clear();
    }

    /// <summary>
    /// Gets the number of registered tile types.
    /// </summary>
    public int Count => _tileTypes.Count;

    /// <summary>
    /// Registers a set of default tile types for testing without texture assets.
    /// All tiles use PreviewColor for colored hex rendering.
    /// </summary>
    public void RegisterDefaultTiles()
    {
        // Ground layer (0)
        RegisterTileType(new TileType("grass", "Grass", TileLayers.Ground)
        {
            PreviewColor = new Color(0.29f, 0.49f, 0.14f), // #4a7c23
            Category = "Terrain",
            SortOrder = 0,
            MovementCost = 1.0f
        });

        RegisterTileType(new TileType("grass_tall", "Tall Grass", TileLayers.Ground)
        {
            PreviewColor = new Color(0.35f, 0.55f, 0.18f),
            Category = "Terrain",
            SortOrder = 1,
            MovementCost = 1.2f
        });

        RegisterTileType(new TileType("dirt", "Dirt", TileLayers.Ground)
        {
            PreviewColor = new Color(0.55f, 0.39f, 0.24f), // #8c6440
            Category = "Terrain",
            SortOrder = 2,
            MovementCost = 1.0f
        });

        RegisterTileType(new TileType("sand", "Sand", TileLayers.Ground)
        {
            PreviewColor = new Color(0.85f, 0.78f, 0.55f), // #d9c88c
            Category = "Terrain",
            SortOrder = 3,
            MovementCost = 1.3f
        });

        RegisterTileType(new TileType("stone", "Stone", TileLayers.Ground)
        {
            PreviewColor = new Color(0.5f, 0.5f, 0.5f), // #808080
            Category = "Terrain",
            SortOrder = 4,
            MovementCost = 1.0f
        });

        RegisterTileType(new TileType("snow", "Snow", TileLayers.Ground)
        {
            PreviewColor = new Color(0.95f, 0.95f, 0.98f),
            Category = "Terrain",
            SortOrder = 5,
            MovementCost = 1.4f
        });

        RegisterTileType(new TileType("water_shallow", "Shallow Water", TileLayers.Ground)
        {
            PreviewColor = new Color(0.35f, 0.65f, 0.85f), // #59a6d9
            Category = "Water",
            SortOrder = 0,
            MovementCost = 2.0f
        });

        RegisterTileType(new TileType("water", "Water", TileLayers.Ground)
        {
            PreviewColor = new Color(0.18f, 0.43f, 0.72f), // #2e6eb8
            Category = "Water",
            SortOrder = 1,
            BlocksMovement = true
        });

        RegisterTileType(new TileType("water_deep", "Deep Water", TileLayers.Ground)
        {
            PreviewColor = new Color(0.1f, 0.25f, 0.5f), // #1a4080
            Category = "Water",
            SortOrder = 2,
            BlocksMovement = true
        });

        // Features layer (1)
        RegisterTileType(new TileType("forest", "Forest", TileLayers.Features)
        {
            PreviewColor = new Color(0.1f, 0.3f, 0.1f), // #1a4d1a
            Category = "Vegetation",
            SortOrder = 0,
            MovementCost = 1.5f,
            BlocksVision = true
        });

        RegisterTileType(new TileType("forest_dense", "Dense Forest", TileLayers.Features)
        {
            PreviewColor = new Color(0.05f, 0.2f, 0.05f),
            Category = "Vegetation",
            SortOrder = 1,
            MovementCost = 2.0f,
            BlocksVision = true
        });

        RegisterTileType(new TileType("hills", "Hills", TileLayers.Features)
        {
            PreviewColor = new Color(0.6f, 0.5f, 0.35f),
            Category = "Elevation",
            SortOrder = 0,
            MovementCost = 1.5f
        });

        RegisterTileType(new TileType("mountain", "Mountain", TileLayers.Features)
        {
            PreviewColor = new Color(0.4f, 0.35f, 0.3f), // #665950
            Category = "Elevation",
            SortOrder = 1,
            BlocksMovement = true,
            BlocksVision = true
        });

        RegisterTileType(new TileType("road", "Road", TileLayers.Features)
        {
            PreviewColor = new Color(0.45f, 0.4f, 0.35f), // #73665a
            Category = "Infrastructure",
            SortOrder = 0,
            MovementCost = 0.5f
        });

        RegisterTileType(new TileType("bridge", "Bridge", TileLayers.Features)
        {
            PreviewColor = new Color(0.55f, 0.45f, 0.3f),
            Category = "Infrastructure",
            SortOrder = 1,
            MovementCost = 0.5f
        });

        RegisterTileType(new TileType("river", "River", TileLayers.Features)
        {
            PreviewColor = new Color(0.25f, 0.55f, 0.75f),
            Category = "Water",
            SortOrder = 0,
            BlocksMovement = true
        });

        // Objects layer (2)
        RegisterTileType(new TileType("town", "Town", TileLayers.Objects)
        {
            PreviewColor = new Color(0.55f, 0.27f, 0.07f), // #8b4513
            Category = "Settlements",
            SortOrder = 0
        });

        RegisterTileType(new TileType("city", "City", TileLayers.Objects)
        {
            PreviewColor = new Color(0.7f, 0.35f, 0.1f),
            Category = "Settlements",
            SortOrder = 1
        });

        RegisterTileType(new TileType("castle", "Castle", TileLayers.Objects)
        {
            PreviewColor = new Color(0.5f, 0.5f, 0.55f), // #80808c
            Category = "Structures",
            SortOrder = 0,
            BlocksVision = true
        });

        RegisterTileType(new TileType("ruins", "Ruins", TileLayers.Objects)
        {
            PreviewColor = new Color(0.4f, 0.38f, 0.35f), // #66615a
            Category = "Structures",
            SortOrder = 1
        });

        RegisterTileType(new TileType("tower", "Tower", TileLayers.Objects)
        {
            PreviewColor = new Color(0.45f, 0.45f, 0.5f),
            Category = "Structures",
            SortOrder = 2,
            BlocksVision = true
        });

        RegisterTileType(new TileType("cave", "Cave", TileLayers.Objects)
        {
            PreviewColor = new Color(0.2f, 0.15f, 0.1f),
            Category = "Points of Interest",
            SortOrder = 0
        });

        RegisterTileType(new TileType("shrine", "Shrine", TileLayers.Objects)
        {
            PreviewColor = new Color(0.9f, 0.85f, 0.6f),
            Category = "Points of Interest",
            SortOrder = 1
        });
    }
}
