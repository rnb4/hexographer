using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;
using Hexographer.Editor;
using Hexographer.Editor.Brushes;
using Hexographer.Editor.UndoRedo;
using Hexographer.Rendering;

public partial class Main : Node2D
{
    private HexGrid _grid = null!;
    private TileRegistry _registry = null!;
    private HexGridRenderer _renderer = null!;
    private EditorContext _context = null!;
    private BrushManager _brushManager = null!;
    private UndoRedoManager _undoManager = null!;

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

        // Create undo/redo manager
        _undoManager = new UndoRedoManager();

        // Create editor context
        _context = new EditorContext
        {
            Grid = _grid,
            Registry = _registry,
            Renderer = _renderer,
            SelectedTileTypeId = "grass",
            ActiveLayer = TileLayers.Ground,
            UndoManager = _undoManager
        };

        // Create and initialize brush manager
        _brushManager = new BrushManager();
        _brushManager.Initialize(_context);
        _brushManager.RegisterDefaultBrushes();
        _brushManager.SetActiveBrush("Brush");

        GD.Print("Hex Map Editor Ready!");
        GD.Print("Controls:");
        GD.Print("  Ctrl+Z - Undo");
        GD.Print("  Ctrl+Y / Ctrl+Shift+Z - Redo");
        GD.Print("  B - Brush tool (paint single tiles)");
        GD.Print("  E - Eraser tool");
        GD.Print("  F - Fill tool (flood fill)");
        GD.Print("  L - Line tool");
        GD.Print("  A - Area tool (right-click to toggle shape)");
        GD.Print("  1/2/3 - Switch layer (Ground/Features/Objects)");
        GD.Print("  G/W/S/D/R/T - Select tile (Grass/Water/Sand/Dirt/Road/Forest)");
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
        // Handle keyboard shortcuts
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            // Check for undo/redo with Ctrl modifier
            if (key.CtrlPressed)
            {
                if (key.Keycode == Key.Z && !key.ShiftPressed)
                {
                    // Ctrl+Z = Undo
                    if (_undoManager.Undo())
                    {
                        GD.Print($"Undo: {_undoManager.NextRedoDescription}");
                    }
                    return;
                }
                else if ((key.Keycode == Key.Z && key.ShiftPressed) || key.Keycode == Key.Y)
                {
                    // Ctrl+Shift+Z or Ctrl+Y = Redo
                    if (_undoManager.Redo())
                    {
                        GD.Print($"Redo: {_undoManager.NextUndoDescription}");
                    }
                    return;
                }
            }

            HandleKeyboardShortcuts(key.Keycode);
        }

        // Route mouse input to brush manager
        if (@event is InputEventMouseButton mouseButton)
        {
            var coord = _renderer.GetHexAtGlobalPosition(mouseButton.Position);

            if (mouseButton.Pressed)
            {
                _brushManager.HandleMouseDown(coord, mouseButton.ButtonIndex);
            }
            else
            {
                _brushManager.HandleMouseUp(coord, mouseButton.ButtonIndex);
            }
        }
        else if (@event is InputEventMouseMotion mouseMotion)
        {
            var coord = _renderer.GetHexAtGlobalPosition(mouseMotion.Position);
            _brushManager.HandleMouseMove(coord);
            _brushManager.UpdatePreview(coord);
        }
    }

    private void HandleKeyboardShortcuts(Key keycode)
    {
        switch (keycode)
        {
            // Brush selection
            case Key.B:
                _brushManager.SetActiveBrush("Brush");
                GD.Print("Brush: Paint");
                break;
            case Key.E:
                _brushManager.SetActiveBrush("Eraser");
                GD.Print("Brush: Eraser");
                break;
            case Key.F:
                _brushManager.SetActiveBrush("Fill");
                GD.Print("Brush: Fill");
                break;
            case Key.L:
                _brushManager.SetActiveBrush("Line");
                GD.Print("Brush: Line");
                break;
            case Key.A:
                _brushManager.SetActiveBrush("Area");
                GD.Print("Brush: Area");
                break;

            // Layer selection
            case Key.Key1:
                _context.SetActiveLayer(TileLayers.Ground);
                GD.Print("Layer: Ground");
                break;
            case Key.Key2:
                _context.SetActiveLayer(TileLayers.Features);
                GD.Print("Layer: Features");
                break;
            case Key.Key3:
                _context.SetActiveLayer(TileLayers.Objects);
                GD.Print("Layer: Objects");
                break;

            // Tile type selection (quick access)
            case Key.G:
                _context.SetSelectedTileType("grass");
                GD.Print("Tile: Grass");
                break;
            case Key.W:
                _context.SetSelectedTileType("water");
                GD.Print("Tile: Water");
                break;
            case Key.S:
                _context.SetSelectedTileType("sand");
                GD.Print("Tile: Sand");
                break;
            case Key.D:
                _context.SetSelectedTileType("dirt");
                GD.Print("Tile: Dirt");
                break;
            case Key.R:
                _context.SetSelectedTileType("road");
                _context.SetActiveLayer(TileLayers.Features);
                GD.Print("Tile: Road (Features layer)");
                break;
            case Key.T:
                _context.SetSelectedTileType("forest");
                _context.SetActiveLayer(TileLayers.Features);
                GD.Print("Tile: Forest (Features layer)");
                break;
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
}
