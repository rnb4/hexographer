using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;
using Hexographer.Rendering;

public partial class Main : Node2D
{
    private HexGrid _grid = null!;
    private TileRegistry _registry = null!;
    private HexGridRenderer _renderer = null!;

    public override void _Ready()
    {
        // Create tile registry with default test tiles
        _registry = new TileRegistry();
        _registry.RegisterDefaultTiles();

        // Create hex grid (pointy-top, 48 pixel hex size)
        _grid = new HexGrid(HexOrientation.PointyTop, 48f);

        // Add some test tiles to demonstrate rendering
        CreateTestMap();

        // Create and initialize the renderer
        _renderer = new HexGridRenderer();
        _renderer.Initialize(_grid, _registry);

        // Center the renderer in the viewport
        var viewportSize = GetViewportRect().Size;
        _renderer.Position = viewportSize / 2;

        AddChild(_renderer);
    }

    public override void _Process(double delta)
    {
        // Update hover highlight based on mouse position
        var mousePos = GetGlobalMousePosition();
        var hoveredHex = _renderer.GetHexAtGlobalPosition(mousePos);
        _renderer.SetHoveredHex(hoveredHex);
    }

    public override void _Input(InputEvent @event)
    {
        // Handle mouse clicks for testing
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            var clickedHex = _renderer.GetHexAtGlobalPosition(mouseButton.Position);

            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                // Left click: cycle through ground tiles
                CycleGroundTile(clickedHex);
            }
            else if (mouseButton.ButtonIndex == MouseButton.Right)
            {
                // Right click: toggle forest on features layer
                ToggleForest(clickedHex);
            }
        }
    }

    private void CreateTestMap()
    {
        // Create a small test map with various tile types
        // Center area with grass
        for (int q = -3; q <= 3; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                // Skip if outside hex radius
                var coord = new HexCoord(q, r);
                if (coord.DistanceTo(HexCoord.Zero) > 4)
                    continue;

                _grid.SetTileLayer(coord, TileLayers.Ground, "grass");
            }
        }

        // Add some water
        _grid.SetTileLayer(new HexCoord(-2, 0), TileLayers.Ground, "water");
        _grid.SetTileLayer(new HexCoord(-2, 1), TileLayers.Ground, "water");
        _grid.SetTileLayer(new HexCoord(-1, 1), TileLayers.Ground, "water_shallow");

        // Add some sand near water
        _grid.SetTileLayer(new HexCoord(-1, 0), TileLayers.Ground, "sand");
        _grid.SetTileLayer(new HexCoord(-2, 2), TileLayers.Ground, "sand");

        // Add dirt path
        _grid.SetTileLayer(new HexCoord(0, 0), TileLayers.Ground, "dirt");
        _grid.SetTileLayer(new HexCoord(1, 0), TileLayers.Ground, "dirt");
        _grid.SetTileLayer(new HexCoord(2, 0), TileLayers.Ground, "dirt");

        // Add some stone
        _grid.SetTileLayer(new HexCoord(2, -2), TileLayers.Ground, "stone");
        _grid.SetTileLayer(new HexCoord(3, -2), TileLayers.Ground, "stone");

        // Features layer: forests
        _grid.SetTileLayer(new HexCoord(1, -2), TileLayers.Features, "forest");
        _grid.SetTileLayer(new HexCoord(1, -1), TileLayers.Features, "forest");
        _grid.SetTileLayer(new HexCoord(2, -1), TileLayers.Features, "forest_dense");

        // Features layer: hills and mountain
        _grid.SetTileLayer(new HexCoord(-1, -2), TileLayers.Features, "hills");
        _grid.SetTileLayer(new HexCoord(0, -3), TileLayers.Features, "mountain");

        // Features layer: road
        _grid.SetTileLayer(new HexCoord(0, 0), TileLayers.Features, "road");
        _grid.SetTileLayer(new HexCoord(1, 0), TileLayers.Features, "road");
        _grid.SetTileLayer(new HexCoord(2, 0), TileLayers.Features, "road");

        // Objects layer: town and structures
        _grid.SetTileLayer(new HexCoord(0, 2), TileLayers.Objects, "town");
        _grid.SetTileLayer(new HexCoord(3, -1), TileLayers.Objects, "castle");
        _grid.SetTileLayer(new HexCoord(-1, 3), TileLayers.Objects, "ruins");
    }

    private void CycleGroundTile(HexCoord coord)
    {
        string[] groundTiles = { "grass", "dirt", "sand", "stone", "water_shallow", "water" };

        var currentType = _grid.GetTileLayer(coord, TileLayers.Ground);
        int currentIndex = -1;

        if (currentType != null)
        {
            for (int i = 0; i < groundTiles.Length; i++)
            {
                if (groundTiles[i] == currentType)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        int nextIndex = (currentIndex + 1) % groundTiles.Length;
        _grid.SetTileLayer(coord, TileLayers.Ground, groundTiles[nextIndex]);
    }

    private void ToggleForest(HexCoord coord)
    {
        var currentFeature = _grid.GetTileLayer(coord, TileLayers.Features);

        if (currentFeature == "forest" || currentFeature == "forest_dense")
        {
            _grid.ClearTileLayer(coord, TileLayers.Features);
        }
        else
        {
            _grid.SetTileLayer(coord, TileLayers.Features, "forest");
        }
    }
}
