# Smoothstep Node

## Description

Returns the result of a smooth Hermite interpolation between 0 and 1, if the value of input **In** is between the values of inputs **Edge1** and **Edge2** respectively. Returns 0 if the value of input **In** is less than the value of input **Edge1** and 1 if greater than the value of input **Edge2**.

The Smoothstep node is similar to the [Lerp Node](Lerp-Node.md) but there are two notable differences. Firstly, with the Smoothstep node, the user specifies the range and the return value is between 0 and 1. You can consider this the opposite of the [Lerp Node](Lerp-Node.md). Secondly, the Smoothstep node uses smooth Hermite interpolation instead of linear interpolation, which means the interpolation gradually speeds up from the start and slows down toward the end. This interpolation is useful for creating natural-looking animation, fading, and other transitions.

## Ports

| Name        | Direction | Type           | Description        |
|:------------|:----------|:---------------|:-------------------|
| Edge1       | Input     | Dynamic Vector | Minimum step value |
| Edge2       | Input     | Dynamic Vector | Maximum step value |
| In          | Input     | Dynamic Vector | Input value        |
| Out         | Output    | Dynamic Vector | Output value       |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Smoothstep_float4(float4 Edge1, float4 Edge2, float4 In, out float4 Out)
{
    Out = smoothstep(Edge1, Edge2, In);
}
```
