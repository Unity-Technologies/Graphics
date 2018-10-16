# Reciprocal Square Root Node

## Description

Returns the result of 1 divided by the square root of the input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ReciprocalSquareRoot_float4(float4 In, out float4 Out)
{
    Out = rsqrt(In);
}
```