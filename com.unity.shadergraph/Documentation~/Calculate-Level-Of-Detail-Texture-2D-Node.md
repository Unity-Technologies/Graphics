# Calculate Level Of Detail Texture 2D Node

## Description

This node is designed to work with Texture2D. It has a [clamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-clamp) and unclamped mode. It maps to the [CalculateLevelOfDetail](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod) and [CalculateLevelOfDetailUnclamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod-unclamped) HLSL intrinsic functions.
On hardware where those intrinsics don't exist, Shader Graph determines a fallback approximation.


## Ports

| **Name**     | **Direction** | **Type**      | **Binding** | **Description**  |
| :---         | :---          | :------       |  :------    |   :----------    |
| Texture      | Input         | Texture 2D    |    None     |        x         |
| UV           | Input         | Vector 2      |    UV       |        x         |
| Sampler      | Input         | SamplerState  |    None     |        x         |
| LOD          | Output        | Float         |    None     |        x         |


## Controls

| **Name**     | **Type** | **Options** | **Description**  |
| :---         | :---     | :------     |  :----------     |
| Clamp        | Toggle   | True, False |    When enabled, |
