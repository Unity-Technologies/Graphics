# Sample TextureCubeArray

Menu Path : **Operator > Sampling > Sample TextureCubeArray**

The **Sample TextureCubeArray** Operator allows you to sample a TextureCubeArray for a specified slice, direction, and Mip level. The Operator uses the same **Filter Mode** and **Wrap Mode** modes as the [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).

This translates to a Sample() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Property**  | **Type**         | **Description**                                             |
| ------------- | ---------------- | ----------------------------------------------------------- |
| **Texture**   | TextureCubeArray | The texture array this Operator samples from.               |
| **UVW**       | Vector3          | The direction this Operator uses to sample the TextureCube. |
| **Slice**     | uint             | The texture slice this Operator samples from.               |
| **Mip Level** | float            | The mip level this Operator uses for the sampling           |

| **Property** | **Type** | **Description**                          |
| ------------ | -------- | ---------------------------------------- |
| **s**        | Vector4  | The sampled value from the texture array |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.