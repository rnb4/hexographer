namespace Hexographer.Scripts.Editor.Actions;

using System.Collections.Generic;
using Core.Data;
using Core.Hex;
using Hexographer.Editor.UndoRedo;

/// <summary>
/// Undoable action for tile painting/erasing operations.
/// Stores only the affected layer values for each coordinate.
/// </summary>
public class TilePaintAction : IUndoableAction
{
    private readonly HexGrid _grid;
    private readonly int _layer;
    private readonly Dictionary<HexCoord, string?> _beforeState;
    private readonly Dictionary<HexCoord, string?> _afterState;

    public string Description { get; }

    /// <summary>
    /// Creates a new tile paint action.
    /// </summary>
    /// <param name="grid">The grid to apply changes to.</param>
    /// <param name="layer">The layer that was modified.</param>
    /// <param name="beforeState">Tile type IDs at each coordinate before the operation.</param>
    /// <param name="afterState">Tile type IDs at each coordinate after the operation.</param>
    /// <param name="description">Human-readable description of the action.</param>
    public TilePaintAction(
        HexGrid grid,
        int layer,
        Dictionary<HexCoord, string?> beforeState,
        Dictionary<HexCoord, string?> afterState,
        string description)
    {
        _grid = grid;
        _layer = layer;
        _beforeState = beforeState;
        _afterState = afterState;
        Description = description;
    }

    /// <summary>
    /// Undoes the action by restoring the before state.
    /// </summary>
    public void Undo()
    {
        ApplyState(_beforeState);
    }

    /// <summary>
    /// Redoes the action by applying the after state.
    /// </summary>
    public void Redo()
    {
        ApplyState(_afterState);
    }

    /// <summary>
    /// Applies a state dictionary to the grid.
    /// </summary>
    private void ApplyState(Dictionary<HexCoord, string?> state)
    {
        _grid.SetTileLayerBatch(state, _layer);
    }

    /// <summary>
    /// Creates a tile paint action by capturing the current state of coordinates,
    /// executing an operation, then capturing the after state.
    /// </summary>
    /// <param name="grid">The grid to modify.</param>
    /// <param name="layer">The layer to modify.</param>
    /// <param name="coords">The coordinates that will be affected.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="description">Description of the action.</param>
    /// <returns>A new TilePaintAction, or null if no changes were made.</returns>
    public static TilePaintAction? CreateFromOperation(
        HexGrid grid,
        int layer,
        IEnumerable<HexCoord> coords,
        System.Action operation,
        string description)
    {
        // Capture before state
        var beforeState = CaptureState(grid, layer, coords);

        // Execute the operation
        operation();

        // Capture after state
        var afterState = CaptureState(grid, layer, beforeState.Keys);

        // Check if any actual changes were made
        bool hasChanges = false;
        foreach (var coord in beforeState.Keys)
        {
            if (beforeState[coord] != afterState[coord])
            {
                hasChanges = true;
                break;
            }
        }

        if (!hasChanges)
            return null;

        return new TilePaintAction(grid, layer, beforeState, afterState, description);
    }

    /// <summary>
    /// Captures the current tile type IDs for the specified coordinates on a layer.
    /// </summary>
    public static Dictionary<HexCoord, string?> CaptureState(
        HexGrid grid,
        int layer,
        IEnumerable<HexCoord> coords)
    {
        var state = new Dictionary<HexCoord, string?>();
        foreach (var coord in coords)
        {
            state[coord] = grid.GetTileLayer(coord, layer);
        }
        return state;
    }
}
