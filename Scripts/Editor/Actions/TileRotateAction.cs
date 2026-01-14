namespace Hexographer.Scripts.Editor.Actions;

using System.Collections.Generic;
using Core.Data;
using Core.Hex;
using Hexographer.Editor.UndoRedo;

public class TileRotateAction : IUndoableAction
{
    private readonly HexGrid _grid;
    private readonly int _layer;
    private readonly Dictionary<HexCoord, float> _beforeState;
    private readonly Dictionary<HexCoord, float> _afterState;
    
    public string Description { get; }

    public TileRotateAction(
        HexGrid grid,
        int layer,
        Dictionary<HexCoord, float> beforeState,
        Dictionary<HexCoord, float> afterState,
        string description
    )
    {
        _grid = grid;
        _layer = layer;
        _beforeState = beforeState;
        _afterState = afterState;
        Description = description;
    }
    
    public void Undo()
    {
        ApplyState(_beforeState);
    }

    public void Redo()
    {
        ApplyState(_afterState);
    }

    public void ApplyState(Dictionary<HexCoord, float> state)
    {
       _grid.RotateTileLayerBatch(state, _layer); 
    }
}