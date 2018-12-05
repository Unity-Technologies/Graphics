# Smoothstep Node

## Description

Returns the result of a smooth Hermite interpolation between 0 and 1, if the value of input **In** is between the values of inputs **Edge1** and **Edge2** respectively. Returns 0 if the value of input **In** is less than the value of input **Step1** and 1 if greater than the value of input **Step2**.

This node is similar to the [Lerp Node](Lerp-Node.md) but there are two notable differences. Firstly, with this node the user specifies the range and the return value is between 0 and 1. This can be seen as the opposite of the [Lerp Node](Lerp-Node.md). Secondly, this node uses smooth Hermite interpolation instead of linear interpolation. This means the interpolation will gradually speed up from the start and slow down toward the end. This is useful for creating natural-looking animation, fading and other transitions.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Edge1      | Input | Dynamic Vector | Minimum step value |
| Edge2      | Input | Dynamic Vector | Maximum step value |
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Smoothstep_float4(float4 Edge1, float4 Edge2, float4 In, out float4 Out)
{
    Out = smoothstep(Step1, Step2, In);
}
```