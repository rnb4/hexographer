# Hex Tile Map Editor - Implementation Plan

## Overview
A hex tile map editor for Godot 4.5 with C#, supporting both flat-top and pointy-top hex orientations, multiple terrain layers, various brush tools, and JSON save/load. Primary use case: World/overworld RPG-style maps.

---

## Phase 1: Core Hex Math

### Files to Create
- `Scripts/Core/Hex/HexCoord.cs` - Immutable struct for axial coordinates (q, r) with derived cube coordinate (s = -q - r)
- `Scripts/Core/Hex/HexOrientation.cs` - Enum and layout utilities for flat-top/pointy-top
- `Scripts/Core/Hex/HexMath.cs` - Distance, neighbors, line drawing, ring/spiral algorithms

### Key Algorithms
- **Coordinate Systems**: Cube (q,r,s) for math, Axial (q,r) for storage, Offset (col,row) for screen
- **Pixel ↔ Hex conversion** for both orientations
- **Hex rounding** for fractional coordinates

---

## Phase 2: Data Structures

### Files to Create
- `Scripts/Core/Data/HexTile.cs` - Single hex with layers array and metadata
- `Scripts/Core/Data/HexGrid.cs` - Dictionary-based sparse grid storage
- `Scripts/Core/Data/TileType.cs` - Tile definition (id, layer index, texture path, properties)

### Layer System
| Index | Layer    | Purpose                          |
|-------|----------|----------------------------------|
| 0     | Ground   | Base terrain (grass, water, sand)|
| 1     | Features | Environmental (forests, roads)   |
| 2     | Objects  | Placeables (buildings, POIs)     |

---

## Phase 3: Rendering

### Files to Create
- `Scripts/Rendering/TileRegistry.cs` - Loads tile definitions and textures
- `Scripts/Rendering/HexGridRenderer.cs` - Main renderer, manages layer renderers
- `Scripts/Rendering/HexLayerRenderer.cs` - Per-layer sprite management
- `Scripts/Rendering/HexGridOverlay.cs` - Grid lines, hover highlight, selection

### Approach
- Separate Node2D per layer for z-ordering and visibility toggle
- Dictionary of Sprite2D per tile for efficient updates
- Custom `_Draw()` for grid overlay

---

## Phase 4: Brush System

### Files to Create
- `Scripts/Editor/Brushes/IBrush.cs` - Strategy interface
- `Scripts/Editor/Brushes/BrushManager.cs` - Brush registration and switching
- `Scripts/Editor/Brushes/SingleTileBrush.cs` - Paint one tile, drag for continuous
- `Scripts/Editor/Brushes/FillBrush.cs` - Flood fill using BFS
- `Scripts/Editor/Brushes/LineBrush.cs` - Click-drag line drawing
- `Scripts/Editor/Brushes/AreaBrush.cs` - Rectangle/hex circle selection
- `Scripts/Editor/Brushes/EraserBrush.cs` - Clear tiles on active layer
- `Scripts/Editor/EditorContext.cs` - Shared state for brushes

---

## Phase 5: Undo/Redo

### Files to Create
- `Scripts/Editor/UndoRedo/IUndoableAction.cs` - Action interface
- `Scripts/Editor/UndoRedo/UndoRedoManager.cs` - Stack-based history (max 100)
- `Scripts/Editor/UndoRedo/Actions/TilePaintAction.cs` - Stores before/after tile state

---

## Phase 6: JSON Serialization

### Files to Create
- `Scripts/Core/Serialization/MapFileFormat.cs` - Data classes for JSON schema
- `Scripts/Core/Serialization/MapSerializer.cs` - Save/load logic

### JSON Schema
```json
{
  "version": "1.0",
  "metadata": { "name": "", "author": "", "created": "", "description": "" },
  "settings": { "orientation": "pointy_top", "hexSize": 64, "layerCount": 3 },
  "tileTypes": { "grass": { "layer": 0, "color": "#4a7c23", "texture": "..." } },
  "tiles": [{ "q": 0, "r": 0, "layers": ["grass", null, null], "elevation": 0 }]
}
```

---

## Phase 7: UI

### Scene Structure
```
EditorRoot (Control)
├── MenuBar - File, Edit, View, Tools
├── Toolbar - Brush buttons, Undo/Redo, Zoom
├── HSplitContainer
│   ├── TilePalette - Scrollable tile grid by layer
│   ├── Viewport - SubViewportContainer with Camera2D
│   └── LayerPanel - Visibility toggles, active layer
└── StatusBar - Coordinates, current tile, zoom level
```

### Files to Create
- `Scenes/Editor/EditorRoot.tscn` + `EditorRoot.cs`
- `Scenes/Editor/UI/MenuBar.tscn`, `Toolbar.tscn`, `TilePalette.tscn`, `LayerPanel.tscn`
- `Scripts/Editor/ViewportController.cs` - Pan (middle-mouse/space+drag), zoom (scroll wheel)

---

## Phase 8: Main Editor Controller

### File to Create
- `Scripts/Editor/MapEditor.cs` - Orchestrates all systems

### Responsibilities
- Initialize grid, renderer, brushes, undo system
- Handle input routing to active brush
- File operations (New, Open, Save, Save As)
- Keyboard shortcuts (Ctrl+Z, Ctrl+Y, B, E, F, L, etc.)

---

## Project Structure

```
Hexographer/
├── Assets/
│   ├── Textures/Terrain/, Features/, Objects/, UI/
│   └── Themes/editor_theme.tres
├── Scenes/
│   ├── Main/main.tscn (entry point, loads EditorRoot)
│   └── Editor/EditorRoot.tscn, UI/...
├── Scripts/
│   ├── Core/Hex/, Data/, Serialization/
│   ├── Rendering/
│   ├── Editor/Brushes/, UndoRedo/, UI/
│   └── Utils/
└── Resources/TileDefinitions/default_tiles.json
```

---

## Implementation Order

1. **Core Math** - HexCoord, HexOrientation, HexMath
2. **Data** - HexTile, HexGrid, TileType
3. **Rendering** - TileRegistry, HexGridRenderer (basic colored hexes first)
4. **Viewport** - Camera pan/zoom, mouse-to-hex
5. **Brushes** - SingleTileBrush, EraserBrush (MVP editing)
6. **Serialization** - Save/Load JSON maps
7. **Undo/Redo** - History system
8. **UI** - Palette, layer panel, toolbar, menus
9. **Advanced Brushes** - Fill, Line, Area
10. **Polish** - Dialogs, settings, keyboard shortcuts

---

## Verification Plan

1. **Unit test hex math** - Coordinate conversions, distance, neighbors, line drawing
2. **Visual test rendering** - Create 10x10 grid, verify both orientations display correctly
3. **Interactive test brushes** - Paint tiles, verify undo/redo restores state
4. **Round-trip test serialization** - Save map, reload, verify identical
5. **Full workflow test** - Create new map → paint terrain → add features → save → reload → edit → save

---

## Key Algorithms Reference

### Hex Distance
```csharp
public static int Distance(HexCoord a, HexCoord b)
{
	return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
}
```

### Hex Neighbors
```csharp
private static readonly HexCoord[] Directions = {
	new(1, 0), new(1, -1), new(0, -1),
	new(-1, 0), new(-1, 1), new(0, 1)
};

public static IEnumerable<HexCoord> Neighbors(HexCoord center)
{
	return Directions.Select(d => new HexCoord(center.Q + d.Q, center.R + d.R));
}
```

### Pixel to Hex (Pointy-Top)
```csharp
public static HexCoord PixelToHex(Vector2 pixel, float size)
{
	float q = (Mathf.Sqrt(3f)/3f * pixel.X - 1f/3f * pixel.Y) / size;
	float r = (2f/3f * pixel.Y) / size;
	return HexRound(q, r);
}
```

### Flood Fill
```csharp
public static IEnumerable<HexCoord> FloodFill(HexGrid grid, HexCoord start, int layer)
{
	string targetType = grid.GetTile(start)?.Layers[layer];
	var visited = new HashSet<HexCoord>();
	var queue = new Queue<HexCoord>();
	queue.Enqueue(start);

	while (queue.Count > 0)
	{
		var current = queue.Dequeue();
		if (visited.Contains(current)) continue;

		var tile = grid.GetTile(current);
		if (tile?.Layers[layer] != targetType) continue;

		visited.Add(current);
		foreach (var neighbor in current.Neighbors())
		{
			if (!visited.Contains(neighbor))
				queue.Enqueue(neighbor);
		}
	}
	return visited;
}
```
