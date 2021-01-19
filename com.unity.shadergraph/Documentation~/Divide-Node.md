# Divide Node

## Description

Returns the result of input **A** (dividend) divided by input **B** (divisor).

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input      |   Dynamic Vector | Second input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Divide_float4(float4 A, float4 B, out float4 Out)
{
    Out = A / B;
}
```
