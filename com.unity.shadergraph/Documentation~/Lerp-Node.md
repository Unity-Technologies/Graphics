## Description

Returns the result of linearly interpolating between input **A** and input **B** by input **T**. The value of input **T** is clamped to the range of 0 to 1.

For example, when the value of input **T** is 0 the return value is equal to the value of input **A**, when it is 1 the return value is equal to the value of input **B** and when it is 0.5 the return value is the midpoint of the two inputs **A** and **B**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| T      | Input | Dynamic Vector | Time value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = lerp(A, B, T)`