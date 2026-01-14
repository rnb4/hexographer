namespace Hexographer.Editor.Brushes;

using System.Collections.Generic;
using Core.Hex;
using Godot;
using Scripts.Editor.Actions;

public class RotateBrush : BaseBrush
{
    public override string Name => "Rotate";
    public override string Description => "Rotates an a tile.";
    
    public override bool OnMouseDown(HexCoord coord, MouseButton button)
    {
        // TODO: Make tile rotations more flexible
        switch (button)
        {
            case MouseButton.Left:
                RecordUndoAction(coord, 60);
                Context.RotateTile(coord, 60);
                return true;
            case MouseButton.Right:
                RecordUndoAction(coord, -60);
                Context.RotateTile(coord, -60);
                return true;
            default:
                return false;
        }
    }

    public void RecordUndoAction(HexCoord coord, float degrees)
    {
        Dictionary<HexCoord, float> before = new();
        before.Add(coord, -degrees);
        
        Dictionary<HexCoord, float> after = new();
        after.Add(coord, degrees);

        var action = new TileRotateAction(
            Context.Grid,
            Context.ActiveLayer,
            before,
            after,
            "Rotate tile");
        
        Context.RecordUndoAction(action);
    }
}