using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.UI;

/// <summary>
/// Displays current editor state information in a status bar.
/// </summary>
public partial class StatusBar : HBoxContainer
{
    private Label _coordLabel = null!;
    private Label _tileLabel = null!;
    private Label _brushLabel = null!;
    private Label _zoomLabel = null!;

    public override void _Ready()
    {
        // Create coordinate display
        _coordLabel = new Label { Text = "Hex: --" };
        _coordLabel.AddThemeStyleboxOverride("normal", CreateStatusBoxStyle());
        _coordLabel.CustomMinimumSize = new Vector2(120, 0);
        AddChild(_coordLabel);

        AddChild(CreateSeparator());

        // Create tile display
        _tileLabel = new Label { Text = "Tile: --" };
        _tileLabel.AddThemeStyleboxOverride("normal", CreateStatusBoxStyle());
        _tileLabel.CustomMinimumSize = new Vector2(150, 0);
        AddChild(_tileLabel);

        AddChild(CreateSeparator());

        // Create brush display
        _brushLabel = new Label { Text = "Brush: --" };
        _brushLabel.AddThemeStyleboxOverride("normal", CreateStatusBoxStyle());
        _brushLabel.CustomMinimumSize = new Vector2(120, 0);
        AddChild(_brushLabel);

        AddChild(CreateSeparator());

        // Create zoom display
        _zoomLabel = new Label { Text = "Zoom: 100%" };
        _zoomLabel.AddThemeStyleboxOverride("normal", CreateStatusBoxStyle());
        _zoomLabel.CustomMinimumSize = new Vector2(100, 0);
        AddChild(_zoomLabel);

        // Spacer to push content to left
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        AddChild(spacer);
    }

    private static StyleBoxFlat CreateStatusBoxStyle()
    {
        return new StyleBoxFlat
        {
            ContentMarginLeft = 8,
            ContentMarginRight = 8,
            ContentMarginTop = 4,
            ContentMarginBottom = 4
        };
    }

    private static VSeparator CreateSeparator()
    {
        return new VSeparator();
    }

    /// <summary>
    /// Updates the coordinate display.
    /// </summary>
    public void UpdateCoordinates(HexCoord? coord)
    {
        _coordLabel.Text = coord.HasValue
            ? $"Hex: ({coord.Value.Q}, {coord.Value.R})"
            : "Hex: --";
    }

    /// <summary>
    /// Updates the selected tile display.
    /// </summary>
    public void UpdateSelectedTile(string? tileId, string? displayName = null)
    {
        if (string.IsNullOrEmpty(tileId))
        {
            _tileLabel.Text = "Tile: --";
        }
        else
        {
            _tileLabel.Text = $"Tile: {displayName ?? tileId}";
        }
    }

    /// <summary>
    /// Updates the active brush display.
    /// </summary>
    public void UpdateActiveBrush(string? brushName)
    {
        _brushLabel.Text = $"Brush: {brushName ?? "--"}";
    }

    /// <summary>
    /// Updates the zoom level display.
    /// </summary>
    public void UpdateZoom(float level)
    {
        _zoomLabel.Text = $"Zoom: {level * 100:F0}%";
    }
}
