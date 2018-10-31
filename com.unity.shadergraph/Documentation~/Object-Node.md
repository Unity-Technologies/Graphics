# Object Node

## Description

Provides access to various parameters of the currently rendering **Object**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | None | Object position in world space |
| Scale       | Output | Vector 3 | None | Object scale in world space |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Object_Position = UNITY_MATRIX_M._m03_m13_m23;
float3 _Object_Scale = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                      length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                      length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));
```