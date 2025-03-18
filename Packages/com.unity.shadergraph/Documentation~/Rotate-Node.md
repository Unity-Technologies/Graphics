# Rotate Node

## Description

Rotates value of input **UV** around a reference point defined by input **Center** by the amount of input **Rotation**. The unit for rotation angle can be selected by the parameter **Unit**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center point to rotate around |
| Rotation      | Input | Float    | None | Amount of rotation to apply |
| Out | Output      |    Vector 2 | None | Output UV value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Unit      | Dropdown | Radians, Degrees | Switches the unit for input **Rotation** |

## Generated Code Example

The following example code represents one possible outcome of this node per **Unit** mode.

**Radians**

```
void Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    UV -= Center;
    float s, c;
    sincos(Rotation, s, c);
    float3 r3 = float(-s, c, s);
    float2 r1;
    r1.y = dot(UV, r3.xy);
    r1.x = dot(UV, r3.yz);
    Out = r1 + Center;
}
```

**Degrees**

```
void Unity_Rotate_Degrees_float(float2 UV, float2 Center, float Rotation, out float2 Out)
{
    Rotation = Rotation * (3.1415926f/180.0f);
    UV -= Center;
    float s, c;
    sincos(Rotation, s, c);
    float3 r3 = float(-s, c, s);
    float2 r1;
    r1.y = dot(UV, r3.xy);
    r1.x = dot(UV, r3.yz);
    Out = r1 + Center;
}
```
