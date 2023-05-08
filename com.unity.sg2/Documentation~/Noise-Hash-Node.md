# Noise Hash Node

## Description

Uses input data, such as **UV** coordinates or **Position**, to generate deterministic random values in a grid pattern. Use the **Scale** input to control the size of the grid. 

You can select the desired input and output types using the dropdown. For example, if you want to input UV coordinates (or other Vector2 data) and get random single channel float data as a result, you'd select Hash21 from the dropdown - the 2 indicating the input data type and the 1 indication the output data type. And if you wanted to input Vector 3 Position data and get random three channel color data as a result, you'd select Hash33.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| UV      | Input | Vector 2 | a vector 2 input - available when Hash21, Hash22, or Hash23 is selected from the dropdown |
| Position      | Input | Vector 3 | a vector 3 input - available when Hash31, or Hash33 is selected from the dropdown |
| Scale      | Input      |   Vector 2 or Vector 3 | controls the size of the output grid |
| Out | Output      |    Dynamic Vector | grid of random values |

