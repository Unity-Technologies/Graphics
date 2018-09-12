## Description

Returns the arctangent of the values of both input **A** and input **B**. The signs (whether they are positive or negative values) of the input values are used to determine whether the output components, or channels, are positive or negative within a value of -Pi to Pi.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = atan2(A, B)`