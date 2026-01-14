using Godot;
using System;
using System.Collections.Generic;

namespace Hexographer.Editor.UI;

/// <summary>
/// Toolbar with brush selection, undo/redo, and zoom controls.
/// </summary>
public partial class Toolbar : HBoxContainer
{
    private readonly Dictionary<string, Button> _brushButtons = new();
    private Button _undoButton = null!;
    private Button _redoButton = null!;
    private HSlider _zoomSlider = null!;
    private Label _zoomLabel = null!;
    private string? _activeBrush;

    /// <summary>
    /// Event fired when a brush is selected.
    /// </summary>
    public event Action<string>? BrushSelected;

    /// <summary>
    /// Event fired when undo is pressed.
    /// </summary>
    public event Action? UndoPressed;

    /// <summary>
    /// Event fired when redo is pressed.
    /// </summary>
    public event Action? RedoPressed;

    /// <summary>
    /// Event fired when zoom slider changes.
    /// </summary>
    public event Action<float>? ZoomChanged;

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(0, 36);

        // Brush buttons
        AddBrushButton("Brush", "Paint (B)");
        AddBrushButton("Eraser", "Eraser (E)");
        AddBrushButton("Fill", "Fill (F)");
        AddBrushButton("Line", "Line (L)");
        AddBrushButton("Area", "Area (A)");
        AddBrushButton("Rotate", "Rotate (R)");

        AddChild(CreateSeparator());

        // Undo/Redo buttons
        _undoButton = new Button
        {
            Text = "Undo",
            TooltipText = "Undo (Ctrl+Z)",
            Disabled = true
        };
        _undoButton.Pressed += () => UndoPressed?.Invoke();
        AddChild(_undoButton);

        _redoButton = new Button
        {
            Text = "Redo",
            TooltipText = "Redo (Ctrl+Y)",
            Disabled = true
        };
        _redoButton.Pressed += () => RedoPressed?.Invoke();
        AddChild(_redoButton);

        AddChild(CreateSeparator());

        // Zoom controls
        var zoomContainer = new HBoxContainer();

        var zoomTextLabel = new Label { Text = "Zoom:" };
        zoomContainer.AddChild(zoomTextLabel);

        _zoomSlider = new HSlider
        {
            MinValue = 0.25,
            MaxValue = 4.0,
            Step = 0.05,
            Value = 1.0,
            CustomMinimumSize = new Vector2(100, 0)
        };
        _zoomSlider.ValueChanged += OnZoomSliderChanged;
        zoomContainer.AddChild(_zoomSlider);

        _zoomLabel = new Label
        {
            Text = "100%",
            CustomMinimumSize = new Vector2(50, 0)
        };
        zoomContainer.AddChild(_zoomLabel);

        AddChild(zoomContainer);

        // Spacer
        var spacer = new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        AddChild(spacer);

        // Set default brush
        HighlightBrush("Brush");
    }

    private void AddBrushButton(string brushName, string tooltip)
    {
        var button = new Button
        {
            Text = brushName,
            TooltipText = tooltip,
            ToggleMode = true
        };
        button.Pressed += () => OnBrushButtonPressed(brushName);
        _brushButtons[brushName] = button;
        AddChild(button);
    }

    private static VSeparator CreateSeparator()
    {
        return new VSeparator();
    }

    private void OnBrushButtonPressed(string brushName)
    {
        HighlightBrush(brushName);
        BrushSelected?.Invoke(brushName);
    }

    private void OnZoomSliderChanged(double value)
    {
        _zoomLabel.Text = $"{value * 100:F0}%";
        ZoomChanged?.Invoke((float)value);
    }

    /// <summary>
    /// Highlights the specified brush button.
    /// </summary>
    public void HighlightBrush(string? brushName)
    {
        _activeBrush = brushName;
        foreach (var (name, button) in _brushButtons)
        {
            button.ButtonPressed = (name == brushName);
        }
    }

    /// <summary>
    /// Sets whether undo is enabled.
    /// </summary>
    public void SetUndoEnabled(bool enabled)
    {
        _undoButton.Disabled = !enabled;
    }

    /// <summary>
    /// Sets whether redo is enabled.
    /// </summary>
    public void SetRedoEnabled(bool enabled)
    {
        _redoButton.Disabled = !enabled;
    }

    /// <summary>
    /// Updates the zoom slider and label (without firing event).
    /// </summary>
    public void SetZoomLevel(float level)
    {
        _zoomSlider.SetValueNoSignal(level);
        _zoomLabel.Text = $"{level * 100:F0}%";
    }

    /// <summary>
    /// Gets the currently active brush name.
    /// </summary>
    public string? GetActiveBrush() => _activeBrush;
}
