# Sample Texture2DArray

Menu Path : **Operator > Sampling > Sample Texture2DArray** 

The **Sample Texture2DArray** Operator samples a Texture2DArray for a specified slice, UV and mip level. This Operator uses the same **Filter Mode** and **Wrap Mode** as the input texture’s [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).

This translates to a Sample() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Input**     | **Type**       | **Description**                                   |
| ------------- | -------------- | ------------------------------------------------- |
| **Texture**   | Texture2DArray | The texture array this Operator samples from.     |
| **UV**        | Vector2        | The UV this Operator samples the texture at.      |
| **Slice**     | uint           | The texture slice this Operator samples from.     |
| **Mip Level** | float          | The Mip level this Operator uses for the sampling |

| **Output** | **Type** | **Description**                          |
| ---------- | -------- | ---------------------------------------- |
| **s**      | Vector4  | The sampled value from the texture array |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.