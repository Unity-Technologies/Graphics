## Description
Generates a hexagonal grid - including a lined grid, distance from edges, and a unique value or ID for each tile.

## Inputs
**UV** - The coordinates to use to create the grid.
**Scale** - A multiplier for the UV coordinates that controls the size of the grid.
**Line Width** - Controls the thickness of the grid lines. Only affects the Grid output.

## Outputs
**Grid** - A hexagonal grid with black lines and white tiles.
**Edge Distance** - A value that's black on tile edges and white at tile centers.
**Tile ID** - A unique random value, between 0 and 1, assigned to each tile in the grid.

## Controls
**Samples** - Specifies the number of samples to use to generate the data. A higher number of samples reduces aliasing but renders slower.
