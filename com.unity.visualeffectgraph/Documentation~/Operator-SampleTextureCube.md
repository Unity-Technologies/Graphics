# Sample TextureCube

Menu Path : **Operator > Sampling > Sample TextureCube**

The **Sample TextureCube** Operator samples a TextureCube for a specified direction and Mip level. This Operator uses the same **Filter Mode** and **Wrap Mode** as the input texture’s [texture's import settings.](https://docs.unity3d.com/Manual/class-TextureImporter.html)

This translates to a Sample call on the texture in High-Level Shading Language (HLSL).

## Operator properties

| **Property**  | **Type** | **Description**                                             |
| ------------- | -------- | ----------------------------------------------------------- |
| **Texture**   | Texture  | The TextureCube this Operator samples from.                 |
| **UVW**       | Vector3  | The direction this Operator uses to sample the TextureCube. |
| **Mip Level** | float    | The mip level this Operator uses for the sampling.          |

| **Property** | **Type** | **Description**                    |
| ------------ | -------- | ---------------------------------- |
| **s**        | Vector4  | The sampled value from the texture |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.