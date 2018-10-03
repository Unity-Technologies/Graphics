## Description

Applies a twirl warping effect similar to a black hole to the value of input **UV**. The center reference point of the warping effect is defined by input **Center** and the overall strength of the effect is defined by the value of input **Strength**. Input **Offset** can be used to offset the individual channels of the result.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center reference point |
| Strength      | Input | Vector 1 | None | Strength of the effect |
| Offset      | Input | Vector 2 | None | Individual channel offsets |
| Out | Output      |    Vector 2 | None | Output UV value |

## Shader Function

```
float2 delta = UV - Center;
float angle = Strength * length(delta);
float x = cos(angle) * delta.x - sin(angle) * delta.y;
float y = sin(angle) * delta.x + cos(angle) * delta.y;
Out = float2(x + Center.x + Offset.x, y + Center.y + Offset.y);
```