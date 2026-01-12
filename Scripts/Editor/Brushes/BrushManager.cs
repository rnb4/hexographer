using System;
using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Manages brush registration, switching, and routes input to the active brush.
/// </summary>
public class BrushManager
{
    private readonly Dictionary<string, IBrush> _brushes = new();
    private IBrush? _activeBrush;
    private EditorContext _context = null!;
    private HexCoord? _lastHoveredCoord;

    /// <summary>
    /// Event fired when the active brush changes.
    /// </summary>
    public event Action<IBrush?>? BrushChanged;

    /// <summary>
    /// The currently active brush.
    /// </summary>
    public IBrush? ActiveBrush => _activeBrush;

    /// <summary>
    /// The name of the currently active brush.
    /// </summary>
    public string? ActiveBrushName => _activeBrush?.Name;

    /// <summary>
    /// All registered brushes.
    /// </summary>
    public IEnumerable<IBrush> AllBrushes => _brushes.Values;

    /// <summary>
    /// Initializes the brush manager with an editor context.
    /// </summary>
    public void Initialize(EditorContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Registers a brush with the manager.
    /// </summary>
    public void RegisterBrush(IBrush brush)
    {
        _brushes[brush.Name] = brush;
    }

    /// <summary>
    /// Registers all default brushes.
    /// </summary>
    public void RegisterDefaultBrushes()
    {
        RegisterBrush(new SingleTileBrush());
        RegisterBrush(new EraserBrush());
        RegisterBrush(new FillBrush());
        RegisterBrush(new LineBrush());
        RegisterBrush(new AreaBrush());
    }

    /// <summary>
    /// Sets the active brush by name.
    /// </summary>
    public bool SetActiveBrush(string name)
    {
        if (!_brushes.TryGetValue(name, out var brush))
            return false;

        if (_activeBrush == brush)
            return true;

        // Clear preview before switching to avoid lingering highlights
        _context.ClearPreview();

        // Deactivate current brush
        _activeBrush?.OnDeactivate();

        // Activate new brush
        _activeBrush = brush;
        _activeBrush.OnActivate(_context);

        BrushChanged?.Invoke(_activeBrush);

        // Update preview for current position
        if (_lastHoveredCoord.HasValue)
        {
            UpdatePreview(_lastHoveredCoord.Value);
        }

        return true;
    }

    /// <summary>
    /// Gets a brush by name.
    /// </summary>
    public IBrush? GetBrush(string name)
    {
        return _brushes.GetValueOrDefault(name);
    }

    /// <summary>
    /// Handles mouse button press events.
    /// </summary>
    public bool HandleMouseDown(HexCoord coord, MouseButton button)
    {
        return _activeBrush?.OnMouseDown(coord, button) ?? false;
    }

    /// <summary>
    /// Handles mouse move events.
    /// </summary>
    public bool HandleMouseMove(HexCoord? coord)
    {
        _lastHoveredCoord = coord;
        return _activeBrush?.OnMouseMove(coord) ?? false;
    }

    /// <summary>
    /// Handles mouse button release events.
    /// </summary>
    public bool HandleMouseUp(HexCoord coord, MouseButton button)
    {
        return _activeBrush?.OnMouseUp(coord, button) ?? false;
    }

    /// <summary>
    /// Updates the preview highlight based on the current hover position.
    /// </summary>
    public void UpdatePreview(HexCoord coord)
    {
        if (_activeBrush != null)
        {
            var previewCoords = _activeBrush.GetPreviewCoords(coord);
            _context.SetPreview(previewCoords);
        }
    }

    /// <summary>
    /// Clears the preview highlight.
    /// </summary>
    public void ClearPreview()
    {
        _context.ClearPreview();
    }
}
