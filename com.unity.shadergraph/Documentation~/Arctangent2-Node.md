# Arctangent2 Node

## Description

Returns the arctangent of the values of both input **A** and input **B**. The signs (whether they are positive or negative values) of the input values are used to determine whether the output components, or channels, are positive or negative within a range of -Pi to Pi.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Arctangent2_float4(float4 A, float4 B, out float4 Out)
{
    Out = atan2(A, B);
}
```