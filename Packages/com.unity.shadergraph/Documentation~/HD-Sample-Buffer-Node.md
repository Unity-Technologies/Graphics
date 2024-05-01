# HD Sample Buffer Node

## Description

The HD Sample Buffer Node samples a buffer directly from the Camera.

## Render pipeline compatibility

| **Node**             | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------- | ----------------------------------- | ------------------------------------------ |
| **HD Sample Buffer** | No                                  | Yes                                        |

## Ports

| **Name**          | **Direction** | **Type**                                                     | **Binding** | **Description**                                              |
| ----------------- | ------------- | ------------------------------------------------------------ | ----------- | ------------------------------------------------------------ |
| **UV**            | Input         | Vector 2                                                     | UV          | Input UV value.                                              |
| **Sampler**       | Input         | SamplerState                                                 | None        | Determines the sampler that Unity uses to sample the buffer. |
| **Layer Mask**    | Input         | Float                                                        | None        | Set the number of the Layer to sample. This port appears when you select **Thickness** in the **Source Buffer** dropdown. |
| **Output**        | Output        | Changes to one of the following depending on the Source Buffer you select:<br/>&#8226; Float<br/>&#8226; Vector 2<br/>&#8226; Vector 3<br/>&#8226; Vector 4 | None        | Output value.                                                |
| **Thickness**     | Output        | Float                                                        | None        | Sample the Worldspace value, in meters, between the near and the far plane of the camera.This port appears when you select **Thickness** in the **Source Buffer** dropdown. |
| **Overlap Count** | Output        | Float                                                        | None        | Count the number of triangles for a given pixel. This is useful for vegetation or flat surfaces.<br/>This port appears when you select **Thickness** in the **Source Buffer** dropdown. |

## Controls

| **Name**          | **Type** | **Options**                                                  | **Description**                    |
| ----------------- | -------- | ------------------------------------------------------------ | ---------------------------------- |
| **Source Buffer** | Dropdown | &#8226; NormalWorldSpace<br>&#8226; Smoothness<br/>&#8226; MotionVectors<br/>&#8226; IsSky<br/>&#8226; PostProcessInput<br/>&#8226; RenderingLayerMask<br/>&#8226; Thickness | Determines which buffer to sample. |

## Generated Code Example

The following example code represents one possible outcome of this node:

```c#
float4 Unity_HDRP_SampleBuffer_float(float2 uv, SamplerState samplerState)
{
    return SAMPLE_TEXTURE2D_X_LOD(_CustomPostProcessInput, samplerState, uv * _RTHandlePostProcessScale.xy, 0);
}
```
