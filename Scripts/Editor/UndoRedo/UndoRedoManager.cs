using System;
using System.Collections.Generic;

namespace Hexographer.Editor.UndoRedo;

/// <summary>
/// Manages undo/redo stacks with configurable history limit.
/// </summary>
public class UndoRedoManager
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxHistorySize;

    /// <summary>
    /// Event fired when the history changes (action recorded, undo, redo, or clear).
    /// </summary>
    public event Action? HistoryChanged;

    /// <summary>
    /// Creates a new UndoRedoManager with the specified history limit.
    /// </summary>
    /// <param name="maxHistorySize">Maximum number of actions to keep in history. Default is 100.</param>
    public UndoRedoManager(int maxHistorySize = 100)
    {
        _maxHistorySize = maxHistorySize;
    }

    /// <summary>
    /// Whether there are actions that can be undone.
    /// </summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>
    /// Whether there are actions that can be redone.
    /// </summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// The number of actions in the undo stack.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// The number of actions in the redo stack.
    /// </summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Gets the description of the next action to undo, or null if nothing to undo.
    /// </summary>
    public string? NextUndoDescription => _undoStack.Count > 0 ? _undoStack.Peek().Description : null;

    /// <summary>
    /// Gets the description of the next action to redo, or null if nothing to redo.
    /// </summary>
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    /// <summary>
    /// Records a new action. Clears the redo stack and trims history if needed.
    /// </summary>
    public void RecordAction(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();

        // Trim history if over limit
        if (_undoStack.Count > _maxHistorySize)
        {
            TrimUndoStack();
        }

        HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Undoes the most recent action.
    /// </summary>
    /// <returns>True if an action was undone, false if nothing to undo.</returns>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
            return false;

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Redoes the most recently undone action.
    /// </summary>
    /// <returns>True if an action was redone, false if nothing to redo.</returns>
    public bool Redo()
    {
        if (_redoStack.Count == 0)
            return false;

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Clears all history (both undo and redo stacks).
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    /// <summary>
    /// Trims the undo stack to the maximum size by removing oldest actions.
    /// </summary>
    private void TrimUndoStack()
    {
        // Convert to array, trim, rebuild stack (preserving order)
        var actions = _undoStack.ToArray();
        _undoStack.Clear();

        // Actions are in reverse order (newest first), so we take from the beginning
        int keepCount = Math.Min(actions.Length, _maxHistorySize);
        for (int i = keepCount - 1; i >= 0; i--)
        {
            _undoStack.Push(actions[i]);
        }
    }
}
