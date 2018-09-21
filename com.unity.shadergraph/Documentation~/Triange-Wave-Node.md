## Description

Returns a triangle wave from the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;`