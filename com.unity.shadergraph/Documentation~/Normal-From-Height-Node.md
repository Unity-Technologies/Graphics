# Normal From Height Node

## Description

Creates a normal map from a height value defined by input **Input**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 1 | Input height value |
| Out | Output      |    Vector 3 | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Output Space      | Dropdown | Tangent, World | Sets the coordinate space of the output normal. |

## Generated Code Example

The following example code represents one possible outcome of this node per **Output Space** mode.

**Tangent**

```
void Unity_NormalFromHeight_Tangent(float In, out float3 Out)
{
    float3 worldDirivativeX = ddx(Position * 100);
    float3 worldDirivativeY = ddy(Position * 100);
    float3 crossX = cross(TangentMatrix[2].xyz, worldDirivativeX);
    float3 crossY = cross(TangentMatrix[2].xyz, worldDirivativeY);
    float3 d = abs(dot(crossY, worldDirivativeX));
    float3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);
    inToNormal.y *= -1.0;
    Out = normalize((d * TangentMatrix[2].xyz) - inToNormal);
    Out = TransformWorldToTangent(Out, TangentMatrix);
}
```

**World**

```
void Unity_NormalFromHeight_World(float In, out float3 Out)
{
    float3 worldDirivativeX = ddx(Position * 100);
    float3 worldDirivativeY = ddy(Position * 100);
    float3 crossX = cross(TangentMatrix[2].xyz, worldDirivativeX);
    float3 crossY = cross(TangentMatrix[2].xyz, worldDirivativeY);
    float3 d = abs(dot(crossY, worldDirivativeX));
    float3 inToNormal = ((((In + ddx(In)) - In) * crossY) + (((In + ddy(In)) - In) * crossX)) * sign(d);
    inToNormal.y *= -1.0;
    Out = normalize((d * TangentMatrix[2].xyz) - inToNormal);
}
```