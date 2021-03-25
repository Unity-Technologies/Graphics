# Power Node

## Description

Returns the result of input **A** to the power of input **B**.

Note: If the input **A** is negative, the output might be inconsistent or NaN.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input      |   Dynamic Vector | Second input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Power_float4(float4 A, float4 B, out float4 Out)
{
    Out = pow(A, B);
}
```
