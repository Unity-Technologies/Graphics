# Radial Shear Node

## Description

Applies a radial shear warping effect similar to a wave to the value of input **UV**. The center reference point of the warping effect is defined by input **Center** and the overall strength of the effect is defined by the value of input **Strength**. Input **Offset** can be used to offset the individual channels of the result.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center reference point |
| Strength      | Input | Float    | None | Strength of the effect |
| Offset      | Input | Vector 2 | None | Individual channel offsets |
| Out | Output      |    Vector 2 | None | Output UV value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RadialShear_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
{
    float2 delta = UV - Center;
    float delta2 = dot(delta.xy, delta.xy);
    float2 delta_offset = delta2 * Strength;
    Out = UV + float2(delta.y, -delta.x) * delta_offset + Offset;
}
```
