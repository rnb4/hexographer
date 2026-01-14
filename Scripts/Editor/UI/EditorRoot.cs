using Godot;
using Hexographer.Core.Data;
using Hexographer.Core.Hex;
using Hexographer.Core.Serialization;
using Hexographer.Editor.Brushes;
using Hexographer.Editor.UndoRedo;
using Hexographer.Rendering;

namespace Hexographer.Editor.UI;

using System.Collections.Generic;

/// <summary>
/// Main editor scene that manages all UI components and editor systems.
/// </summary>
public partial class EditorRoot : Control
{
    private const string DefaultSavePath = "user://map.json";

    // Core systems
    private HexGrid _grid = null!;
    private TileRegistry _registry = null!;
    private HexGridRenderer _renderer = null!;
    private EditorContext _context = null!;
    private BrushManager _brushManager = null!;
    private UndoRedoManager _undoManager = null!;

    // UI components
    private Toolbar _toolbar = null!;
    private TilePalette _tilePalette = null!;
    private LayerPanel _layerPanel = null!;
    private StatusBar _statusBar = null!;
    private ViewportController _viewportController = null!;
    private SubViewport _subViewport = null!;
    private FileDialog _fileDialog = null!;
    private PopupMenu _viewMenu = null!;
    private TileTypeEditorDialog _tileTypeEditor = null!;
    private BatchImportDialog _batchImportDialog = null!;

    // State
    private string _currentFilePath = DefaultSavePath;
    private MapMetadata? _currentMetadata;
    private bool[] _layerVisibility = { true, true, true };
    private bool _hasUnsavedChanges;
    private ConfirmationDialog _confirmExitDialog = null!;

    public override void _Ready()
    {
        // Build UI structure
        BuildUI();

        // Initialize core systems
        InitializeSystems();

        // Connect UI events
        ConnectSignals();

        // Initial UI state
        UpdateUIState();
        UpdateWindowTitle();

        // Handle window close request
        GetTree().AutoAcceptQuit = false;
    }

    private void BuildUI()
    {
        // Make this control fill the window
        AnchorsPreset = (int)LayoutPreset.FullRect;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Main vertical container
        var vbox = new VBoxContainer
        {
            AnchorsPreset = (int)LayoutPreset.FullRect
        };
        vbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(vbox);

        // Menu bar
        var menuBar = CreateMenuBar();
        vbox.AddChild(menuBar);

        // Toolbar
        _toolbar = new Toolbar();
        vbox.AddChild(_toolbar);

        vbox.AddChild(new HSeparator());

        // Main content split
        var hSplit = new HSplitContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill,
            SplitOffset = 200
        };
        vbox.AddChild(hSplit);

        // Left panel (tile palette + layer panel)
        var leftPanel = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(200, 0)
        };
        hSplit.AddChild(leftPanel);

        _tilePalette = new TilePalette
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        leftPanel.AddChild(_tilePalette);

        leftPanel.AddChild(new HSeparator());

        _layerPanel = new LayerPanel();
        leftPanel.AddChild(_layerPanel);

        // Right panel (viewport + status bar)
        var rightPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        hSplit.AddChild(rightPanel);

        // Viewport container
        var viewportContainer = new SubViewportContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            Stretch = true
        };
        rightPanel.AddChild(viewportContainer);

        _subViewport = new SubViewport
        {
            HandleInputLocally = true,
            Size = new Vector2I(800, 600),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        viewportContainer.AddChild(_subViewport);

        // Camera with viewport controller
        _viewportController = new ViewportController();
        _subViewport.AddChild(_viewportController);

        // Status bar
        _statusBar = new StatusBar();
        rightPanel.AddChild(_statusBar);

        // File dialog (hidden)
        _fileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.json ; JSON Map Files" },
            Size = new Vector2I(800, 600)
        };
        _fileDialog.FileSelected += OnFileDialogFileSelected;
        AddChild(_fileDialog);

        // Exit confirmation dialog
        _confirmExitDialog = new ConfirmationDialog
        {
            Title = "Unsaved Changes",
            DialogText = "You have unsaved changes. Do you want to save before exiting?",
            OkButtonText = "Save & Exit",
            Size = new Vector2I(400, 150)
        };
        _confirmExitDialog.AddButton("Discard", true, "discard");
        _confirmExitDialog.Confirmed += OnConfirmExitSave;
        _confirmExitDialog.CustomAction += OnConfirmExitCustomAction;
        _confirmExitDialog.Canceled += () => { }; // Do nothing, stay in editor
        AddChild(_confirmExitDialog);

        // Tile type editor dialog
        _tileTypeEditor = new TileTypeEditorDialog();
        AddChild(_tileTypeEditor);

        // Batch import dialog
        _batchImportDialog = new BatchImportDialog();
        AddChild(_batchImportDialog);
    }

    private MenuBar CreateMenuBar()
    {
        var menuBar = new MenuBar();

        // File menu
        var fileMenu = new PopupMenu { Name = "File" };
        fileMenu.AddItem("New (Ctrl+N)", 0);
        fileMenu.AddItem("Open... (Ctrl+O)", 1);
        fileMenu.AddItem("Save (Ctrl+S)", 2);
        fileMenu.AddItem("Save As... (Ctrl+Shift+S)", 3);
        fileMenu.AddSeparator();
        fileMenu.AddItem("Exit", 4);
        fileMenu.IdPressed += OnFileMenuItemPressed;
        menuBar.AddChild(fileMenu);

        // Edit menu
        var editMenu = new PopupMenu { Name = "Edit" };
        editMenu.AddItem("Undo (Ctrl+Z)", 0);
        editMenu.AddItem("Redo (Ctrl+Y)", 1);
        editMenu.IdPressed += OnEditMenuItemPressed;
        menuBar.AddChild(editMenu);

        // View menu
        _viewMenu = new PopupMenu { Name = "View" };
        _viewMenu.AddItem("Reset Zoom", 0);
        _viewMenu.AddSeparator();
        _viewMenu.AddCheckItem("Show Grid (Ctrl+G)", 4);
        _viewMenu.SetItemChecked(2, true); // Grid visible by default (index 2 after separator)
        _viewMenu.AddSeparator();
        _viewMenu.AddCheckItem("Ground Layer", 1);
        _viewMenu.AddCheckItem("Features Layer", 2);
        _viewMenu.AddCheckItem("Objects Layer", 3);
        _viewMenu.SetItemChecked(4, true);
        _viewMenu.SetItemChecked(5, true);
        _viewMenu.SetItemChecked(6, true);
        _viewMenu.IdPressed += OnViewMenuItemPressed;
        menuBar.AddChild(_viewMenu);

        return menuBar;
    }

    private void InitializeSystems()
    {
        // Create tile registry
        _registry = new TileRegistry();
        _registry.RegisterDefaultTiles();

        // Create hex grid
        _grid = new HexGrid(HexOrientation.PointyTop);

        // Create renderer
        _renderer = new HexGridRenderer();
        _renderer.Initialize(_grid, _registry);

        // Center renderer in viewport
        _renderer.Position = new Vector2(400, 300);
        _subViewport.AddChild(_renderer);

        // Create undo manager
        _undoManager = new UndoRedoManager();
        _undoManager.HistoryChanged += OnUndoHistoryChanged;

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

        // Create brush manager
        _brushManager = new BrushManager();
        _brushManager.Initialize(_context);
        _brushManager.RegisterDefaultBrushes();
        _brushManager.SetActiveBrush("Brush");

        // Populate tile palette
        _tilePalette.PopulatePalette(_registry);
        _tilePalette.SelectTile("grass");

        // Create a starter map
        CreateStarterMap();
    }

    private void ConnectSignals()
    {
        // Toolbar events
        _toolbar.BrushSelected += OnBrushSelected;
        _toolbar.UndoPressed += OnUndoPressed;
        _toolbar.RedoPressed += OnRedoPressed;
        _toolbar.ZoomChanged += OnZoomSliderChanged;

        // Tile palette events
        _tilePalette.TileSelected += OnTileSelected;
        _tilePalette.AddTileRequested += OnAddTileRequested;
        _tilePalette.BatchImportRequested += OnBatchImportRequested;
        _tilePalette.EditTileRequested += OnEditTileRequested;

        // Tile type editor events
        _tileTypeEditor.TileTypeSaved += OnTileTypeSaved;
        _tileTypeEditor.TileTypeDeleted += OnTileTypeDeleted;

        // Batch import events
        _batchImportDialog.TilesImported += OnTilesImported;

        // Layer panel events
        _layerPanel.LayerSelected += OnLayerSelected;
        _layerPanel.LayerVisibilityChanged += OnLayerVisibilityChanged;

        // Viewport controller events
        _viewportController.ZoomChanged += OnViewportZoomChanged;

        // Editor context events
        _context.ActiveLayerChanged += OnActiveLayerChanged;
        _context.SelectedTileTypeChanged += OnSelectedTileTypeChanged;

        // Grid events for dirty tracking
        _grid.TileChanged += OnTileChanged;
        _grid.TilesChanged += OnTilesChanged;
    }

    private void UpdateUIState()
    {
        _toolbar.SetUndoEnabled(_undoManager.CanUndo);
        _toolbar.SetRedoEnabled(_undoManager.CanRedo);
        _statusBar.UpdateActiveBrush(_brushManager.ActiveBrush?.Name);
        _statusBar.UpdateSelectedTile(_context.SelectedTileTypeId);
        _statusBar.UpdateZoom(_viewportController.ZoomLevel);
    }

    public override void _Process(double delta)
    {
        // Update hover coordinate in status bar
        var worldPos = GetMouseWorldPosition();
        if (worldPos.HasValue)
        {
            var hoveredHex = _renderer.PixelToHex(worldPos.Value);
            _statusBar.UpdateCoordinates(hoveredHex);
            _renderer.SetHoveredHex(hoveredHex);
        }
    }

    /// <summary>
    /// Converts the current mouse position to world coordinates within the SubViewport.
    /// Returns null if mouse is not over the viewport.
    /// </summary>
    private Vector2? GetMouseWorldPosition()
    {
        // Get viewport container's global rect to check if mouse is inside
        var viewportContainer = _subViewport.GetParent<SubViewportContainer>();
        if (viewportContainer == null)
            return null;

        var containerRect = viewportContainer.GetGlobalRect();
        var globalMousePos = GetGlobalMousePosition();

        if (!containerRect.HasPoint(globalMousePos))
            return null;

        // Get mouse position relative to the viewport container
        var localMousePos = globalMousePos - containerRect.Position;

        // Account for stretch - scale to SubViewport size
        var scale = new Vector2(
            _subViewport.Size.X / containerRect.Size.X,
            _subViewport.Size.Y / containerRect.Size.Y
        );
        var viewportMousePos = localMousePos * scale;

        // Apply inverse camera transform to get world position
        var cameraTransform = _viewportController.GetCanvasTransform();
        var worldPos = cameraTransform.AffineInverse() * viewportMousePos;

        return worldPos;
    }

    public override void _Input(InputEvent @event)
    {
        // Handle keyboard shortcuts
        if (@event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.CtrlPressed)
            {
                HandleCtrlShortcuts(key);
                return;
            }

            HandleKeyboardShortcuts(key.Keycode);
        }

        // Route mouse input to brush manager when in viewport
        if (IsMouseInViewport())
        {
            RouteMouseInput(@event);
        }
    }

    private bool IsMouseInViewport()
    {
        return GetMouseWorldPosition().HasValue;
    }

    private void RouteMouseInput(InputEvent @event)
    {
        var worldPos = GetMouseWorldPosition();
        if (!worldPos.HasValue)
            return;

        var coord = _renderer.PixelToHex(worldPos.Value);

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed)
            {
                _brushManager.HandleMouseDown(coord, mouseButton.ButtonIndex);
            }
            else
            {
                _brushManager.HandleMouseUp(coord, mouseButton.ButtonIndex);
            }
        }
        else if (@event is InputEventMouseMotion)
        {
            _brushManager.HandleMouseMove(coord);
            _brushManager.UpdatePreview(coord);
        }
    }

    private void HandleCtrlShortcuts(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Key.N:
                NewMap();
                break;
            case Key.O:
                ShowOpenDialog();
                break;
            case Key.S when key.ShiftPressed:
                ShowSaveAsDialog();
                break;
            case Key.S:
                SaveMap();
                break;
            case Key.Z when !key.ShiftPressed:
                OnUndoPressed();
                break;
            case Key.Z when key.ShiftPressed:
            case Key.Y:
                OnRedoPressed();
                break;
            case Key.G:
                ToggleGrid();
                break;
        }
    }

    private void HandleKeyboardShortcuts(Key keycode)
    {
        switch (keycode)
        {
            // Brush selection
            case Key.B:
                SelectBrush("Brush");
                break;
            case Key.E:
                SelectBrush("Eraser");
                break;
            case Key.F:
                SelectBrush("Fill");
                break;
            case Key.L:
                SelectBrush("Line");
                break;
            case Key.A:
                SelectBrush("Area");
                break;

            // Layer selection
            case Key.Key1:
                _context.SetActiveLayer(TileLayers.Ground);
                break;
            case Key.Key2:
                _context.SetActiveLayer(TileLayers.Features);
                break;
            case Key.Key3:
                _context.SetActiveLayer(TileLayers.Objects);
                break;

            // Quick tile selection
            case Key.G:
                _context.SetSelectedTileType("grass");
                _tilePalette.SelectTile("grass");
                break;
            case Key.W:
                _context.SetSelectedTileType("water");
                _tilePalette.SelectTile("water");
                break;
            case Key.S:
                _context.SetSelectedTileType("sand");
                _tilePalette.SelectTile("sand");
                break;
            case Key.D:
                _context.SetSelectedTileType("dirt");
                _tilePalette.SelectTile("dirt");
                break;
            case Key.R:
                _context.SetSelectedTileType("road");
                _context.SetActiveLayer(TileLayers.Features);
                _tilePalette.SelectTile("road");
                break;
            case Key.T:
                _context.SetSelectedTileType("forest");
                _context.SetActiveLayer(TileLayers.Features);
                _tilePalette.SelectTile("forest");
                break;
        }
    }

    private void SelectBrush(string brushName)
    {
        _brushManager.SetActiveBrush(brushName);
        _toolbar.HighlightBrush(brushName);
        _statusBar.UpdateActiveBrush(brushName);
    }

    // Menu handlers
    private void OnFileMenuItemPressed(long id)
    {
        switch (id)
        {
            case 0: NewMap(); break;
            case 1: ShowOpenDialog(); break;
            case 2: SaveMap(); break;
            case 3: ShowSaveAsDialog(); break;
            case 4: RequestExit(); break;
        }
    }

    private void OnEditMenuItemPressed(long id)
    {
        switch (id)
        {
            case 0: OnUndoPressed(); break;
            case 1: OnRedoPressed(); break;
        }
    }

    private void OnViewMenuItemPressed(long id)
    {
        switch (id)
        {
            case 0: // Reset Zoom
                _viewportController.ResetView();
                break;
            case 4: // Toggle Grid
                ToggleGrid();
                break;
            case 1: // Ground Layer
            case 2: // Features Layer
            case 3: // Objects Layer
                ToggleLayerVisibility((int)id - 1);
                break;
        }
    }

    private void ToggleGrid()
    {
        _renderer.ShowGrid = !_renderer.ShowGrid;
        // Update menu checkbox (index 2 is the grid toggle after Reset Zoom and separator)
        _viewMenu.SetItemChecked(2, _renderer.ShowGrid);
    }

    private void ToggleLayerVisibility(int layerIndex)
    {
        // Menu indices: 4=Ground, 5=Features, 6=Objects (after Reset, sep, Grid, sep)
        int menuIndex = layerIndex + 4;
        bool newState = !_viewMenu.IsItemChecked(menuIndex);
        _viewMenu.SetItemChecked(menuIndex, newState);
        _layerVisibility[layerIndex] = newState;
        _renderer.SetLayerVisible(layerIndex, newState);
        _layerPanel.SetLayerVisibility(layerIndex, newState);
    }

    // UI event handlers
    private void OnBrushSelected(string brushName)
    {
        _brushManager.SetActiveBrush(brushName);
        _statusBar.UpdateActiveBrush(brushName);
    }

    private void OnUndoPressed()
    {
        if (_undoManager.Undo())
        {
            _renderer.RenderAll();
        }
    }

    private void OnRedoPressed()
    {
        if (_undoManager.Redo())
        {
            _renderer.RenderAll();
        }
    }

    private void OnZoomSliderChanged(float level)
    {
        _viewportController.SetZoom(level);
        _statusBar.UpdateZoom(level);
    }

    private void OnTileSelected(string tileId)
    {
        _context.SetSelectedTileType(tileId);
    }

    private void OnLayerSelected(int layer)
    {
        _context.SetActiveLayer(layer);
    }

    private void OnLayerVisibilityChanged(int layer, bool visible)
    {
        _layerVisibility[layer] = visible;
        _renderer.SetLayerVisible(layer, visible);
    }

    private void OnViewportZoomChanged(float level)
    {
        _toolbar.SetZoomLevel(level);
        _statusBar.UpdateZoom(level);
    }

    private void OnUndoHistoryChanged()
    {
        _toolbar.SetUndoEnabled(_undoManager.CanUndo);
        _toolbar.SetRedoEnabled(_undoManager.CanRedo);
    }

    private void OnActiveLayerChanged(int layer)
    {
        _layerPanel.SetActiveLayer(layer);
        _tilePalette.ShowLayerTab(layer);
    }

    private void OnSelectedTileTypeChanged(string? tileId)
    {
        _tilePalette.SelectTile(tileId);
        var tile = _registry.GetTileType(tileId);
        _statusBar.UpdateSelectedTile(tileId, tile?.DisplayName);
    }

    // Tile type editor handlers
    private void OnAddTileRequested()
    {
        _tileTypeEditor.ShowNew(id => !_registry.HasTileType(id));
    }

    private void OnBatchImportRequested()
    {
        _batchImportDialog.ShowDialog(id => !_registry.HasTileType(id));
    }

    private void OnEditTileRequested(string tileId)
    {
        var tile = _registry.GetTileType(tileId);
        if (tile != null)
        {
            _tileTypeEditor.ShowEdit(tile, id => id == tileId || !_registry.HasTileType(id));
        }
    }

    private void OnTileTypeSaved(TileType tile)
    {
        _registry.RegisterTileType(tile);
        _registry.ClearTextureCache();
        _tilePalette.PopulatePalette(_registry);
        _tilePalette.SelectTile(tile.Id);
        _renderer.RenderAll();
        MarkDirty();
    }

    private void OnTileTypeDeleted(string tileId)
    {
        _registry.UnregisterTileType(tileId);
        _tilePalette.PopulatePalette(_registry);
        _tilePalette.SelectTile(null);
        _renderer.RenderAll();
        MarkDirty();
    }

    private void OnTilesImported(System.Collections.Generic.List<TileType> tiles)
    {
        foreach (var tile in tiles)
        {
            _registry.RegisterTileType(tile);
        }
        _registry.ClearTextureCache();
        _tilePalette.PopulatePalette(_registry);
        _renderer.RenderAll();
        MarkDirty();
    }

    // Dirty state tracking
    private void MarkDirty()
    {
        if (!_hasUnsavedChanges)
        {
            _hasUnsavedChanges = true;
            UpdateWindowTitle();
        }
    }

    private void ClearDirty()
    {
        _hasUnsavedChanges = false;
        UpdateWindowTitle();
    }

    private void UpdateWindowTitle()
    {
        var filename = System.IO.Path.GetFileName(_currentFilePath);
        if (string.IsNullOrEmpty(filename)) filename = "Untitled";
        var dirty = _hasUnsavedChanges ? "*" : "";
        DisplayServer.WindowSetTitle($"Hexographer - {filename}{dirty}");
    }

    // Exit handling
    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            RequestExit();
        }
    }

    private void RequestExit()
    {
        if (_hasUnsavedChanges)
        {
            _confirmExitDialog.PopupCentered();
        }
        else
        {
            GetTree().Quit();
        }
    }

    private void OnConfirmExitSave()
    {
        SaveMap();
        GetTree().Quit();
    }

    private void OnConfirmExitCustomAction(StringName action)
    {
        if (action == "discard")
        {
            GetTree().Quit();
        }
    }

    // File operations
    private void ShowOpenDialog()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFile;
        _fileDialog.Title = "Open Map";
        _fileDialog.PopupCentered();
    }

    private void ShowSaveAsDialog()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.SaveFile;
        _fileDialog.Title = "Save Map As";
        _fileDialog.PopupCentered();
    }

    private void OnFileDialogFileSelected(string path)
    {
        if (_fileDialog.FileMode == FileDialog.FileModeEnum.OpenFile)
        {
            LoadMap(path);
        }
        else
        {
            _currentFilePath = path;
            SaveMap();
        }
    }

    private void SaveMap()
    {
        try
        {
            _currentMetadata ??= new MapMetadata { Name = "My Map" };
            MapSerializer.Save(_grid, _currentFilePath, _currentMetadata, _registry);
            ClearDirty();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to save map: {ex.Message}");
        }
    }

    private void LoadMap(string path)
    {
        try
        {
            if (!FileAccess.FileExists(path))
            {
                GD.PrintErr($"File not found: {path}");
                return;
            }

            var loadedGrid = MapSerializer.Load(path, out _currentMetadata, out var customTileTypes);

            // Clear existing custom tiles and register new ones
            // First, re-initialize registry with defaults
            _registry.Clear();
            _registry.RegisterDefaultTiles();

            // Add custom tile types from the map
            if (customTileTypes != null)
            {
                foreach (var tile in customTileTypes)
                {
                    _registry.RegisterTileType(tile);
                }
            }

            _grid.Clear();
            foreach (var tile in loadedGrid.GetAllTiles())
            {
                _grid.SetTile(tile.Coord, tile);
            }
            _grid.Orientation = loadedGrid.Orientation;
            _grid.HexSize = loadedGrid.HexSize;
            _grid.LayerCount = loadedGrid.LayerCount;

            _currentFilePath = path;
            _undoManager.Clear();
            _tilePalette.PopulatePalette(_registry);
            _renderer.RenderAll();
            ClearDirty();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to load map: {ex.Message}");
        }
    }

    private void NewMap()
    {
        _grid.Clear();
        _undoManager.Clear();

        // Reset registry to default tiles only
        _registry.Clear();
        _registry.RegisterDefaultTiles();
        _tilePalette.PopulatePalette(_registry);

        _currentMetadata = new MapMetadata { Name = "Untitled Map" };
        _currentFilePath = DefaultSavePath;
        _renderer.RenderAll();
        ClearDirty();
    }

    private void CreateStarterMap()
    {
        // Create a small test map
        for (int q = -3; q <= 3; q++)
        {
            for (int r = -3; r <= 3; r++)
            {
                var coord = new HexCoord(q, r);
                if (coord.DistanceTo(HexCoord.Zero) <= 4)
                {
                    _grid.SetTileLayer(coord, TileLayers.Ground, "grass");
                }
            }
        }

        // Add some variety
        _grid.SetTileLayer(new HexCoord(-2, 0), TileLayers.Ground, "water");
        _grid.SetTileLayer(new HexCoord(-2, 1), TileLayers.Ground, "water");
        _grid.SetTileLayer(new HexCoord(-1, 1), TileLayers.Ground, "water_shallow");
        _grid.SetTileLayer(new HexCoord(-1, 0), TileLayers.Ground, "sand");

        _grid.SetTileLayer(new HexCoord(1, -2), TileLayers.Features, "forest");
        _grid.SetTileLayer(new HexCoord(1, -1), TileLayers.Features, "forest");
        _grid.SetTileLayer(new HexCoord(0, -3), TileLayers.Features, "mountain");

        _grid.SetTileLayer(new HexCoord(0, 2), TileLayers.Objects, "town");

        _renderer.RenderAll();
        // Clear dirty since this is initial state
        _hasUnsavedChanges = false;
    }

    private void OnTileChanged(HexCoord _)
    {
        MarkDirty();
    }

    private void OnTilesChanged(IEnumerable<HexCoord> _)
    {
        MarkDirty();
    }

    public override void _ExitTree()
    {
        // Unsubscribe from events
        _grid.TileChanged -= OnTileChanged;
        _grid.TilesChanged -= OnTilesChanged;
        _undoManager.HistoryChanged -= OnUndoHistoryChanged;
        _context.ActiveLayerChanged -= OnActiveLayerChanged;
        _context.SelectedTileTypeChanged -= OnSelectedTileTypeChanged;

        base._ExitTree();
    }
}
