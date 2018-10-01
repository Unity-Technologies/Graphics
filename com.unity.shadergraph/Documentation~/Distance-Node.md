## Description

Returns the euclidean distance between the values of the inputs **A** and **B**. This is useful for, among other things, calculating the distance between two points in space and is commonly used in calculating a [Signed Distance Function](https://en.wikipedia.org/wiki/Signed_distance_function).

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Vector 1 | Output value |

## Shader Function

`Out = distance(A, B)`