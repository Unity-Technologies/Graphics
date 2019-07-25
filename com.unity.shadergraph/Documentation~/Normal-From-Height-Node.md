# Normal From Height Node

## Description

Creates a normal map from a height value defined by input **Input**.

## Ports

| Name         | Direction| Type         | Description |
|:------------ |:---------|:-------------|:---|
| In           | Input    | Vector 1     | Input height value |
| Strength     | Input    | Vector 1     | Strength of the normal |
| Out | Output | Vector 3 | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Output Space      | Dropdown | Tangent, World | Sets the coordinate space of the output normal. |

## Generated Code Example

The following example code represents one possible outcome of this node per **Output Space** mode.

**Tangent**

```
void Unity_NormalFromHeight_Tangent_float(float In, float3x3 TangentMatrix, float scale, out float3 Out)
{
    float3 partialDerivativeX = float3(scale, 0.0, ddx(In));
    float3 partialDerivativeY = float3(0.0, scale, ddy(In));

    Out = normalize(cross(partialDerivativeX, partialDerivativeY));
}
```

**World**

```
void Unity_NormalFromHeight_World_float(float In, float3x3 TangentMatrix, float scale, out float3 Out)
{
    float3 partialDerivativeX = float3(scale, 0.0, ddx(In));
    float3 partialDerivativeY = float3(0.0, scale, ddy(In));

    Out = normalize(cross(partialDerivativeX, partialDerivativeY));
    Out = TransformTangentToWorld(Out, TangentMatrix);
}
```