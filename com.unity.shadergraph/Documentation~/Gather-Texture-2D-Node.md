# Gather Texture 2D Node

The Gather Texture 2D node helps when creating custom blending between pixels. Normal texture sampling reads all four channels (RGBA) of a texture to blend the result across neighboring pixels. The Gather Texture 2D node takes four samples, using only the red channel, of each neighboring pixel for bilinear interpolation. It returns a value of `RRRR`, with each `R` value coming from a different neighbor.

This node uses the [Gather](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-gather) HLSL intrinsic function. For platforms where this intrinsic function doesn't exist, Shader Graph uses an appropriate approximation to use, instead.

> [!NOTE]
> When developing for platforms using the Metal graphics API, such as iOS or macOS, the `sample`, `sample_compare`, `gather`, and `gather_compare` instrinsics use an integer (int2) `offset` argument when sampling or gathering from a 2D texture. This value is applied to texture coordinates before looking up each pixel, and must be in the range of `-8` to `+7`. Otherwise, the Metal API clamps the `offset` value.


## Ports

| **Name**     | **Direction** | **Type**      | **Binding** | **Description**  |
| :---         | :---          | :------       |  :------    |   :----------    |
| Texture      | Input         | Texture 2D    |    None     | The texture to sample. |
| UV           | Input         | Vector 2      |    UV       | The UV coordinates that Shader Graph should use to take the sample. |
| Sampler      | Input         | SamplerState  |    None     | The Sampler State and its corresponding settings that should be used for taking the sample.    |
| Offset       | Input         | Vector 2      |    None     | The pixel offset to apply to the sample's UV coordinates, in pixels, not UV space.       |
| RGBA         | Output        | Vector 4      |    None     | The resulting sample value. This is the Red channel of the 4 neighboring pixels from the specified sample position on the given texture.     |
| R            | Output        | Float         |    None     | The first neighboring pixel's Red channel.        |
| G            | Output        | Float         |    None     | The second neighboring pixel's Red channel.       |
| B            | Output        | Float         |    None     | The third neighboring pixel's Red channel.        |
| A            | Output        | Float         |    None     | The fourth neighboring pixel's Red channel.       |
