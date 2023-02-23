# Inverse Lerp Node

## Description

Returns the linear parameter that produces the interpolant specified by input **T** within the range of input **A** to input **B**.

**Inverse Lerp** is the inverse operation of the [Lerp Node](Lerp-Node.md). It can be used to determine what the input to a [Lerp](Lerp-Node.md) was based on its output.

For example, the value of a **Lerp** between 0 and 2 with a **T** value of 0.5 is 1. Therefore the value of an **Inverse Lerp** between 0 and 2 with a **T** value of 1 is 0.5.

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
void Unity_InverseLerp_float4(float4 A, float4 B, float4 T, out float4 Out)
{
    Out = (T - A)/(B - A);
}
```
