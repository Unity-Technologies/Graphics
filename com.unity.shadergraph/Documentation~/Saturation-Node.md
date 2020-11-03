# Saturation Node

## Description

Adjusts the saturation of input **In** by the amount of input **Saturation**. A **Saturation** value of 1 will return the input unaltered. A **Saturation** value of 0 will return the input completely desaturated.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Saturation      | Input | Float    | None | Saturation value |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Saturation_float(float3 In, float Saturation, out float3 Out)
{
    float luma = dot(In, float3(0.2126729, 0.7151522, 0.0721750));
    Out =  luma.xxx + Saturation.xxx * (In - luma.xxx);
}
```