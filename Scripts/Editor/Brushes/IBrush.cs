using System.Collections.Generic;
using Godot;
using Hexographer.Core.Hex;

namespace Hexographer.Editor.Brushes;

/// <summary>
/// Strategy interface defining brush behavior contract.
/// </summary>
public interface IBrush
{
    /// <summary>
    /// The display name of the brush.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// A description of what the brush does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Called when this brush becomes the active brush.
    /// </summary>
    void OnActivate(EditorContext context);

    /// <summary>
    /// Called when this brush is no longer the active brush.
    /// </summary>
    void OnDeactivate();

    /// <summary>
    /// Called when a mouse button is pressed.
    /// </summary>
    /// <param name="coord">The hex coordinate under the mouse.</param>
    /// <param name="button">The mouse button that was pressed.</param>
    /// <returns>True if the input was handled.</returns>
    bool OnMouseDown(HexCoord coord, MouseButton button);

    /// <summary>
    /// Called when the mouse moves.
    /// </summary>
    /// <param name="coord">The hex coordinate under the mouse, or null if outside the grid.</param>
    /// <returns>True if the input was handled.</returns>
    bool OnMouseMove(HexCoord? coord);

    /// <summary>
    /// Called when a mouse button is released.
    /// </summary>
    /// <param name="coord">The hex coordinate under the mouse.</param>
    /// <param name="button">The mouse button that was released.</param>
    /// <returns>True if the input was handled.</returns>
    bool OnMouseUp(HexCoord coord, MouseButton button);

    /// <summary>
    /// Gets the coordinates that should be previewed for the given hover position.
    /// </summary>
    IEnumerable<HexCoord> GetPreviewCoords(HexCoord coord);
}

/// <summary>
/// Abstract base class providing common functionality for brushes.
/// </summary>
public abstract class BaseBrush : IBrush
{
    /// <summary>
    /// The editor context providing access to grid, registry, and state.
    /// </summary>
    protected EditorContext Context { get; private set; } = null!;

    /// <summary>
    /// Whether this brush is currently active.
    /// </summary>
    protected bool IsActive { get; private set; }

    /// <summary>
    /// The display name of the brush.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// A description of what the brush does. Defaults to the name.
    /// </summary>
    public virtual string Description => Name;

    /// <summary>
    /// Called when this brush becomes the active brush.
    /// </summary>
    public virtual void OnActivate(EditorContext context)
    {
        Context = context;
        IsActive = true;
    }

    /// <summary>
    /// Called when this brush is no longer the active brush.
    /// </summary>
    public virtual void OnDeactivate()
    {
        IsActive = false;
        Context?.ClearPreview();
    }

    /// <summary>
    /// Called when a mouse button is pressed.
    /// </summary>
    public abstract bool OnMouseDown(HexCoord coord, MouseButton button);

    /// <summary>
    /// Called when the mouse moves. Default implementation does nothing.
    /// </summary>
    public virtual bool OnMouseMove(HexCoord? coord)
    {
        return false;
    }

    /// <summary>
    /// Called when a mouse button is released. Default implementation does nothing.
    /// </summary>
    public virtual bool OnMouseUp(HexCoord coord, MouseButton button)
    {
        return false;
    }

    /// <summary>
    /// Gets the coordinates that should be previewed. Default returns just the hovered coord.
    /// </summary>
    public virtual IEnumerable<HexCoord> GetPreviewCoords(HexCoord coord)
    {
        return new[] { coord };
    }
}
