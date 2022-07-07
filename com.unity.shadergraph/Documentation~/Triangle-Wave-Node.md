# Triangle Wave Node

## Description

Returns a triangle wave from the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_TriangleWave_float4(float4 In, out float4 Out)
{
    Out = 2.0 * abs( 2 * (In - floor(0.5 + In)) ) - 1.0;
}
```