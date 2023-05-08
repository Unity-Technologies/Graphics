# Noise Hash Node

## Description

Uses input data, such as **UV** coordinates or **Position**, to generate deterministic random values in a grid pattern. Use the **Scale** input to control the size of the grid. 

Use the dropdown to select the desired input and output types. For example, if you want to input UV coordinates (or other Vector2 data) and get random single channel float data as a result, select Hash21 from the dropdown. The 2 indicates the input data type and the 1 indicates the output data type. If you wanted to input Vector 3 Position data and get random three channel color data as a result, select Hash33.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| UV      | Input | Vector 2 | A vector 2 input. Available if you select Hash21, Hash22, or Hash23 from the dropdown. |
| Position      | Input | Vector 3 | A vector 3 input. Available if you select Hash31 or Hash33 from the dropdown. |
| Scale      | Input      |   Vector 2 or Vector 3 | Control the size of the output grid. |
| Out | Output      |    Dynamic Vector | A grid of random values. |

