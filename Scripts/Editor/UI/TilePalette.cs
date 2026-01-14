using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using Hexographer.Core.Data;
using Hexographer.Rendering;

namespace Hexographer.Editor.UI;

/// <summary>
/// Visual tile selection panel organized by layer tabs and categories.
/// </summary>
public partial class TilePalette : VBoxContainer
{
    private TabContainer _tabContainer = null!;
    private TileRegistry? _registry;
    private readonly Dictionary<string, Button> _tileButtons = new();
    private string? _selectedTileId;
    private Button _editButton = null!;

    /// <summary>
    /// Event fired when a tile is selected.
    /// </summary>
    public event Action<string>? TileSelected;

    /// <summary>
    /// Event fired when the user wants to add a new tile.
    /// </summary>
    public event Action? AddTileRequested;

    /// <summary>
    /// Event fired when the user wants to batch import tiles.
    /// </summary>
    public event Action? BatchImportRequested;

    /// <summary>
    /// Event fired when the user wants to edit the selected tile.
    /// </summary>
    public event Action<string>? EditTileRequested;

    public override void _Ready()
    {
        // Header
        var header = new Label
        {
            Text = "Tile Palette",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        // Management buttons
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 4);
        AddChild(buttonRow);

        var addButton = new Button
        {
            Text = "+",
            TooltipText = "Add new tile type",
            CustomMinimumSize = new Vector2(32, 0)
        };
        addButton.Pressed += () => AddTileRequested?.Invoke();
        buttonRow.AddChild(addButton);

        var batchButton = new Button
        {
            Text = "Batch",
            TooltipText = "Batch import tiles from images"
        };
        batchButton.Pressed += () => BatchImportRequested?.Invoke();
        buttonRow.AddChild(batchButton);

        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        buttonRow.AddChild(spacer);

        _editButton = new Button
        {
            Text = "Edit",
            TooltipText = "Edit selected tile type",
            Disabled = true
        };
        _editButton.Pressed += OnEditPressed;
        buttonRow.AddChild(_editButton);

        AddChild(new HSeparator());

        // Tab container for layers
        _tabContainer = new TabContainer
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _tabContainer.TabChanged += OnTabChanged;
        AddChild(_tabContainer);
    }

    /// <summary>
    /// Populates the palette from a tile registry.
    /// </summary>
    public void PopulatePalette(TileRegistry registry)
    {
        _registry = registry;
        _tileButtons.Clear();

        // Clear existing tabs
        foreach (var child in _tabContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create a tab for each layer
        for (int layer = 0; layer < TileLayers.LayerCount; layer++)
        {
            var layerScroll = CreateLayerTab(layer);
            _tabContainer.AddChild(layerScroll);
            layerScroll.Name = TileLayers.GetLayerName(layer);
        }
    }

    private ScrollContainer CreateLayerTab(int layerIndex)
    {
        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
        };

        var content = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        scroll.AddChild(content);

        if (_registry == null)
            return scroll;

        // Get tiles for this layer grouped by category
        var tiles = _registry.GetTileTypesForLayer(layerIndex).ToList();
        var categories = tiles.Select(t => t.Category).Distinct().OrderBy(c => c);

        foreach (var category in categories)
        {
            // Category header
            var categoryLabel = new Label
            {
                Text = category,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            categoryLabel.AddThemeFontSizeOverride("font_size", 12);
            categoryLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
            content.AddChild(categoryLabel);

            // Grid of tile buttons
            var grid = new GridContainer
            {
                Columns = 3,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            grid.AddThemeConstantOverride("h_separation", 4);
            grid.AddThemeConstantOverride("v_separation", 4);
            content.AddChild(grid);

            var categoryTiles = tiles.Where(t => t.Category == category).OrderBy(t => t.SortOrder);
            foreach (var tile in categoryTiles)
            {
                var button = CreateTileButton(tile);
                _tileButtons[tile.Id] = button;
                grid.AddChild(button);
            }

            // Spacer
            content.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
        }

        return scroll;
    }

    private Button CreateTileButton(TileType tile)
    {
        var button = new Button
        {
            TooltipText = tile.DisplayName,
            ToggleMode = true,
            CustomMinimumSize = new Vector2(48, 48),
            ClipText = true
        };

        // Try to show texture if available
        if (_registry != null && !string.IsNullOrEmpty(tile.TexturePath))
        {
            var texture = _registry.GetTexture(tile.Id);
            if (texture != null)
            {
                var textureRect = new TextureRect
                {
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(32, 32),
                    AnchorRight = 1,
                    AnchorBottom = 1,
                    OffsetLeft = 8,
                    OffsetTop = 8,
                    OffsetRight = -8,
                    OffsetBottom = -8,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                button.AddChild(textureRect);
            }
            else
            {
                // Fallback to color if texture fails to load
                AddColorPreview(button, tile.PreviewColor);
            }
        }
        else
        {
            // Show preview color
            AddColorPreview(button, tile.PreviewColor);
        }

        var capturedId = tile.Id;
        button.Pressed += () => OnTileButtonPressed(capturedId);

        return button;
    }

    private static void AddColorPreview(Button button, Color color)
    {
        var colorRect = new ColorRect
        {
            Color = color,
            CustomMinimumSize = new Vector2(32, 32),
            AnchorRight = 1,
            AnchorBottom = 1,
            OffsetLeft = 8,
            OffsetTop = 8,
            OffsetRight = -8,
            OffsetBottom = -8,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        button.AddChild(colorRect);
    }

    private void OnTileButtonPressed(string tileId)
    {
        SelectTile(tileId);
        TileSelected?.Invoke(tileId);
    }

    private void OnEditPressed()
    {
        if (!string.IsNullOrEmpty(_selectedTileId))
        {
            EditTileRequested?.Invoke(_selectedTileId);
        }
    }

    private void OnTabChanged(long tabIndex)
    {
        // Could emit a layer change event if needed
    }

    /// <summary>
    /// Selects and highlights a tile by ID.
    /// </summary>
    public void SelectTile(string? tileId)
    {
        _selectedTileId = tileId;

        // Update button states
        foreach (var (id, button) in _tileButtons)
        {
            button.ButtonPressed = (id == tileId);
        }

        // Enable/disable edit button
        _editButton.Disabled = string.IsNullOrEmpty(tileId);

        // Switch to the appropriate tab
        if (!string.IsNullOrEmpty(tileId) && _registry != null)
        {
            var tile = _registry.GetTileType(tileId);
            if (tile != null)
            {
                ShowLayerTab(tile.LayerIndex);
            }
        }
    }

    /// <summary>
    /// Shows the tab for the specified layer.
    /// </summary>
    public void ShowLayerTab(int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < _tabContainer.GetTabCount())
        {
            _tabContainer.CurrentTab = layerIndex;
        }
    }

    /// <summary>
    /// Gets the currently selected tile ID.
    /// </summary>
    public string? GetSelectedTileId() => _selectedTileId;
}
