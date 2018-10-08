# Dither Node

## Description

Dither is an intentional form of noise used to randomize quantization error. It is used to prevent large-scale patterns such as color banding in images. The **Dither** node applies dithering in screen-space to ensure a uniform distribution of the pattern. This can be adjusted by connecting another node to input **Screen Position**. 

This [Node](Node.md) is commonly used as an input to **Alpha Clip Threshold** on a [Master Node](Master-Node.md) to give the appearance of transparency to an opaque object. This is useful for creating objects that appear to be transparent but have the advantages of rendering as opaque, such as writing depth and/or being rendered in deferred.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Screen Position      | Input | Vector 4 | Screen Position | Coordinates used to apply dither pattern |
| Out | Output      |    Dynamic Vector | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Dither_float4(float4 In, float4 ScreenPosition, out float4 Out)
{
    float2 uv = ScreenPosition.xy * _ScreenParams.xy;
    float DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    Out = In - DITHER_THRESHOLDS[index];
}
```