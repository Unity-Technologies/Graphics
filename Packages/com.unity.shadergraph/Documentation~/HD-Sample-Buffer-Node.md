# HD Sample Buffer Node

## Description

The HD Sample Buffer Node samples a buffer directly from the Camera. 

## Render pipeline compatibility

| **Node**             | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------- | ----------------------------------- | ------------------------------------------ |
| **HD Sample Buffer** | No                                  | Yes                                        |

## Ports

| **Name**    | **Direction** | **Type**     | **Binding** | **Description**                                              |
| ----------- | ------------- | ------------ | ----------- | ------------------------------------------------------------ |
| **UV**      | Input         | Vector 2     | UV          | Input UV value.                                              |
| **Sampler** | Input         | SamplerState | None        | Determines the sampler that Unity uses to sample the buffer. |
| **Output**  | Output        | Float        | None        | Output value.                                                |

## Controls

| **Name**      | **Type** | **Options**                                                  | **Description**                    |
| ------------- | -------- | ------------------------------------------------------------ | ---------------------------------- |
| Source Buffer | Dropdown | World Normal, Roughness, Motion Vectors, PostProcess Input, Blit Source. | Determines which buffer to sample. |

## Generated Code Example

The following example code represents one possible outcome of this node:

```c#
float4 Unity_HDRP_SampleBuffer_float(float2 uv, SamplerState samplerState)

{

return SAMPLE_TEXTURE2D_X_LOD(_CustomPostProcessInput, samplerState, uv * _RTHandlePostProcessScale.xy, 0);

}
```
