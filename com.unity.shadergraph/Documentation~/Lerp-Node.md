# Lerp Node

## Description

Returns the result of linearly interpolating between input **A** and input **B** by input **T**.

For example, when the value of input **T** is 0 the return value is equal to the value of input **A**, when it is 1 the return value is equal to the value of input **B** and when it is 0.5 the return value is the midpoint of the two inputs **A** and **B**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| T      | Input | Dynamic Vector | Time value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Lerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = lerp(A, B, T);
}
```
