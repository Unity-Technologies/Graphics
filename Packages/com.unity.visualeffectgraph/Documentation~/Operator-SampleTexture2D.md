# Sample Texture2D

Menu Path : **Operator > Sampling > Sample Texture2D**

The **Sample Texture2D** Operator samples a Texture2D for a specified UV and Mip level. This Operator uses the same **Filter Mode** and **Wrap Mode** as the input texture’s [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).

This translates to a Sample() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Input**     | **Type**  | **Description**                         |
| ------------- | --------- | --------------------------------------- |
| **Texture**   | Texture2D | The texture this Operator samples from. |
| **UV**        | Vector2   | The UV to samples the Texture at.       |
| **Mip Level** | float     | The mip level to read from.             |

| **Output** | **Type** | **Description**                    |
| ---------- | -------- | ---------------------------------- |
| **s**      | Vector4  | The sampled value from the texture |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner Context** ports.
