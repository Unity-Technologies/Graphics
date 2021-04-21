# DDX Node

## Description

Returns the partial derivative of the input **In** with respect to the screen-space x-coordinate. This node can only be used in the pixel shader stage.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output partial derivative value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_DDX_float4(float4 In, out float4 Out)
{
    Out = ddx(In);
}
```
