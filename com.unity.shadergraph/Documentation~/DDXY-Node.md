## Description

Returns the sum of both partial derivatives of input **In**, with respect to the screen-space x-coordinate and screen-space y-coordinate respectively. This node can only be used in the pixel shader stage.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = abs(ddx(In)) + abs(ddy(In))`