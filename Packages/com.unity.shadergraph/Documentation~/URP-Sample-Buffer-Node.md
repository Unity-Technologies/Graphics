# URP Sample Buffer node

The URP Sample Buffer node samples a buffer directly from the camera. Use this node in a Fullscreen Shader Graph to access Universal Render Pipeline (URP) render buffers for post-processing effects.

## Render pipeline compatibility

| **Node**              | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| --------------------- | ----------------------------------- | ------------------------------------------ |
| **URP Sample Buffer** | Yes                                 | No                                         |

## Ports

| **Name**   | **Direction** | **Type**                                                                                                                              | **Binding**     | **Description**                                                                                                                                   |
| ---------- | ------------- | ------------------------------------------------------------------------------------------------------------------------------------- | --------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| **UV**     | Input         | Vector 4                                                                                                                              | Screen position | Input UV value.                                                                                                                                   |
| **Output** | Output        | Changes to one of the following depending on the **Source Buffer** you select:<br/>&#8226; Vector 2<br/>&#8226; Vector 3<br/>&#8226; Vector 4 | None            | Output value. |

## Controls

| **Name**          | **Type**  | **Options**                                                                    | **Description**                    |
| ----------------- | --------- | ------------------------------------------------------------------------------ | ---------------------------------- |
| **Source Buffer** | Dropdown  | &#8226; NormalWorldSpace<br/>&#8226; MotionVectors<br/>&#8226; BlitSource | Determines which buffer to sample. |

The **Source Buffer** dropdown determines the type of the **Output** port:

| **Source Buffer**    | **Output type** | **Description**                                          |
| -------------------- | --------------- | -------------------------------------------------------- |
| **NormalWorldSpace** | Vector 3        | World-space surface normals from the G-buffer.           |
| **MotionVectors**    | Vector 2        | Per-pixel motion vectors.                                |
| **BlitSource**       | Vector 4        | RGBA color data from the post-process blit source.       |

## Generated code example

The following example code represents one possible outcome of this node.

**NormalWorldSpace:**

```hlsl
float3 Unity_Universal_SampleBuffer_NormalWorldSpace_float(float2 uv)
{
    return SHADERGRAPH_SAMPLE_SCENE_NORMAL(uv);
}
```

**MotionVectors:**

```hlsl
float2 Unity_Universal_SampleBuffer_MotionVectors_float(float2 uv)
{
    uint2 pixelCoords = uint2(uv * _ScreenSize.xy);
    return LOAD_TEXTURE2D_X_LOD(_MotionVectorTexture, pixelCoords, 0).xy;
}
```

**BlitSource:**

```hlsl
float4 Unity_Universal_SampleBuffer_BlitSource_float(float2 uv)
{
    uint2 pixelCoords = uint2(uv * _ScreenSize.xy);
    return LOAD_TEXTURE2D_X_LOD(_BlitTexture, pixelCoords, 0);
}
```
