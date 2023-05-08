# Hexagon Grid Node

## Description
Generates a hexagonal grid based on input **UV**. Control the scale of the grid with input **Scale**. In addition to grid tile outlines, the Hexagon Grid node also outputs the distance from edges, and a unique value or ID for each tile.

Since aliasing can be a concern, the node provides three levels of samples. One sample gives the best performance, but aliasing artifacts may be visible. Using Four or Nine samples will reduce aliasing, but will be slower to generate.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Scale      | Input | Float    | None | Grid scale |
| Line Width | Input      |    Float    | None | Width of grid lines |
| Grid | Output | Float | None | grid lines |
| Edge Distance | Output | Float | None | black on tile edges and white at tile centers |
| Tile ID | Output | Float | None | random value between 0 and 1 for each tile |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Samples      | Dropdown | One, Four, Nine | determines the number of samples used to generate the data. Higher samples reduces aliasing but renders slower. |
