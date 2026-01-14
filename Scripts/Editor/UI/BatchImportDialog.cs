using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Hexographer.Core.Data;

namespace Hexographer.Editor.UI;

/// <summary>
/// Dialog for batch importing multiple image files as tile types.
/// </summary>
public partial class BatchImportDialog : Window
{
    private ItemList _fileList = null!;
    private OptionButton _layerOption = null!;
    private LineEdit _categoryEdit = null!;
    private SpinBox _pixelSizeSpinBox = null!;
    private Label _countLabel = null!;
    private Button _importButton = null!;
    private FileDialog _fileDialog = null!;

    private readonly List<string> _filePaths = new();
    private Func<string, bool>? _idValidator;

    /// <summary>
    /// Event fired when tiles are imported.
    /// </summary>
    public event Action<List<TileType>>? TilesImported;

    public override void _Ready()
    {
        Title = "Batch Import Tiles";
        Size = new Vector2I(500, 600);
        Exclusive = true;
        Visible = false;

        BuildUI();
        CloseRequested += Hide;
    }

    private void BuildUI()
    {
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        AddChild(margin);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        margin.AddChild(vbox);

        // Instructions
        vbox.AddChild(new Label { Text = "Select image files to import as tiles:" });

        // Add buttons
        var addRow = new HBoxContainer();
        addRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(addRow);

        var addFilesButton = new Button { Text = "Add Files..." };
        addFilesButton.Pressed += OnAddFiles;
        addRow.AddChild(addFilesButton);

        var addFolderButton = new Button { Text = "Add Folder..." };
        addFolderButton.Pressed += OnAddFolder;
        addRow.AddChild(addFolderButton);

        var clearButton = new Button { Text = "Clear All" };
        clearButton.Pressed += OnClearAll;
        addRow.AddChild(clearButton);

        vbox.AddChild(new HSeparator());

        // Import settings
        vbox.AddChild(new Label { Text = "Import Settings:" });

        var settingsGrid = new GridContainer { Columns = 2 };
        settingsGrid.AddThemeConstantOverride("h_separation", 8);
        settingsGrid.AddThemeConstantOverride("v_separation", 8);
        vbox.AddChild(settingsGrid);

        settingsGrid.AddChild(new Label { Text = "Target Layer:" });
        _layerOption = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _layerOption.AddItem("Ground", TileLayers.Ground);
        _layerOption.AddItem("Features", TileLayers.Features);
        _layerOption.AddItem("Objects", TileLayers.Objects);
        settingsGrid.AddChild(_layerOption);

        settingsGrid.AddChild(new Label { Text = "Category:" });
        _categoryEdit = new LineEdit
        {
            Text = "Imported",
            PlaceholderText = "Imported",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        settingsGrid.AddChild(_categoryEdit);

        settingsGrid.AddChild(new Label { Text = "Pixel Size:" });
        _pixelSizeSpinBox = new SpinBox
        {
            MinValue = 16,
            MaxValue = 512,
            Step = 1,
            Value = 64,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Width of the hex in the source textures (in pixels)"
        };
        settingsGrid.AddChild(_pixelSizeSpinBox);

        vbox.AddChild(new HSeparator());

        // File list
        vbox.AddChild(new Label { Text = "Files to Import:" });

        var scroll = new ScrollContainer
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 200)
        };
        vbox.AddChild(scroll);

        _fileList = new ItemList
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SelectMode = ItemList.SelectModeEnum.Multi
        };
        scroll.AddChild(_fileList);

        // Count label
        _countLabel = new Label { Text = "0 files selected" };
        vbox.AddChild(_countLabel);

        // Remove selected button
        var removeButton = new Button { Text = "Remove Selected" };
        removeButton.Pressed += OnRemoveSelected;
        vbox.AddChild(removeButton);

        vbox.AddChild(new HSeparator());

        // Action buttons
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(buttonRow);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        buttonRow.AddChild(spacer);

        _importButton = new Button { Text = "Import All", Disabled = true };
        _importButton.Pressed += OnImport;
        buttonRow.AddChild(_importButton);

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Pressed += Hide;
        buttonRow.AddChild(cancelButton);

        // File dialog for selecting images
        _fileDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.png, *.jpg, *.jpeg, *.webp ; Image Files" },
            Size = new Vector2I(700, 500)
        };
        _fileDialog.FilesSelected += OnFilesSelected;
        _fileDialog.DirSelected += OnFolderSelected;
        AddChild(_fileDialog);
    }

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    public void ShowDialog(Func<string, bool>? idValidator = null)
    {
        _idValidator = idValidator;
        _filePaths.Clear();
        _fileList.Clear();
        UpdateCountLabel();
        PopupCentered();
    }

    private void OnAddFiles()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenFiles;
        _fileDialog.Title = "Select Image Files";
        _fileDialog.PopupCentered();
    }

    private void OnAddFolder()
    {
        _fileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
        _fileDialog.Title = "Select Folder";
        _fileDialog.PopupCentered();
    }

    private void OnFilesSelected(string[] paths)
    {
        AddFiles(paths);
    }

    private void OnFolderSelected(string path)
    {
        AddFolder(path);
    }

    /// <summary>
    /// Adds files to the import list.
    /// </summary>
    public void AddFiles(string[] paths)
    {
        foreach (var path in paths)
        {
            if (!_filePaths.Contains(path) && IsImageFile(path))
            {
                _filePaths.Add(path);
                _fileList.AddItem(System.IO.Path.GetFileName(path));
            }
        }
        UpdateCountLabel();
    }

    /// <summary>
    /// Adds all image files from a folder recursively.
    /// </summary>
    public void AddFolder(string folderPath)
    {
        try
        {
            var dir = DirAccess.Open(folderPath);
            if (dir == null)
                return;

            dir.ListDirBegin();
            var fileName = dir.GetNext();

            while (!string.IsNullOrEmpty(fileName))
            {
                var fullPath = System.IO.Path.Combine(folderPath, fileName);

                if (dir.CurrentIsDir() && fileName != "." && fileName != "..")
                {
                    AddFolder(fullPath); // Recurse into subdirectories
                }
                else if (IsImageFile(fileName))
                {
                    if (!_filePaths.Contains(fullPath))
                    {
                        _filePaths.Add(fullPath);
                        _fileList.AddItem(fileName);
                    }
                }

                fileName = dir.GetNext();
            }
            dir.ListDirEnd();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error scanning folder: {ex.Message}");
        }

        UpdateCountLabel();
    }

    private void OnClearAll()
    {
        _filePaths.Clear();
        _fileList.Clear();
        UpdateCountLabel();
    }

    private void OnRemoveSelected()
    {
        var selected = _fileList.GetSelectedItems();
        // Remove in reverse order to maintain indices
        foreach (var index in selected.Reverse())
        {
            _filePaths.RemoveAt(index);
            _fileList.RemoveItem(index);
        }
        UpdateCountLabel();
    }

    private void UpdateCountLabel()
    {
        _countLabel.Text = $"{_filePaths.Count} file{(_filePaths.Count == 1 ? "" : "s")} selected";
        _importButton.Disabled = _filePaths.Count == 0;
    }

    private void OnImport()
    {
        var tiles = new List<TileType>();
        var layer = _layerOption.Selected;
        var category = string.IsNullOrEmpty(_categoryEdit.Text.Trim()) ? "Imported" : _categoryEdit.Text.Trim();
        var pixelSize = (int)_pixelSizeSpinBox.Value;

        foreach (var path in _filePaths)
        {
            var id = GenerateId(path);

            // Skip if ID already exists
            if (_idValidator != null && !_idValidator(id))
            {
                GD.Print($"Skipping {path}: ID '{id}' already exists");
                continue;
            }

            var tile = new TileType
            {
                Id = id,
                DisplayName = GenerateDisplayName(path),
                LayerIndex = layer,
                Category = category,
                TexturePath = path,
                PixelSize = pixelSize,
                PreviewColor = ExtractPreviewColor(path)
            };

            tiles.Add(tile);
        }

        if (tiles.Count > 0)
        {
            TilesImported?.Invoke(tiles);
        }

        Hide();
    }

    /// <summary>
    /// Generates a tile ID from a filename.
    /// </summary>
    private static string GenerateId(string path)
    {
        var filename = System.IO.Path.GetFileNameWithoutExtension(path);
        // Convert to lowercase, replace spaces and hyphens with underscores
        var id = filename.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_");
        // Remove any characters that aren't alphanumeric or underscore
        id = Regex.Replace(id, @"[^a-z0-9_]", "");
        // Ensure it starts with a letter
        if (id.Length > 0 && char.IsDigit(id[0]))
        {
            id = "tile_" + id;
        }
        return string.IsNullOrEmpty(id) ? "imported_tile" : id;
    }

    /// <summary>
    /// Generates a display name from a filename.
    /// </summary>
    private static string GenerateDisplayName(string path)
    {
        var filename = System.IO.Path.GetFileNameWithoutExtension(path);
        // Replace underscores and hyphens with spaces
        filename = filename.Replace("_", " ").Replace("-", " ");
        // Title case
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(filename.ToLower());
    }

    /// <summary>
    /// Extracts a preview color from an image (center pixel).
    /// </summary>
    private static Color ExtractPreviewColor(string path)
    {
        try
        {
            var image = Image.LoadFromFile(path);
            if (image != null)
            {
                // Get center pixel
                int centerX = image.GetWidth() / 2;
                int centerY = image.GetHeight() / 2;
                return image.GetPixel(centerX, centerY);
            }
        }
        catch
        {
            // Ignore errors
        }
        return Colors.Gray;
    }

    private static bool IsImageFile(string path)
    {
        var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".webp";
    }
}
