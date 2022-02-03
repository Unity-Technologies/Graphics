# Gather Texture 2D node

The Gather Texture 2D node samples the red channel of four neighboring pixels from a sample point. It returns a value of `RRRR`, and takes each `R` value from a different neighbor. Normal Texture sampling reads all four channels (RGBA) of a Texture.

This node is useful when you want to modify the bilinear interpolation between pixels, such as when you want to create custom blends.

![An image of the Graph window, with a Gather Texture 2D node.](images/sg-gather-Texture-2d-node.png)

This node uses the [Gather](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-gather) HLSL intrinsic function. For platforms where this intrinsic function doesn't exist, Shader Graph uses an appropriate approximation, instead.

> [!NOTE]
> When you use the Metal graphics API, the `sample`, `sample_compare`, `gather`, and `gather_compare` intrinsics use an integer (int2) `offset` argument when sampling or gathering from a 2D Texture. The intrinsics apply this value to Texture coordinates before looking up each pixel. The `offset` value must be in the range of `-8` to `+7`, or the Metal API clamps the `offset` value.

The pixels that the Gather Texture 2D samples are always from the top mip level of the Texture, from a 2×2 block of pixels around the sample point. Rather than blending the 2×2 sample, it returns the sampled pixels in counter-clockwise order. It starts with the sample to the lower left of the query location:

![An image that shows 4 quadrants, numbered 1 to 4, to display the order that the Gather Texture 2D node collects its samples: (-,+), (+,+), (-,+), (-,-).](images/sg-gather-Texture-2d-node-sample-order.png)

## Create Node menu category

The Gather Texture 2D node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

[!include[nodes-compatibility-all](./snippets/nodes-compatibility-all.md)]    <!-- ALL PIPELINES INCLUDE  -->

[!include[nodes-fragment-only](./snippets/nodes-fragment-only.md)]       <!-- FRAGMENT ONLY INCLUDE  -->

## Inputs

[!include[nodes-inputs](./snippets/nodes-inputs.md)] <!-- MULTIPLE INPUT PORTS INCLUDE -->

| **Name**     | **Type**      | **Binding** | **Description**  |
| :---         | :------       |  :------    |   :----------    |
| Texture      | Texture 2D    |    None     | The Texture to sample. |
| UV           | Vector 2      |    UV       | The UV coordinates to use to take the sample. |
| Sampler      | SamplerState  |    None     | The Sampler State and its corresponding settings to use for the sample.    |
| Offset       | Vector 2      |    None     | The pixel offset to apply to the sample's UV coordinates. The **Offset** value is in pixels, not UV space.       |


## Outputs

[!include[nodes-outputs](./snippets/nodes-outputs.md)] <!-- MULTIPLE OUTPUT PORTS INCLUDE -->

| **Name** | **Type** | **Description** |
| :------  | :------- | :-------------  |
| RGBA     | Vector 4 | The sample value. This is the red channels of the 4 neighboring pixels from the specified sample position on the given Texture.     |
| R        | Float    | The first neighboring pixel's red channel.        |
| G        | Float    | The second neighboring pixel's red channel.       |
| B        | Float    | The third neighboring pixel's red channel.        |
| A        | Float    | The fourth neighboring pixel's red channel.       |

## Example graph usage

In the following example, a Gather Texture 2D node creates a blurred version of a Texture by averaging its 4 samples:

![An image of the Graph window, that displays a Gather Texture 2D node with its R & G ports connected to one Add node, its B port connected to another Add node, and its A port connected to another. The Add nodes add all the Gather Texture 2D node's ports together, then uses a Divide node to divide them by 4.](images/sg-gather-Texture-2d-node-example.png)

Then, the rest of the Shader Graph uses a Sample Texture 2D node to sample the Texture again, and uses a Lerp node to determine when to use the blurred Texture and when to use the regular Texture:

![An image of the Graph window, that displays a Sample Texture 2D node with its R port connected to the B port on a Lerp node. The Lerp node takes the result of the Divide node from the previous image and sends its Output port result to the Fragment Stage's Base Color and Emission nodes.](images/sg-gather-Texture-2d-node-example-2.png)

By changing the value provided to the T port on the Lerp node, you can change whether you want to blur or sharpen the Texture in your Shader Graph:

![An image of the Graph window, that displays the full graph from the previous two example images.](images/sg-gather-Texture-2d-node-example-3.png)

## Related nodes

[!include[nodes-related](./snippets/nodes-related.md)]

- [Sample Texture 2D node](Sample-Texture-2D-Node.md)
- [Sample Texture 2D LOD node](Sample-Texture-2D-LOD-Node.md)
- [Sampler State node](Sampler-State-Node.md)
- [Texture 2D Asset node](Texture-2D-Asset-Node.md)
