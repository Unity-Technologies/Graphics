# Spherize Node

## Description

Applies a spherical warping effect similar to a fisheye camera lens to the value of input **UV**. The center reference point of the warping effect is defined by input **Center** and the overall strength of the effect is defined by the value of input **Strength**. Input **Offset** can be used to offset the individual channels of the result.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center reference point |
| Strength      | Input | Vector 1 | None | Strength of the effect |
| Offset      | Input | Vector 2 | None | Individual channel offsets |
| Out | Output      |    Vector 2 | None | Output UV value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Spherize_float(float2 UV, float2 Center, float Strength, float2 Offset, out float2 Out)
{
    float2 delta = UV - Center;
    float delta2 = dot(delta.xy, delta.xy);
    float delta4 = delta2 * delta2;
    float2 delta_offset = delta4 * Strength;
    Out = UV + delta * delta_offset + Offset;
}
```