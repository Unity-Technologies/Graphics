## Description

Returns 1 if the value of input **In** is greater than or equal to the value of input **Edge**, otherwise returns 0.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Edge      | Input | Dynamic Vector | Step value |
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = step(Edge, In)`