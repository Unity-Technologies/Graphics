# Sample Texture3D

Menu Path : **Operator > Sampling > Sample Texture3D**

The **Sample Texture3D** Operator samples a Texture3D for a specified UV and mip level. This Operator uses the same **Filter Mode** and **Wrap Mode** as the input texture’s [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).

This translates to a Sample() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Property**  | **Type**  | **Description**                                    |
| ------------- | --------- | -------------------------------------------------- |
| **Texture**   | Texture3D | The texture this Operator samples.                 |
| **UVW**       | Vector3   | The UV this Operator to samples the Texture at.    |
| **Mip Level** | float     | The mip level this Operator uses for the sampling. |

| **Property** | **Type** | **Description**                    |
| ------------ | -------- | ---------------------------------- |
| **s**        | Vector4  | The sampled value from the texture |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.
