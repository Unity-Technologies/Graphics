# Clamp Node

## Description

Returns the input **In** clamped between the minimum and maximum values defined by inputs **Min** and **Max** respectively.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Unclamped input value |
| Min      | Input | Dynamic Vector | Minimum value |
| Max      | Input | Dynamic Vector | Maximum value |
| Out | Output      |    Dynamic Vector | Clamped output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Clamp_float4(float4 In, float4 Min, float4 Max, out float4 Out)
{
    Out = clamp(In, Min, Max);
}
```
