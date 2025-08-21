# Remap node

The Remap node converts a value from one range to another, which is also known as linear interpolation. For example, you can use the node to convert a value in the range 0 to 1 to the equivalent value in the range 0 to 100.

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|:------------ |:-------------|:-----|:---|
| **In** | Input | Dynamic Vector | The value to convert. |
| **In Min Max** | Input | Vector 2 | The original minimum and maximum range of **In**. |
| **Out Min Max** | Input | Vector 2 | The new minimum and maximum range to use to interpolate **In**. |
| **Out** | Output | Dynamic Vector | The converted value. |

## Generated code example

The following example code represents one possible outcome of this node.

```
void Unity_Remap_float4(float4 In, float2 InMinMax, float2 OutMinMax, out float4 Out)
{
    Out = OutMinMax.x + (In - InMinMax.x) * (OutMinMax.y - OutMinMax.x) / (InMinMax.y - InMinMax.x);
}
```