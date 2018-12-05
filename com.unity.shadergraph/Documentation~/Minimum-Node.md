# Minimum Node

## Description

Returns the smallest of the two inputs values **A** and **B**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Minimum_float4(float4 A, float4 B, out float4 Out)
{
    Out = min(A, B);
}
```