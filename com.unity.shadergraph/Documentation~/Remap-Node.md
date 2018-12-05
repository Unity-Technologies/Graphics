# Remap Node

## Description

Returns a value between the x and y components of input **Out Min Max** based on the linear interpolation of the value of input **In** between the x and y components of input **In Min Max**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| In Min Max      | Input | Vector 2 | Minimum and Maximum values for input interpolation |
| Out Min Max      | Input | Vector 2 | Minimum and Maximum values for output interpolation |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Remap_float4(float4 In, float2 InMinMax, float2 OutMinMax, out float4 Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}
```