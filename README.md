# Hexographer

A hex tile map editor built with Godot 4.5 and C#, designed for creating world maps and tactical game boards.

## Features

- **Dual hex orientations** - Pointy-top and flat-top hex support
- **Multi-layer editing** - Ground, Features, and Objects layers
- **Multiple brush tools** - Paint, erase, fill, line, and area brushes
- **Custom tile types** - Create and import your own tiles
- **PNG export** - Export maps as transparent PNG images
- **Undo/redo** - Full history with up to 100 actions
- **JSON save format** - Human-readable map files with embedded custom tiles

## Installation

1. Clone this repository
2. Open the project in Godot 4.5 or later
3. Build the C# solution
4. Run the project

## Controls

### Mouse

| Action                 | Control                               |
| ---------------------- | ------------------------------------- |
| Paint/Use tool         | Left click / Left drag                |
| Pan camera             | Middle mouse drag                     |
| Zoom                   | Scroll wheel                          |
| Toggle area brush mode | Right click (while Area brush active) |

### Keyboard Shortcuts

#### File Operations

| Shortcut     | Action      |
| ------------ | ----------- |
| Ctrl+N       | New map     |
| Ctrl+O       | Open map    |
| Ctrl+S       | Save map    |
| Ctrl+Shift+S | Save as     |
| Ctrl+Z       | Undo        |
| Ctrl+Y       | Redo        |
| Ctrl+G       | Toggle grid |

#### Tools

| Key | Tool          |
| --- | ------------- |
| B   | Brush (paint) |
| E   | Eraser        |
| F   | Fill          |
| L   | Line          |
| A   | Area          |

#### Layers

| Key | Layer    |
| --- | -------- |
| 1   | Ground   |
| 2   | Features |
| 3   | Objects  |

#### Quick Tiles

| Key | Tile   |
| --- | ------ |
| G   | Grass  |
| W   | Water  |
| S   | Sand   |
| D   | Dirt   |
| R   | Road   |
| T   | Forest |

## Layers

The editor uses a 3-layer system:

| Layer        | Purpose                | Examples                          |
| ------------ | ---------------------- | --------------------------------- |
| **Ground**   | Base terrain           | Grass, water, sand, stone, snow   |
| **Features** | Environmental elements | Forests, roads, mountains, rivers |
| **Objects**  | Placeables             | Towns, castles, ruins, caves      |

Each hex can have a different tile on each layer, allowing for rich compositions like a road over grass with a town on top.

## Brush Tools

### Brush (B)

Paint individual tiles. Click to place a single tile, or drag to paint continuously.

### Eraser (E)

Remove tiles from the active layer. Works the same as the brush but clears tiles instead.

### Fill (F)

Flood fill an area of contiguous matching tiles. Previews the fill area before clicking.

### Line (L)

Draw a line between two points. Click to start, drag to preview, release to paint.

### Area (A)

Fill rectangular or circular areas. Click to start, drag to set size, release to paint. Right-click to toggle between rectangle and hexagonal circle modes.

## Custom Tiles

### Creating Tiles

1. Click the **+** button in the tile palette
2. Enter an ID (lowercase, underscores allowed)
3. Set the display name, layer, and category
4. Choose a preview color or select a texture file
5. Click Save

### Batch Import

1. Click the **Batch** button in the tile palette
2. Add image files or folders
3. Set the target layer and category
4. Set the pixel size (hex width in the source images)
5. Click Import All

Custom tiles are saved globally and persist across sessions.

## File Formats

### Map Files (.json)

Maps are saved as JSON with the following structure:

- Map metadata (name, author, description, timestamps)
- Grid settings (orientation, hex size)
- Tile data (coordinates and layer contents)
- Embedded custom tile definitions

### PNG Export

Export your map as a PNG image via **File > Export as PNG**. The export:

- Includes all visible tiles
- Uses a transparent background
- Automatically sizes to fit all content
- Excludes grid lines and UI elements

## Default Tiles

The editor includes 27 built-in tile types:

**Ground:** Grass, Tall Grass, Dirt, Sand, Stone, Snow, Shallow Water, Water, Deep Water

**Features:** Forest, Dense Forest, Hills, Mountain, Road, Bridge, River

**Objects:** Town, City, Castle, Ruins, Tower, Cave, Shrine

Each tile has properties for movement cost, movement blocking, and vision blocking for game integration.

## Project Structure

```
Scripts/
  Core/
    Data/        - HexGrid, TileRegistry, TileType
    Hex/         - HexCoord, HexLayout, HexOrientation
    Serialization/ - MapSerializer, file formats
  Editor/
    Brushes/     - Brush tools (IBrush implementations)
    UI/          - Editor UI components
    UndoRedo/    - Undo/redo system
  Rendering/     - HexGridRenderer, layer renderers, overlays
```
