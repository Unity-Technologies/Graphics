# Hexagon Grid Node

## Description
Generates a hexagonal grid based on input **UV**. Control the scale of the grid with input **Scale**. In addition to grid tile outlines, the Hexagon Grid node also outputs the distance from edges, and a unique value or ID for each tile.

Because aliasing can be a concern, the Hexagon Grid node provides three levels of samples: One, Four, and Nine. One sample gives the best performance, but aliasing artifacts may be visible. Four or Nine samples reduces aliasing, but is slower to generate.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Scale      | Input | Vector 2    | None | Grid scale |
| Line Width | Input      |    Float    | None | Specify the width of grid lines as a fraction of the distance from the edge to the center of the hexagon tiles. Range (0.0 to 1.0) |
| Grid | Output | Float | None | A float value between 0.0 and 1.0 that indicates whether or not a pixel is on the grid lines. A value of 0.0 (black) indicates that a pixel is on a grid line. A value of 1.0 (white) indicates that the pixel is on the tile. When **Samples** is set to Four or Nine, pixels on diagonal grid edges may have other values  in that range (shades of gray) due to anti-aliasing.
| Edge Distance | Output | Float | None | A float value between 0.0 and 1.0 that indicates how far the pixel is from the nearest tile edge. Pixels on the tile edge are black (0.0) and pixels in the center of the tile are white (1.0). Other pixels are a shade of gray that corresponds to their distance from the edge. |
| Tile ID | Output | Float | None | A random value, between 0.0 and 1.0, used to identify each tile. |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Samples      | Dropdown | One, Four, Nine | Specify the number of samples to use to generate the data. A higher number of samples reduces aliasing but renders slower. |
