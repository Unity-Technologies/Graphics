## Description

Returns the result of the projection of the value of input **A** onto the plane orthogonal, or perpendicular, to the value of input **B**. The value of the rejection vector is equal to the original vector, the value of input **A**, minus the value of the [projection](Projection-Node.md) of the same inputs.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Dynamic Vector | Output value |

## Shader Function

`Out = A - (B * dot(A, B) / dot(B, B))`