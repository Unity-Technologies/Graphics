# Polar Coordinates Node

## Description

Converts the value of input **UV** to polar coordinates. In mathematics, the polar coordinate system is a two-dimensional coordinate system in which each point on a plane is determined by a distance from a reference point and an angle from a reference direction.

The resulting effect is that the x channel of the input to **UV** is converted to a distance value from the point specified by the value of input **Center** and the y channel of same input is converted to the value of an angle of rotation around that point.

These values can be scaled by the values of inputs **Radial Scale** and **Length Scale** respectively.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center | Input      |    Vector 2 | None | Center reference point |
| Radial Scale | Input      |    Vector 1 | None | Scale of distance value |
| Length Scale | Input      |    Vector 1 | None | Scale of angle value |
| Out | Output      |    Vector 2 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
{
    float2 delta = UV - Center;
    float radius = length(delta) * 2 * RadialScale;
    float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
    Out = float2(radius, angle);
}
```