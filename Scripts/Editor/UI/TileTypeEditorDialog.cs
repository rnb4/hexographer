using Godot;
using System;
using System.Text.RegularExpressions;
using Hexographer.Core.Data;

namespace Hexographer.Editor.UI;

/// <summary>
/// Modal dialog for creating and editing tile types.
/// </summary>
public partial class TileTypeEditorDialog : Window
{
    // Form fields
    private LineEdit _idEdit = null!;
    private LineEdit _displayNameEdit = null!;
    private OptionButton _layerOption = null!;
    private LineEdit _categoryEdit = null!;
    private ColorPickerButton _colorPicker = null!;
    private LineEdit _texturePathEdit = null!;
    private Button _browseButton = null!;
    private Button _clearTextureButton = null!;
    private SpinBox _pixelSizeSpinBox = null!;
    private Control _previewContainer = null!;
    private Button _saveButton = null!;
    private Button _deleteButton = null!;
    private FileDialog _textureFileDialog = null!;

    // State
    private TileType? _editingTileType;
    private bool _isNewTile;
    private Func<string, bool>? _idValidator;

    /// <summary>
    /// Event fired when a tile type is saved.
    /// </summary>
    public event Action<TileType>? TileTypeSaved;

    /// <summary>
    /// Event fired when a tile type is deleted.
    /// </summary>
    public event Action<string>? TileTypeDeleted;

    public override void _Ready()
    {
        Title = "Tile Type Editor";
        Size = new Vector2I(400, 500);
        Exclusive = true;
        Visible = false;

        BuildUI();
        CloseRequested += Hide;
    }

    private void BuildUI()
    {
        var vbox = new VBoxContainer();
        vbox.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        vbox.AddThemeConstantOverride("separation", 8);
        AddChild(vbox);

        // Margins
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 16);
        margin.AddThemeConstantOverride("margin_right", 16);
        margin.AddThemeConstantOverride("margin_top", 16);
        margin.AddThemeConstantOverride("margin_bottom", 16);
        margin.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        vbox.AddChild(margin);

        var innerVbox = new VBoxContainer();
        innerVbox.AddThemeConstantOverride("separation", 8);
        margin.AddChild(innerVbox);

        // Form grid
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 8);
        grid.AddThemeConstantOverride("v_separation", 8);
        innerVbox.AddChild(grid);

        // ID field
        grid.AddChild(new Label { Text = "ID:" });
        _idEdit = new LineEdit { PlaceholderText = "my_tile", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _idEdit.TextChanged += _ => ValidateForm();
        grid.AddChild(_idEdit);

        // Display Name field
        grid.AddChild(new Label { Text = "Display Name:" });
        _displayNameEdit = new LineEdit { PlaceholderText = "My Tile", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _displayNameEdit.TextChanged += _ => ValidateForm();
        grid.AddChild(_displayNameEdit);

        // Layer field
        grid.AddChild(new Label { Text = "Layer:" });
        _layerOption = new OptionButton { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _layerOption.AddItem("Ground", TileLayers.Ground);
        _layerOption.AddItem("Features", TileLayers.Features);
        _layerOption.AddItem("Objects", TileLayers.Objects);
        grid.AddChild(_layerOption);

        // Category field
        grid.AddChild(new Label { Text = "Category:" });
        _categoryEdit = new LineEdit { PlaceholderText = "Custom", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        grid.AddChild(_categoryEdit);

        // Preview Color field
        grid.AddChild(new Label { Text = "Preview Color:" });
        _colorPicker = new ColorPickerButton { Color = Colors.Gray, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _colorPicker.ColorChanged += _ => UpdatePreview();
        grid.AddChild(_colorPicker);

        // Texture field
        grid.AddChild(new Label { Text = "Texture:" });
        var textureRow = new HBoxContainer();
        _texturePathEdit = new LineEdit
        {
            Editable = false,
            PlaceholderText = "(none)",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        textureRow.AddChild(_texturePathEdit);

        _browseButton = new Button { Text = "Browse..." };
        _browseButton.Pressed += OnBrowseTexture;
        textureRow.AddChild(_browseButton);

        _clearTextureButton = new Button { Text = "Clear" };
        _clearTextureButton.Pressed += OnClearTexture;
        textureRow.AddChild(_clearTextureButton);

        grid.AddChild(textureRow);

        // Pixel Size field (for texture scaling)
        grid.AddChild(new Label { Text = "Pixel Size:" });
        _pixelSizeSpinBox = new SpinBox
        {
            MinValue = 16,
            MaxValue = 512,
            Step = 1,
            Value = 64,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            TooltipText = "Width of the hex in the source texture (in pixels)"
        };
        grid.AddChild(_pixelSizeSpinBox);

        innerVbox.AddChild(new HSeparator());

        // Preview section
        innerVbox.AddChild(new Label { Text = "Preview:" });
        _previewContainer = new CenterContainer
        {
            CustomMinimumSize = new Vector2(0, 80)
        };
        innerVbox.AddChild(_previewContainer);

        innerVbox.AddChild(new HSeparator());

        // Buttons
        var buttonRow = new HBoxContainer();
        buttonRow.AddThemeConstantOverride("separation", 8);
        innerVbox.AddChild(buttonRow);

        _deleteButton = new Button { Text = "Delete", Visible = false };
        _deleteButton.Pressed += OnDelete;
        buttonRow.AddChild(_deleteButton);

        var spacer = new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        buttonRow.AddChild(spacer);

        _saveButton = new Button { Text = "Save" };
        _saveButton.Pressed += OnSave;
        buttonRow.AddChild(_saveButton);

        var cancelButton = new Button { Text = "Cancel" };
        cancelButton.Pressed += Hide;
        buttonRow.AddChild(cancelButton);

        // File dialog for texture selection
        _textureFileDialog = new FileDialog
        {
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Access = FileDialog.AccessEnum.Filesystem,
            Filters = new[] { "*.png, *.jpg, *.jpeg, *.webp ; Image Files" },
            Title = "Select Texture",
            Size = new Vector2I(700, 500)
        };
        _textureFileDialog.FileSelected += OnTextureSelected;
        AddChild(_textureFileDialog);

        UpdatePreview();
    }

    /// <summary>
    /// Shows the dialog to create a new tile type.
    /// </summary>
    public void ShowNew(Func<string, bool>? idValidator = null)
    {
        _isNewTile = true;
        _editingTileType = null;
        _idValidator = idValidator;

        Title = "New Tile Type";
        _idEdit.Text = "";
        _idEdit.Editable = true;
        _displayNameEdit.Text = "";
        _layerOption.Selected = TileLayers.Ground;
        _categoryEdit.Text = "Custom";
        _colorPicker.Color = Colors.Gray;
        _texturePathEdit.Text = "";
        _pixelSizeSpinBox.Value = 64;
        _deleteButton.Visible = false;

        UpdatePreview();
        ValidateForm();
        PopupCentered();
    }

    /// <summary>
    /// Shows the dialog to edit an existing tile type.
    /// </summary>
    public void ShowEdit(TileType tile, Func<string, bool>? idValidator = null)
    {
        _isNewTile = false;
        _editingTileType = tile;
        _idValidator = idValidator;

        Title = $"Edit Tile: {tile.DisplayName}";
        _idEdit.Text = tile.Id;
        _idEdit.Editable = false; // Can't change ID of existing tile
        _displayNameEdit.Text = tile.DisplayName;
        _layerOption.Selected = tile.LayerIndex;
        _categoryEdit.Text = tile.Category;
        _colorPicker.Color = tile.PreviewColor;
        _texturePathEdit.Text = tile.TexturePath;
        _pixelSizeSpinBox.Value = tile.PixelSize;
        _deleteButton.Visible = true;

        UpdatePreview();
        ValidateForm();
        PopupCentered();
    }

    private void OnBrowseTexture()
    {
        _textureFileDialog.PopupCentered();
    }

    private void OnTextureSelected(string path)
    {
        _texturePathEdit.Text = path;
        UpdatePreview();
    }

    private void OnClearTexture()
    {
        _texturePathEdit.Text = "";
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        // Clear existing preview
        foreach (var child in _previewContainer.GetChildren())
        {
            child.QueueFree();
        }

        // Create preview based on texture or color
        if (!string.IsNullOrEmpty(_texturePathEdit.Text))
        {
            var texture = LoadTextureFromPath(_texturePathEdit.Text);
            if (texture != null)
            {
                var textureRect = new TextureRect
                {
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    CustomMinimumSize = new Vector2(64, 64)
                };
                _previewContainer.AddChild(textureRect);
                return;
            }
        }

        // Fallback to color preview
        var colorRect = new ColorRect
        {
            Color = _colorPicker.Color,
            CustomMinimumSize = new Vector2(64, 64)
        };
        _previewContainer.AddChild(colorRect);
    }

    /// <summary>
    /// Loads a texture from a filesystem path (not a Godot resource path).
    /// </summary>
    private static Texture2D? LoadTextureFromPath(string path)
    {
        try
        {
            var image = Image.LoadFromFile(path);
            if (image == null)
                return null;

            var texture = ImageTexture.CreateFromImage(image);
            return texture;
        }
        catch
        {
            return null;
        }
    }

    private void ValidateForm()
    {
        bool valid = true;

        // Check ID
        var id = _idEdit.Text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            valid = false;
        }
        else if (!Regex.IsMatch(id, @"^[a-zA-Z][a-zA-Z0-9_]*$"))
        {
            valid = false;
        }
        else if (_isNewTile && _idValidator != null && !_idValidator(id))
        {
            valid = false; // ID already exists
        }

        // Check display name
        if (string.IsNullOrEmpty(_displayNameEdit.Text.Trim()))
        {
            valid = false;
        }

        _saveButton.Disabled = !valid;
    }

    private void OnSave()
    {
        var tile = _editingTileType ?? new TileType();

        tile.Id = _idEdit.Text.Trim();
        tile.DisplayName = _displayNameEdit.Text.Trim();
        tile.LayerIndex = _layerOption.Selected;
        tile.Category = string.IsNullOrEmpty(_categoryEdit.Text.Trim()) ? "Custom" : _categoryEdit.Text.Trim();
        tile.PreviewColor = _colorPicker.Color;
        tile.TexturePath = _texturePathEdit.Text;
        tile.PixelSize = (int)_pixelSizeSpinBox.Value;

        TileTypeSaved?.Invoke(tile);
        Hide();
    }

    private void OnDelete()
    {
        if (_editingTileType == null)
            return;

        // Show confirmation
        var confirm = new ConfirmationDialog
        {
            Title = "Delete Tile Type",
            DialogText = $"Are you sure you want to delete '{_editingTileType.DisplayName}'?\n\nTiles of this type on the map will become empty.",
            OkButtonText = "Delete"
        };
        confirm.Confirmed += () =>
        {
            TileTypeDeleted?.Invoke(_editingTileType.Id);
            Hide();
            confirm.QueueFree();
        };
        confirm.Canceled += () => confirm.QueueFree();
        AddChild(confirm);
        confirm.PopupCentered();
    }
}
