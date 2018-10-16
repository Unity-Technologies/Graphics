# Rejection Node

## Description

Returns the result of the projection of the value of input **A** onto the plane orthogonal, or perpendicular, to the value of input **B**. The value of the rejection vector is equal to the original vector, the value of input **A**, minus the value of the [Projection](Projection-Node.md) of the same inputs.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Rejection_float4(float4 A, float4 B, out float4 Out)
{
    Out = A - (B * dot(A, B) / dot(B, B))
}
```