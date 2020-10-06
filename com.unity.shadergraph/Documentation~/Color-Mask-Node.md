# Color Mask Node

## Description

Creates a mask from values in input **In** equal to input **Mask Color**. Input **Range** can be used to define a wider range of values around input **Mask Color** to create the mask. Colors within this range will return 1, otherwise the node will return 0. Input **Fuzziness** can be used to soften the edges around the selection similar to anti-aliasing.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value. |
| Mask Color      | Input | Vector 3 | Color | Color to use for mask. |
| Range      | Input | Float    | None | Select colors within this range from input **Mask Color**. |
| Fuzziness      | Input | Float    | None | Feather edges around selection. Higher values result in a softer selection mask. |
| Out | Output      |    Float    | None | Output mask value. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ColorMask_float(float3 In, float3 MaskColor, float Range, float Fuzziness, out float4 Out)
{
    float Distance = distance(MaskColor, In);
    Out = saturate(1 - (Distance - Range) / max(Fuzziness, 1e-5));
}
```