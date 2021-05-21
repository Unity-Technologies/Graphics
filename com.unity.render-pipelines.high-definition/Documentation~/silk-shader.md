# Silk shader

The Silk shader is your starting point for rendering anisotropic fabrics in the High Definition Render Pipeline (HDRP). You can use the Silk shader to create fabrics like silk, satin, nylon, and polyester.
In general, silk and other synthetic fibers are smoother because they are produced as a single smooth filament. When this is weaved, it produces a fabric with anisotropic specular highlights.
![img](Images/HDRPFeatures-SilkShader.png)

Under the hood, the Silk shader is a pre-configured Shader Graph. To learn more about the Silk shader implementation, or to create your own Silk shader variant, see the Shader Graph documentation about the [Fabric Master Stack](master-stack-fabric.md).

## Importing the Silk Fabric Sample

The High Definition Render Pipeline (HDRP) also comes with Silk Material samples to further help you get started. To find this Sample:

1. Go to **Windows > Package Manager**, and select **High Definition RP** from the package list.
2. In the main window that shows the package's details, find the **Samples** section.
3. To import a Sample into your Project, click the **Import into Project** button. This creates a **Samples** folder in your Project and imports the Sample you selected into it. This is also where Unity imports any future Samples into.
4. In the Asset window, go to **Samples > High Definition RP > 11.0** and open the **Fabric** scene. Here you can see the silk sample material set up in-context in the scene, and available for you to use.



## Creating a Silk Material

New Materials in HDRP use the [Lit shader](Lit-Shader.md) by default. To create a Silk Material from scratch, create a Material and then make it use the Silk shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.

3. Click the **Shader** drop-down at the top of the Material Inspector, and select **HDRP > Fabric > Silk**.

   

[!include[](snippets/thread-map.md)]

[!include[](snippets/fuzz-map.md)]

## Properties

[!include[](snippets/shader-properties/surface-options/lit-surface-options.md)]

### Exposed Properties

| **Property**                          | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Base UV Mask**                      | Set the Base UV channel, by typing "1" in the column that corresponds to the channel desired and 0 in the others. |
| **Base UV Scale Transform**           | Sets the tiling rate (xy) and offsets (zw) for the base UV.  |
| **Base Color Map**                    | Assign a Texture that controls the base color of your material. |
| **Base Color**                        | Set the color of the Material. If you do not assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Mask Map AO(G) S(A)**               | Assign a [channel-packed Texture](Glossary.html#channel-packing) with the following Material maps in its RGBA channels.<br/>&#8226; **Green**: Stores the ambient occlusion map.<br/>&#8226; **Alpha**: Stores the smoothness map.For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#mask-map). |
| **Smoothness Min**                    | Sets the minimum smoothness of your Material.                |
| **Smoothness Max**                    | Sets the maximum smoothness of your Material.                |
| **Specular Color**                    | Sets the color of the specular highlight.                    |
| **Normal Map**                        | Assign a Texture that defines the normal map for this Material in tangent space. |
| **Normal Map Strength**               | Modulates the normal intensity between 0 and 8.              |
| **Use Thread Map**                    | Set whether the thread map details will be applied to your Material. |
| **Thread Map AO(R) Ny(G) S(B) Nx(A)** | Assign a Texture that defines parameters for fabric thread, with the following maps in its RGBA channels.  • **Red**: Stores the ambient occlusion map.• **Green**: Stores the normal’s Y component.• **Blue**: Stores the smoothness map.• **Alpha**: Stores the normal’s X component.For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#mask-map). |
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

## Limitations

[!include[](snippets/area-light-material-support-disclaimer.md)]