# Polygon Node

## Description

Generates a regular polygon shape based on input **UV** at the size specified by inputs **Width** and **Height**. The polygon's amount of sides is determined by input **Sides**. The generated shape can be offset or tiled by connecting a [Tiling And Offset Node](Tiling-And-Offset-Node.md). Note that in order to preserve the ability to offset the shape within the UV space the shape will not automatically repeat if tiled. To achieve a repeating polygon effect first connect your input through a [Fraction Node](Fraction-Node.md).

NOTE: This [Node](Node.md) can only be used in the **Fragment** shader stage.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Sides      | Input | Float    | None | Amount of sides |
| Width      | Input | Float    | None | Polygon width |
| Height      | Input | Float    | None | Polygon height |
| Out | Output      |    Float    | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Polygon_float(float2 UV, float Sides, float Width, float Height, out float Out)
{
    float pi = 3.14159265359;
    float aWidth = Width * cos(pi / Sides);
    float aHeight = Height * cos(pi / Sides);
    float2 uv = (UV * 2 - 1) / float2(aWidth, aHeight);
    uv.y *= -1;
    float pCoord = atan2(uv.x, uv.y);
    float r = 2 * pi / Sides;
    float distance = cos(floor(0.5 + pCoord / r) * r - pCoord) * length(uv);
    Out = saturate((1 - distance) / fwidth(distance));
}
```