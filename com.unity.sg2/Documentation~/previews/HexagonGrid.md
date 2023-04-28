## Description
Generates a hexagonal grid - including a lined grid, distance from edges, and a unique value or ID for each tile.

## Inputs
**UV** - The coordinates to use to create the grid.
**Scale** - A multiplier for the UV coordinates that controls the size of the grid.
**Line Width** - controls the thickness of the grid lines. Only affects the Grid output.

## Outputs
**Grid** - a hexagonal grid with black lines and white tiles.
**Edge Distance** - a value that's black on tile edges and white at tile centers.
**Tile ID** - each hex grid tile is assigned a random value between 0 and 1.

## Controls
**Samples** - a dropdown that determines the number of samples used to generate the data.  Using one sample gives the best performance, but aliasing artifacts may be visible. Using Four or Nine samples will reduce aliasing, but will be slower to generate.
