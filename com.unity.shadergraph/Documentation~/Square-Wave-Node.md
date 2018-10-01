## Description

Returns a square wave from the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = 1.0 - 2.0 * round(frac(In));`