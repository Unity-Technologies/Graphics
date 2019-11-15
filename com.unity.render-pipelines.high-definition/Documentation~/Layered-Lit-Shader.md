# Layered Lit Shader

The Layered Lit Shader allows you to stack up to four Materials on the same GameObject in the High Definition Render Pipeline (HDRP). The Materials that it uses for each layer are HDRP [Lit Materials](Lit-Shader.html). This makes it easy to create realistic and diverse Materials in HDRP. The **Main Layer** is the undermost layer and can influence upper layers with albedo, normals, and height. HDRP renders **Layer 1**, **Layer 2**, and **Layer 3** in that order on top of the **Main Layer**. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

The Layered Lit Shader is perfect for photogrammetry. For a tutorial on how to use it in a photogrammetry workflow, see the [Photogrammetry with the Layered Shader Expert Guide](<https://unity3d.com/files/solutions/photogrammetry/Unity-Photogrammetry-Workflow-Layered-Shader_v2.pdf?_ga=2.199967142.94076306.1557740919-264818330.1555079437>).

![](Images/HDRPFeatures-LayeredLitShader.png)

## Creating a Layered Lit Material

To create a new Layered Lit Material, navigate to your Project's Asset window, right-click in the window and select **Create > Material**. This adds a new Material to your Unity Project's Asset folder. Materials use the [Lit Shader](Lit-Shader.html) by default. To make Materials use the Layered Lit Shader:

1. Click on the Material to view it in the Inspector.
2. In the **Shader** drop-down, select **HDRP > LayeredLit**.

## Properties

### Surface Options

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Surface Type**          | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Render Pass**         | Use the drop-down to set the rendering pass that HDRP processes this Material in. For more information on this property, see the [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**          | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |
| **Alpha Clipping**        | Enable the checkbox to make this Material act like a Cutout Shader. Enabling this feature exposes more properties. For more information about the feature and for the  list of properties this feature exposes, see the [Alpha Clipping documentation](Alpha-Clipping.html). |
| **Material Type**         | Allows you to give your Material a type, which allows you to customize it with different settings depending on the **Material Type** you select. For Layered Lit Materials, you can only use the **Subsurface Scattering**, **Standard**, or **Translucent** **Material Type**. For more information about the feature and for the list of properties each **Material Type** exposes, see the [Material Type documentation](Material-Type.html). |
| **Receive Decals**        | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR**           | Enable the checkbox to make HDRP include this Material when it processes the screen space reflection pass. |
| **Geometric Specular AA** | Enable the checkbox to tell HDRP to perform geometric anti-aliasing on this Material. This modifies the smoothness values on surfaces of curved geometry in order to remove specular artifacts. For more information about the feature and for the  list of properties this feature exposes, see the [Geometric Specular Anti-aliasing documentation](Geometric-Specular-Anti-Aliasing.html). |
| **Displacement Mode**     | Use this drop-down to select the method that HDRP uses to alter the height of the Material’s surface. For more information about the feature and for the list of properties each **Displacement Mode** exposes, see the [Displacement Mode documentation](Displacement-Mode.html). |

### Vertex Animation

| **Property**                           | **Description**                                              |
| -------------------------------------- | ------------------------------------------------------------ |
| **Motion Vector For Vertex Animation** | Enable the checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause. |

### Surface Inputs

| **Property**                                 | **Description**                                              |
| -------------------------------------------- | ------------------------------------------------------------ |
| **Layer Count**                              | Use the slider to set the number of layers this Material uses. You can set up to four layers. |
| **Layer Mask**                               | Assign a Texture to the field to manage the visibility of each layer. If you do not assign a Texture, the Material uses the maximum value for every channel.<br />&#8226; Alpha channel for the **Main Layer**.<br />&#8226; Red channel for **Layer 1**.<br />&#8226; Green channel for **Layer 2**.<br />&#8226; Blue channel for **Layer** |
| **BlendMask UV Mapping**                     | Use the drop-down to select the type of UV mapping that HDRP uses to map the **Layer Mask**.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom.<br />&#8226; **Triplanar**: A planar projection in three directions:X-axis: Left to rightY-axis: Top to bottomZ-axis: Front to back Unity blends these three projections together to produce the final result. |
| **World Scale**                              | Set the world-space size of the Texture in meters. If you set this to **1**, then HDRP maps the Texture to 1 meter in world space.If you set this to **2**, then HDRP maps the Texture to 0.5 meters in world space.This property only appears when you select **Planar** or **Triplanar** from the **BlendMask UV Mapping** drop-down. |
| **Tiling**                                   | Set an **X** and **Y** tile rate for the **Layer Mask** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Layer Mask** across the Material’s surface, in object space. |
| **Offset**                                   | Set an **X** and **Y** offset for the **Layer Mask** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Layer Mask** from the Material’s surface, in object space. |
| **Vertex Color Mode**                        | Use the drop-down to select the method HDRP uses to combine the **Layer Mask** to manager layer visibility.<br />&#8226; **None**: Only the **Layer Mask** affects visibility. HDRP does not combine it with vertex colors.<br />&#8226; **Multiply**: Multiplies the vertex colors from a layer with the corresponding values from the channel in the **Layer Mask** that represents that layer. The default value for a pixel in the mask is 1. Multiplying the vertex colors of a layer by the **Layer Mask** reduces the intensity of that layer, unless the value in the **Layer Mask** is 1.<br />&#8226; **Add**: Remaps vertex color values to between 0 and 1, and then adds them to the corresponding values from the channel in the **Layer Mask** that represents that layer. **Layer Mask** values between 0 and 0.5 reduce the effect of that layer, values between 0.5 and 1 increase the effect of that layer. |
| **Main Layer Influence**                     | Enable the checkbox to allow the **Main Layer** to influence the albedo, normal, and height of **Layer 1**, **Layer 2**, and **Layer 3**. You can change the strength of the influence for each layer. |
| **Use Height Based Blend**                   | Enable the checkbox to blend the layers with a heightmap. HDRP then evaluates the height of each layer to check whether to display that layer or the layer above. |
| **Height Transition**                        | Use the slider to set the transition blend size between the Materials in each layer. |
| **Lock Layers 123 Tiling With Object Scale** | Enable the checkbox to multiply the Material's tiling rate by the scale of the GameObject. This keeps the appearance of the heightmap consistent when you scale the GameObject. |

### Material To Copy

This section contains a list of the Materials that this Layered Material uses as layers. To assign a Material to a layer, either drag and drop a Material into the property field for that layer, or:

1. Click the radio button on the right of the layer to open the **Select Material** window.
2. Find the Material you want from the list of Materials in the window and double-click it.

To synchronize the properties between a referenced Material and the Layered Material, press the **Synchronize** button. This copies all of the properties from the referenced Material into the relevant Layered Material layer.

![](Images/LayeredLit1.png)

### Layers

Unity exposes up to four Material layers for you to use in your Layered Material. Use the **Layer Count** slider to set the number of layers that Unity exposes. Every layer shares the same **Surface Inputs** and **Detail Inputs**. The only difference is between the **Main Layer** and the numbered layers (**Layer 1**, **Layer 2**, and **Layer 3**) which have separate **Layering Options**.

#### Layering Options - Main Layer

| **Property**             | **Description**                                              |
| ------------------------ | ------------------------------------------------------------ |
| **Layer Influence Mask** | Assign a Texture to define the areas where the **Main Layer** can influence the numbered layers. White pixels mean full influence and black pixels mean no influence.This property only appears when you enable the **Main Layer Influence** checkbox. |

#### Layering Options - Numbered layers

| **Property**                       | **Description**                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **Use Opacity map as Density map** | Enable the checkbox to use the alpha channel of the **Base Map** as the opacity threshold. |
| **BaseColor Influence**            | Use the slider to set the strength of the **Main Layer**'s impact on this layer's base color. As you increase this value, the **Main Layer** color becomes more visible, but the Material maintains the other layers' variance.This property only appears when you enable the **Main Layer Influence** checkbox. |
| **Normal Influence**               | Use the slider to set the strength of the **Main Layer**'s impact on this layer's normals. HDRP adds the **Main Layer**'s normal values to the layer's normals.This property only appears when you enable the **Main Layer Influence** checkbox. |
| **Heightmap Influence**            | Use the slider to set the strength of the **Main Layer**'s impact on this layer's heightmap. HDRP adds the **Main Layer**'s heightmap values to the layer's heightmap.This property only appears when you enable the **Main Layer Influence** checkbox. |

#### Surface Inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Base Map**                    | Assign a Texture that controls both the color and opacity of your Material. To assign a Texture to this field, click the radio button and select your Texture in the Select Texture window. Use the color picker to select the color of the Material. If you do not assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Smoothness**                  | Use the slider to adjust the smoothness of your Material. Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a perfectly smooth surface that reflects light like a mirror, set this to a value of 1. Less smooth surfaces reflect light over a wider range of angles (because the light hits the bumps in the microsurface), so the reflections have less detail and spread across the surface in a more diffused pattern.<br />This property only appears when you unassign the Texture in the **Mask Map**. |
| **Smoothness Remapping**        | Use this min-max slider to remap the smoothness values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Ambient Occlusion Remapping** | Use this min-max slider to remap the ambient occlusion values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Mask Map**                    | Assign a [channel-packed Texture](Glossary.html#ChannelPacking) with the following Material maps in its RGBA channels.<br />&#8226; **Red**: Stores the metallic map. <br />&#8226; **Green**: Stores the ambient occlusion map.<br />&#8226; **Blue**: Stores the detail mask map.<br />&#8226; **Alpha**: Stores the smoothness map.<br />For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.html#MaskMap). |
| **Normal Map Space**            | Use this drop-down to select the type of Normal Map space that this Material uses.• **TangentSpace**: Defines the normal map in UV space; use this to tile a Texture on a Mesh. The normal map Texture must be BC7, BC5, or DXT5nm format.• **ObjectSpace**: Defines the normal maps in world space. Use this for planar-mapping objects like the terrain. The normal map must be an RGB Texture . |
| **Normal Map**                  | Assign a Texture that defines the normal map for this Material in tangent space. Use the slider to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down. |
| **Normal Map OS**               | Assign a Texture that defines the object space normal map for this Material. Use the handle to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Bent Normal Map**             | Assign a Texture that defines the bent normal map for this Material in tangent space. HDRP uses bent normal maps to simulate more accurate ambient occlusion.  Note: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down.. |
| **Bent Normal Map OS**          | Assign a Texture that defines the bent normal map for this Material in object space. HDRP uses bent normal maps to simulate more accurate ambient occlusion. Note: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Height Map**                  | Assign a Texture that defines the heightmap for this Material. Unity uses this map to blend this layer. |
| **- Parametrization**           | Use the drop-down to select the parametrization method for the to use for the **Height Map**.•**Min/Max**: HDRP compares the **Min** and **Max** value to calculate the peak, trough, and base position of the heightmap. If the **Min** is -1 and the **Max** is 3, then the base is at the Texture value 0.25. This uses the full range of the heightmap.•**Amplitude**: Allows you to manually set the amplitude and base position of the heightmap. This uses the full range of the heightmap. |
| **- Min**                       | Set the minimum value in the **Height Map**.                 |
| **- Max**                       | Set the maximum value in the **Height Map**.                 |
| **- Offset**                    | Set the offset that HDRP applies to the **Height Map**.      |
| **- Amplitude**                 | Set the amplitude of the **Height Map**.                     |
| **- Base**                      | Use the slider to set the base for the **Height Map**.       |
| **Base UV Mapping**             | Use the drop-down to select the type of UV mapping that HDRP uses to map Textures to this Material’s surface.• Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.• **Planar:** A planar projection from top to bottom.• **Triplanar**: A planar projection in three directions:X-axis: Left to rightY-axis: Top to bottomZ-axis: Front to back Unity blends these three projections together to produce the final result. |
| **Tiling**                      | Set an **X** and **Y** UV tile rate for all of the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to tile these Textures across the Material’s surface, in object space. |
| **Offset**                      | Set an **X** and **Y** UV offset for all of the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to offset these Textures across the Material’s surface, in object. |

#### Detail Inputs

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Detail Map**                   | Assign a [channel-packed Texture](Glossary.html#ChannelPacking) that HDRP uses to add micro details into the Material. The Detail Map uses the following channel settings:<br />&#8226; **Red**: Stores the grey scale as albedo.<br />&#8226; **Green**: Stores the green channel of the detail normal map.<br />&#8226; **Blue**: Stores the detail smoothness.<br />&#8226; **Alpha**: Stores the red channel of the detail normal map.<br />For more information on channel-packed Textures and the detail map, see [detail map](Mask-Map-and-Detail-Map.html#DetailMap).|
| **Detail UV Mapping**            | Use the drop-down to set the type of UV map to use for the **Detail Map**. If the Material’s **Base UV mapping** property is set to **Planar** or **Triplanar**, the **Detail UV Mapping** is also set to **Planar** or **Triplanar**.The **Detail Map** Texture modifies the appearance of the Material so, by default, HDRP applies the **Tiling** and **Offset** of the **Base UV Map** to the **Detail Map** to synchronize the **Detail Map** and the rest of the Material Textures. HDRP then applies the **Detail Map** **Tiling** and **Offset** properties on top of the **Base Map Tiling** and **Offset**. For example, on a plane, if the **Tiling** for **Base UV Mapping** is 2, and this value is also 2, then the **Detail Map** Texture tiles by 4 on the plane.This workflow allows you to change the **Tiling** of the Texture on the Material, without having to set the **Tiling** of the **Detail UV** too.To separate the **Detail UV Map** from the **Base UV Map** to set it independently, disable the **Lock to Base Tiling/Offset** checkbox. |
| **- Lock to Base Tiling/Offset** | Enable the checkbox to make the **Base UV Map**’s **Tiling** and **Offset** properties affect the **Detail Map**. HDRP multiplies these properties by the **Detail UV Map**’s **Tiling** and **Offset** properties respectively. To separate the **Detail UV Map** from the **Base UV Map** to set it independently, disable this checkbox. |
| **Tiling**                       | Set an **X** and **Y** tile rate for the **Detail Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Detail Map** across the Material’s surface, in object space. |
| **Offset**                       | Set an **X** and **Y** offset for the **Detail Map** UV. HDRP uses the **X** and **Y** values to offset  the Texture assigned to the **Detail Map** across the Material’s surface, in object space. |
| **Detail Albedo Scale**          | Use the slider to modulate the albedo of the detail map (red channel) between 0 and 2. This is an overlay effect. |
| **Detail Normal Scale**          | Use the slider to modulate the intensity of the detail normal map, between 0 and 2. The default value is 1 and has no scale. |
| **Detail Smoothness Scale**      | Use the slider modulate the smoothness of the detail map (blue channel) between 0 and 2, like an overlay effect. The default value is 1 and has no scale. |

### Emission Inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. When enabled, this exposes the **Emission Intensity** property. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission UV Mapping**         | Use the drop-down to select the type of UV mapping that HDRP uses for the **Emission Map**.• Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.• **Planar:** A planar projection from top to bottom.• **Triplanar**: A planar projection in three directions:X-axis: Left to rightY-axis: Top to bottomZ-axis: Front to back Unity blends these three projections together to produce the final result. |
| **- Tiling**                    | Set an **X** and **Y** tile rate for the **Emission Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **- Offset**                    | Set an **X** and **Y** offset for the **Emission Map** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material.Use the drop-down to select one of the following [physical light units](Physical-Light-Units.html) to use for intensity:• [Nits](Physical-Light-Units.html#Nits)• [EV<sub>100</sub>](Physical-Light-Units.html#EV) |
| **Exposure Weight**             | Use the slider to set how much effect the exposure has on the emission power. For example, if you create a neon tube, you would want to apply the emissive glow effect at every exposure. |
| **Emission Multiply with Base** | Enable the checkbox to make HDRP use the base color of the Material when it calculates the final color of the emission. When enabled, HDRP multiplies the emission color by the base color to calculate the final emission color. |
| **Emission**                    | Enable the checkbox to make the emission color affect global illumination. |
| **- Global Illumination**       | Use the drop-down to choose how color emission interacts with global illumination.• **Realtime**: Select this option to make emission affect the result of real-time global illumination.• **Baked**: Select this option to make emission only affect global illumination during the baking process.• **None**: Select this option to make emission not affect global illumination. |

### Advanced Options

| **Property**                            | **Description**                                              |
| --------------------------------------- | ------------------------------------------------------------ |
| **Enable GPU instancing**               | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you can not [static-batch](https://docs.unity3d.com/Manual/DrawCallBatching.html) GameObjects that have an animation based on the object pivot, but the GPU can instance them. |
| **Specular Occlusion from Bent normal** | Enable the checkbox to make HDRP use the Bent Normal Map to process specular occlusion for Reflection Probes. |
