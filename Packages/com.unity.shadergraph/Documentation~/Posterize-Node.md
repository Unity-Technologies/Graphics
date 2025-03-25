# Posterize Node

## Description

> Posterization or posterisation of an image entails conversion of a continuous gradation of tone to several regions of fewer tones, with abrupt changes from one tone to another.

*[https://en.wikipedia.org/wiki/Posterization](https://en.wikipedia.org/wiki/Posterization)*

This node returns the posterized (also known as quantized) value of the input **In** into an amount of values specified by input **Steps**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Steps      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Posterize_float4(float4 In, float4 Steps, out float4 Out)
{
    Out = floor(In * Steps) / Steps;
}
```
