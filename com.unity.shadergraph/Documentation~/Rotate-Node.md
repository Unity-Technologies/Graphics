## Description

Rotates value of input **UV** around a reference point defined by input **Center** by the amount of input **Rotation**. The unit for rotation angle can be selected by the parameter **Unit**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| UV      | Input | Vector 2 | UV | Input UV value |
| Center      | Input | Vector 2 | None | Center point to rotate around |
| Rotation      | Input | Vector 1 | None | Amount of rotation to apply |
| Out | Output      |    Vector 2 | None | Output UV value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Unit      | Dropdown | Radians, Degrees | Switches the unit for input **Rotation** |

## Shader Function

**Radians**

```
UV -= Center;
float s = sin(Rotation);
float c = cos(Rotation);
float2x2 rMatrix = float2x2(c, -s, s, c);
rMatrix *= 0.5;
rMatrix += 0.5;
rMatrix = rMatrix * 2 - 1;
UV.xy = mul(UV.xy, rMatrix);
UV += Center;
Out = UV;
```

**Degrees**

```
Rotation = Rotation * (3.1415926f/180.0f);
UV -= Center;
float s = sin(Rotation);
float c = cos(Rotation);
float2x2 rMatrix = float2x2(c, -s, s, c);
rMatrix *= 0.5;
rMatrix += 0.5;
rMatrix = rMatrix * 2 - 1;
UV.xy = mul(UV.xy, rMatrix);
UV += Center;
Out = UV;
```