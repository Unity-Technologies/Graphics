# Object node

The Object node outputs the position, scale, or bounds of the overall GameObject that Unity is currently rendering.

## Render pipeline compatibility

The Object node is compatible with the following render pipelines:

- Universal Render Pipeline (URP)
- High Definition Render Pipeline (HDRP)

**Note:** The output of the **Position** port might depend on the render pipeline you use. If you use your shader in both URP and HDRP, check the results in both pipelines before you use the shader in production.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|:------------ |:-------------|:-----|:---|:---|
| **Position** | Output | Vector 3 | None | The position of the overall GameObject in world space. |
| **Scale** | Output | Vector 3 | None | The scale of the overall GameObject in world space |
| **World Bounds Min** | Output | Vector 3 | None | The minimum position of the axis-aligned bounding box that fully encloses the GameObject in world space. |
| **World Bounds Max** | Output | Vector 3 | None | The maximum position of the bounding box. |
| **Bounds Size** | Output | Vector 3 | None | The total size of the bounding box. |

**Note:** The bounds values are equivalent to the [bounds in the Renderer component](https://docs.unity3d.com/ScriptReference/Renderer-bounds.html) of the GameObject. If you deform the vertices in the shader graph, the bounds in the Renderer component don't change.

## Generated code example

The following example code represents one possible outcome of this node.

```hlsl
float3 _Object_Position = SHADERGRAPH_OBJECT_POSITION;
float3 _Object_Scale = float3(length(float3(UNITY_MATRIX_M[0].x, UNITY_MATRIX_M[1].x, UNITY_MATRIX_M[2].x)),
                             length(float3(UNITY_MATRIX_M[0].y, UNITY_MATRIX_M[1].y, UNITY_MATRIX_M[2].y)),
                             length(float3(UNITY_MATRIX_M[0].z, UNITY_MATRIX_M[1].z, UNITY_MATRIX_M[2].z)));
float3 _Object_WorldBoundsMin = SHADERGRAPH_RENDERER_BOUNDS_MIN;
float3 _Object_WorldBoundsMax = SHADERGRAPH_RENDERER_BOUNDS_MAX;
float3 _Object_BoundsSize = (SHADERGRAPH_RENDERER_BOUNDS_MAX - SHADERGRAPH_RENDERER_BOUNDS_MIN);
```
