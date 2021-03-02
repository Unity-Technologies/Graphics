# Normal Strength Node

## Description

Adjusts the strength of the normal map defined by input **In** by the amount of input **Strength**. A **Strength** value of 1 will return the input unaltered. A **Strength** value of 0 will return a blank normal map.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Strength      | Input | Float    | None | Strength value |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_NormalStrength_float(float3 In, float Strength, out float3 Out)
{
    Out = {precision}3(In.rg * Strength, lerp(1, In.b, saturate(Strength)));
}
```
