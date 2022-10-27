# Object Node

## Description

Provides access to various parameters of the currently rendering **Object**.

Note: The behaviour of the Position [Port](Port.md) can be defined per Render Pipeline. Different Render Pipelines may produce different results. If you're building a shader in one Render Pipeline that you want to use in both, try checking it in both pipelines before production.

#### Unity Render Pipelines Support
- Universal Render Pipeline
- High Definition Render Pipeline

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Position      | Output | Vector 3 | None | Object position in world space |
| Scale       | Output | Vector 3 | None | Object scale in world space |
| World Bounds Min | Output | Vector 3 | None | Minimum value of the renderer bounds in world space |
| World Bounds Max | Output | Vector 3 | None | Maximum value of the renderer bounds in world space |
| Bounds Size | Output | Vector 3 | None | Size of the renderer bounds |

Note: the bounds values are the equivalent of [the bounds in the renderer component](https://docs.unity3d.com/ScriptReference/Renderer-bounds.html). This means that vertex deformation done in ShaderGraph doesn't affect these values.

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Object_Position = SHADERGRAPH_OBJECT_POSITION;
float3 _Object_Scale = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));
float3 _Object_WorldBoundsMin = SHADERGRAPH_RENDERER_BOUNDS_MIN;
float3 _Object_WorldBoundsMax = SHADERGRAPH_RENDERER_BOUNDS_MAX;
float3 _Object_BoundsSize = (SHADERGRAPH_RENDERER_BOUNDS_MAX - SHADERGRAPH_RENDERER_BOUNDS_MIN);
```
