# Projection Node

## Description

Returns the result of projecting the value of input **A** onto a straight line parallel to the value of input **B**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Projection_float4(float4 A, float4 B, out float4 Out)
{
    Out = B * dot(A, B) / dot(B, B);
}
```
