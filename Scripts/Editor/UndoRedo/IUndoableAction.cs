namespace Hexographer.Editor.UndoRedo;

/// <summary>
/// Interface for actions that can be undone and redone.
/// </summary>
public interface IUndoableAction
{
    /// <summary>
    /// Human-readable description of the action (e.g., "Paint 5 tiles", "Fill area").
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Reverts the action, restoring the previous state.
    /// </summary>
    void Undo();

    /// <summary>
    /// Re-applies the action after it was undone.
    /// </summary>
    void Redo();
}
