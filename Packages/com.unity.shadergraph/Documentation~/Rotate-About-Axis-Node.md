# Rotate About Axis Node

## Description

Rotates the input vector **In** around the axis **Axis** by the value of **Rotation**. The unit for rotation angle can be selected by the parameter **Unit**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Axis      | Input | Vector 3 | None | Axis to rotate around |
| Rotation      | Input | Float    | None | Amount of rotation to apply |
| Out | Output      |    Vector 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Unit      | Dropdown | Radians, Degrees | Switches the unit for input **Rotation** |

## Generated Code Example

The following example code represents one possible outcome of this node per **Unit** mode.

**Radians**

```
void Unity_RotateAboutAxis_Radians_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    float s, c;
    sincos(Rotation, s, c);
    Axis = normalize(Axis);
    Out = In * c + cross(Axis, In) * s + Axis * dot(Axis, In) * (1 - c);
}
```

**Degrees**

```
void Unity_RotateAboutAxis_Degrees_float(float3 In, float3 Axis, float Rotation, out float3 Out)
{
    Rotation = radians(Rotation);
    float s, c;
    sincos(Rotation, s, c);
    Axis = normalize(Axis);
    Out = In * c + cross(Axis, In) * s + Axis * dot(Axis, In) * (1 - c);
}
```
