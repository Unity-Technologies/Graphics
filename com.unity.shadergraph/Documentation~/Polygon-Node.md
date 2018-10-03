## Description

Generates a regular polygon shape based on input **UV** at the size specified by inputs **Width** and **Height**. The polygon's amount of sides is determined by input **Sides**. The generated shape can be offset or tiled by connecting a [Tiling And Offset Node](Tiling-And-Offset-Node.md). Note that in order to preserve the ability to offset the shape within the UV space the shape will not automatically repeat if tiled. To achieve a repeating polygon effect first connect your input through a [Fraction Node](Fraction-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Sides      | Input | Vector 1 | None | Amount of sides |
| Width      | Input | Vector 1 | None | Polygon width |
| Height      | Input | Vector 1 | None | Polygon height |
| Out | Output      |    Vector 1 | None | Output value |

## Shader Function

```
float tau = 6.28318530718;
float2 uv = (UV * 2 - 1) / float2(Width, Height);
uv.y *= -1;
float pCoord = atan2(uv.x, uv.y);
float r = tau / Sides;
float distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
Out = saturate((1 - distance) / fwidth(distance));
```