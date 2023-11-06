# Silk Material Inspector reference

You can modify the properties of a Silk material in the Silk Material Inspector.

Refer to [Fabrics](fabrics.md) for more information.

## Properties

[!include[](snippets/shader-properties/surface-options/lit-surface-options.md)]

### Exposed Properties

| **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Base UV Mask**                      | Set the Base UV channel, by typing "1" in the column that corresponds to the channel desired and 0 in the others. |
| **Base UV Scale Transform**           | Sets the tiling rate (xy) and offsets (zw) for the base UV.  |
| **Base Color Map**                    | Assign a Texture that controls the base color of your material. |
| **Base Color**                        | Set the color of the Material. If you don't assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Mask Map AO(G) S(A)**               | Assign a channel-packed Texture with the following Material maps in its RGBA channels.<br/>&#8226; **Green**: Stores the ambient occlusion map.<br/>&#8226; **Alpha**: Stores the smoothness map. For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#mask-map). |
| **Smoothness Min**                    | Sets the minimum smoothness of your Material.                |
| **Smoothness Max**                    | Sets the maximum smoothness of your Material.                |
| **Specular Color**                    | Sets the color of the specular highlight.                    |
| **Normal Map**                        | Assign a Texture that defines the normal map for this Material in tangent space. |
| **Normal Map Strength**               | Modulates the normal intensity between 0 and 8.              |
| **Use Thread Map**                    | Set whether the thread map details will be applied to your Material. |
| **Thread Map AO(R) Ny(G) S(B) Nx(A)** | Assign a Texture that defines parameters for fabric thread, with the following maps in its RGBA channels.  • **Red**: Stores the ambient occlusion map.• **Green**: Stores the normal’s Y component.• **Blue**: Stores the smoothness map.• **Alpha**: Stores the normal’s X component. For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#mask-map). |
| **Thread UV Mask**                    | Set the Thread UV channel, by typing "1" in the column that corresponds to the channel desired and 0 in the others. |
| **Thread UV Scale Transform**         | Sets the tiling rate (xy) and offsets (zw) for the thread UV. |
| **Thread AO Strength**                | Modifies the strength of the AO stored in the Thread Map.    |
| **Thread Normal Strength**            | Modifies the strength of the Normal stored in the Thread Map. |
| **Thread Smoothness Scale**           | Modifies the scale of the Smoothness stored in the Thread Map. |
| **Fuzz Map**                          | Assign a Texture that adds fuzz detail to the Base Color of your Material. |
| **Fuzz Map UV Scale**                 | Sets the scale of the Thread UV used to sample the Fuzz Map. |
| **Fuzz Strength**                     | Sets the strength of the Fuzz Color added to the Base Color. |
| **Diffusion Profile**                 | Sets the Diffusion Profile, controlling the Subsurface Scattering properties of your Material. |

[!include[](snippets/shader-properties/advanced-options/lit-advanced-options.md)]
