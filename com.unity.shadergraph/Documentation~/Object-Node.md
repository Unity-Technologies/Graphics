# Object Node

## Description

Provides access to various parameters of the currently rendering **Object**.

Note: The behaviour of the Position [Port](Port.md) can be defined per Render Pipeline. Different Render Pipelines may produce different results. If you're building a shader in one Render Pipeline that you want to use in both, try checking it in both pipelines before production.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | None | Object position in world space |
| Scale       | Output | Vector 3 | None | Object scale in world space |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Object_Position = SHADERGRAPH_OBJECT_POSITION;
float3 _Object_Scale = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));
```
