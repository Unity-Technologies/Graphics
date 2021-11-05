# Calculate Level Of Detail Texture 2D Node

The Calculate Level of Detail Texture 2D node calculates the mip level, or Level of Detail (LOD) used by a texture sample. This node can help you modify the LOD of a texture before sampling in your Shader Graph.

![An image of the Graph window, showing a Calculate Level of Detail Texture 2D node.](images/sg-calculate-level-detail-texture-2d-node.png)

The Calculate Level of Detail Texture 2D node also has a clamped and unclamped mode:

- **Clamped**: The node clamps the returned mip level to the actual mips present on the texture. The node uses the [CalculateLevelOfDetail](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod) HLSL intrinsic function.

- **Unclamped**: The node returns the ideal mip level, based on an idealized texture with all its mips present. The node uses the [CalculateLevelOfDetailUnclamped](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-calculate-lod-unclamped) HLSL intrinsic function.

> [!NOTE]
> On platforms where these HLSL functions don't exist, Shader Graph determines an appropriate approximation to use, instead.

## Create Node menu category

The Calculate Level of Detail Texture 2D node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

The Calculate Level of Detail Texture 2D node is compatible with all render pipelines.

[!include[node-fragment-only](./snippets/sg-node-fragment-only.md)]

## Ports

| **Name**     | **Direction** | **Type**      | **Binding** | **Description**  |
| :---         | :---          | :------       |  :------    |   :----------    |
| Texture      | Input         | Texture 2D    |    None     | The texture to use when calculating the Level of Detail (LOD). |
| UV           | Input         | Vector 2      |    UV       | The UV coordinate to use for calculating the LOD.        |
| Sampler      | Input         | SamplerState  |    None     | The Sampler State and its corresponding settings that Shader Graph should use to calculate the LOD.    |
| LOD          | Output        | Float         |    None     | The final calculated mip level or LOD of the texture sample.         |


## Controls

| **Name**     | **Type** | **Options** | **Description**  |
| :---         | :---     | :------     |  :----------     |
| Clamp        | Toggle   | True, False | When enabled, Shader Graph clamps the output LOD to the actual mips present on the provided **Texture** input. When disabled, Shader Graph returns an ideal mip level, based on an idealized texture with all its mips present. |

## Example shader usage

This Calculate Level of Detail Texture 2D node calculates the level of detail of the **Leaves_Albedo** texture. It sends the calculated level of detail to the LOD input port on the Sample Texture 2D LOD node for the same texture:

![An image of the Graph window, showing a Texture 2D asset node connected to a Calculate Level of Detail Texture 2D node. The node sends the calculated level of detail as an input to the LOD input port on a Sample Texture 2D LOD node.](images/sg-calculate-level-detail-texture-2d-node-example.png)
