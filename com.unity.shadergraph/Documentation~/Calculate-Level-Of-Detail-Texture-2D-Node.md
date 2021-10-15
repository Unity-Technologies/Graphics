# Calculate Level Of Detail Texture 2D Node

## Description

The Calculate Level of Detail Texture 2D node calculates the mip level, or Level of Detail (LOD) used by a texture sample. This node can help you modify the LOD of a texture before sampling in your Shader Graph.

![](images/sg-calculate-level-detail-texture-2d-node.png)

The Calculate Level of Detail Texture 2D node also has a clamped and unclamped mode:

- **Clamped**: The node clamps the returned mip level to the actual mips present on the texture. The node uses the [CalculateLevelOfDetail](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod) HLSL intrinsic function.

- **Unclamped**: The node returns the ideal mip level, based on an idealized texture and number of mips. The node uses the [CalculateLevelOfDetailUnclamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod-unclamped) HLSL intrinsic function.

> [!NOTE]
> On platforms where these HLSL functions don't exist, Shader Graph determines an appropriate approximation to use, instead.

## Ports

| **Name**     | **Direction** | **Type**      | **Binding** | **Description**  |
| :---         | :---          | :------       |  :------    |   :----------    |
| Texture      | Input         | Texture 2D    |    None     | The texture to use when calculating the Level of Detail (LOD). |
| UV           | Input         | Vector 2      |    UV       | The UV coordinate to use for calculating the LOD.        |
| Sampler      | Input         | SamplerState  |    None     | The Sampler State and its corresponding settings that should be used for calculating the LOD.    |
| LOD          | Output        | Float         |    None     | The final calculated mip level or LOD of the texture sample.         |


## Controls

| **Name**     | **Type** | **Options** | **Description**  |
| :---         | :---     | :------     |  :----------     |
| Clamp        | Toggle   | True, False | When enabled, Shader Graph clamps the output LOD to the actual mips present on the provided **Texture** input. When disabled, Shader Graph returns an ideal mip level, based on an idealized texture and number of mips. |
