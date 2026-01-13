using Godot;
using System;
using Hexographer.Core.Data;

namespace Hexographer.Editor.UI;

/// <summary>
/// Panel for selecting and toggling layer visibility.
/// </summary>
public partial class LayerPanel : VBoxContainer
{
    private readonly Button[] _layerButtons = new Button[TileLayers.LayerCount];
    private readonly CheckButton[] _visibilityToggles = new CheckButton[TileLayers.LayerCount];
    private int _activeLayer = TileLayers.Ground;

    /// <summary>
    /// Event fired when a layer is selected.
    /// </summary>
    public event Action<int>? LayerSelected;

    /// <summary>
    /// Event fired when layer visibility is toggled.
    /// </summary>
    public event Action<int, bool>? LayerVisibilityChanged;

    public override void _Ready()
    {
        // Header
        var header = new Label
        {
            Text = "Layers",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        header.AddThemeFontSizeOverride("font_size", 14);
        AddChild(header);

        AddChild(new HSeparator());

        // Create layer rows
        for (int i = 0; i < TileLayers.LayerCount; i++)
        {
            CreateLayerRow(i);
        }

        // Set initial selection
        UpdateActiveLayerVisuals();
    }

    private void CreateLayerRow(int layerIndex)
    {
        var row = new HBoxContainer();

        // Visibility toggle
        var visibility = new CheckButton
        {
            ButtonPressed = true,
            TooltipText = "Toggle visibility",
            CustomMinimumSize = new Vector2(32, 0)
        };
        var capturedIndex = layerIndex;
        visibility.Toggled += (pressed) => OnVisibilityToggled(capturedIndex, pressed);
        _visibilityToggles[layerIndex] = visibility;
        row.AddChild(visibility);

        // Layer button
        var button = new Button
        {
            Text = TileLayers.GetLayerName(layerIndex),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ToggleMode = true,
            ButtonPressed = layerIndex == _activeLayer
        };
        button.Pressed += () => OnLayerButtonPressed(capturedIndex);
        _layerButtons[layerIndex] = button;
        row.AddChild(button);

        AddChild(row);
    }

    private void OnLayerButtonPressed(int layerIndex)
    {
        if (layerIndex == _activeLayer)
        {
            // Keep it pressed if already active
            _layerButtons[layerIndex].ButtonPressed = true;
            return;
        }

        SetActiveLayer(layerIndex);
        LayerSelected?.Invoke(layerIndex);
    }

    private void OnVisibilityToggled(int layerIndex, bool visible)
    {
        LayerVisibilityChanged?.Invoke(layerIndex, visible);
    }

    /// <summary>
    /// Sets the active layer and updates visuals.
    /// </summary>
    public void SetActiveLayer(int layer)
    {
        if (layer < 0 || layer >= TileLayers.LayerCount)
            return;

        _activeLayer = layer;
        UpdateActiveLayerVisuals();
    }

    private void UpdateActiveLayerVisuals()
    {
        for (int i = 0; i < _layerButtons.Length; i++)
        {
            _layerButtons[i].ButtonPressed = (i == _activeLayer);
        }
    }

    /// <summary>
    /// Sets the visibility state for a layer (updates UI only).
    /// </summary>
    public void SetLayerVisibility(int layer, bool visible)
    {
        if (layer >= 0 && layer < _visibilityToggles.Length)
        {
            _visibilityToggles[layer].SetPressedNoSignal(visible);
        }
    }

    /// <summary>
    /// Gets the currently active layer.
    /// </summary>
    public int GetActiveLayer() => _activeLayer;
}
