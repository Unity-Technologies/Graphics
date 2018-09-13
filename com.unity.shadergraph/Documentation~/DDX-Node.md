## Description

Returns the partial derivative of the input **In** with respect to the screen-space x-coordinate. This node can only be used in the pixel shader stage.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = ddx(In)`