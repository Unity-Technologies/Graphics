# Gather Texture 2D Node

The Gather Texture 2D node helps when creating custom blending between pixels. Normal texture sampling reads all four channels (RGBA) of a texture to blend the result across neighboring pixels. The Gather Texture 2D node takes four samples, using only the red channel, of each neighboring pixel for bilinear interpolation. It returns a value of `RRRR`, with each `R` value coming from a different neighbor.

![An image of the Graph window, showing the Gather Texture 2D node.](images/sg-gather-texture-2d-node.png)

This node uses the [Gather](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-gather) HLSL intrinsic function. For platforms where this intrinsic function doesn't exist, Shader Graph uses an appropriate approximation, instead.

> [!NOTE]
> When developing for platforms using the Metal graphics API, the `sample`, `sample_compare`, `gather`, and `gather_compare` intrinsics use an integer (int2) `offset` argument when sampling or gathering from a 2D texture. This value is applied to texture coordinates before looking up each pixel, and must be in the range of `-8` to `+7`. Otherwise, the Metal API clamps the `offset` value.

The pixels that the Gather Texture 2D samples are always from the top mip level of the texture, from a 2×2 block of pixels around the sample point. Rather than blending the 2×2 sample, it returns the sampled pixels in counter-clockwise order, starting with the sample to the lower left of the query location:

![An image showing 4 quadrants, numbered 1 to 4, to indicate the order that the Gather Texture 2D node collects its samples: (-,+), (+,+), (-,+), (-,-).](images/sg-gather-texture-2d-node-sample-order.png)


## Create Node menu category

The Gather Texture 2D node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

The Gather Texture 2D node is compatible with all render pipelines.

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

## Example shader usage

This Gather Texture 2D node creates a blurred version of a texture by averaging its 4 samples:

![An image of the Graph window, showing a Gather Texture 2D node with its R & G ports connected to one Add node, its B port connected to another Add node, and its A port connected to another. The Add nodes add all of the Gather Texture 2D node's ports together, then uses a Divide node to divide them by 4.](images/sg-gather-texture-2d-node-example.png)

Then, the rest of the Shader Graph uses a Sample Texture 2D node to sample the texture again, and uses a Lerp node to determine when to use the blurred texture and when to use the regular texture:

![An image of the Graph window, showing a Sample Texture 2D node with its R port connected to the B port on a Lerp node. The Lerp node takes the result of the Divide node from the previous image and sends its Output port result to the Fragment Stage's Base Color and Emission nodes.](images/sg-gather-texture-2d-node-example-2.png)

By changing the value provided to the T port on the Lerp node, you can change whether the texture is blurred or sharpened in the Shader Graph.

![An image of the Graph window, showing the full graph from the previous two example images.](images/sg-gather-texture-2d-node-example-3.png)
