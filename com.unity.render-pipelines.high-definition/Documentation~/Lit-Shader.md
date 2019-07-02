# Lit Shader

The Lit Shader lets you easily create realistic materials in the High Definition Render Pipeline (HDRP). It includes options for effects like subsurface scattering, iridescence, vertex or pixel displacement, and decal compatibility. For more information about Materials, Shaders, and Textures, see the [Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/HDRPFeatures-LitShader.png)

## Creating a Lit Material

To create a new Lit Material, navigate to your Project's Asset window, right-click in the window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder. When you create new Materials in HDRP, they use the Lit Shader by default.

## Properties

### Surface Options

**Surface Options** control the overall look of your Material's surface and how Unity renders the Material on screen.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Surface Type**          | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Rendering Pass**      | Use the drop-down to set the rendering pass that HDRP processes this Material in. For more information on this property, see the [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**          | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties enabling this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |
| **Alpha Clipping**        | Enable the checkbox to make this Material act like a Cutout Shader. Enabling this feature exposes more properties. For more information about the feature and for the  list of properties enabling this feature exposes, see the [Alpha Clipping documentation](Alpha-Clipping.html). |
| **Material Type**         | Allows you to give your Material a type, which allows you to customize it with different settings depending on the **Material Type** you select. For more information about the feature and for the list of properties each **Material Type** exposes, see the [Material Type documentation](Material-Type.html). |
| **Receive Decals**        | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR**           | Enable the checkbox to make HDRP include this Material when it processes the screen space reflection pass. |
| **Geometric Specular AA** | Enable the checkbox to tell HDRP to perform geometric anti-aliasing on this Material. This modifies the smoothness values on surfaces of curved geometry in order to remove specular artifacts. For more information about the feature and for the  list of properties enabling this feature exposes, see the [Geometric Specular Anti-aliasing documentation](Geometric-Specular-Anti-Aliasing.html). |
| **Displacement Mode**     | Use this drop-down to select the method that HDRP uses to alter the height of the Material’s surface. For more information about the feature and for the list of properties each **Displacement Mode ** exposes, see the [Displacement Mode documentation](Displacement-Mode.html). |


### Vertex Animation

| **Property**                           | **Description**                                              |
| -------------------------------------- | ------------------------------------------------------------ |
| **Motion Vector For Vertex Animation** | Enable this checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause. |

### Surface Inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Base Map**                    | Assign a Texture that controls both the color and opacity of your Material. To assign a Texture to this field, click the radio button and select your Texture in the Select Texture window. Use the color picker to select the color of the Material. If you do not assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Smoothness**                  | Use the slider to adjust the smoothness of your Material. Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a perfectly smooth surface that reflects light like a mirror, set this to a value of 1. Less smooth surfaces reflect light over a wider range of angles (because the light hits the bumps in the microsurface), so the reflections have less detail and spread across the surface in a more diffused pattern.<br />This property only appears when you unassign the Texture in the **Mask Map**. |
| **Smoothness Remapping**        | Use this min-max slider to remap the smoothness values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Ambient Occlusion Remapping** | Use this min-max slider to remap the ambient occlusion values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Mask Map**                    | Assign a Texture that packs different Material maps into each of its RGBA channels.<br />&#8226; **Red**: Stores the metallic map. <br />&#8226; **Green**: Stores the ambient occlusion map.<br />&#8226; **Blue**: Stores the detail mask map.<br />&#8226; **Alpha**: Stores the smoothness map. |
| **Normal Map Space**            | Use this drop-down to select the type of Normal Map space that this Material uses.<br />&#8226; **TangentSpace**: Defines the normal map in [tangent space](Glossary.html#TangentSpaceNormalMap). use this to tile a Texture on a Mesh. The normal map Texture must be BC7, BC5, or DXT5nm format.<br />&#8226; **ObjectSpace**: Defines the normal maps in [object space](Glossary.html#ObjectSpaceNormalMap). Use this for planar-mapping GameObjects like the terrain. The normal map must be an RGB Texture . |
| **Normal Map**                  | Assign a Texture that defines the normal map for this Material in tangent space. Use the slider to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down. |
| **Normal Map OS**               | Assign a Texture that defines the object space normal map for this Material. Use the handle to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Bent Normal Map**             | Assign a Texture that defines the bent normal map for this Material in tangent space. HDRP uses bent normal maps to simulate more accurate ambient occlusion.  Note: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down.. |
| **Bent Normal Map OS**          | Assign a Texture that defines the bent normal map for this Material in object space. HDRP uses bent normal maps to simulate more accurate ambient occlusion. Note: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Coat Mask**                   | Assign a Texture that defines the coat mask for this Material. HDRP uses this mask to simulate a clear coat effect on the Material to mimic Materials like car paint or plastics. The Coat Mask value is 0 by default, but you can use the handle to modulate the clear Coat Mask effect using a value between 0 and 1. |
| **Base UV Mapping**             | Use the drop-down to select the type of UV mapping that HDRP uses to map Textures to this Material’s surface.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom.<br />&#8226; **Triplanar**: A planar projection in three directions:<br />X-axis: Left to right<br />Y-axis: Top to bottom<br />Z-axis: Front to back<br /><br />Unity blends these three projections together to produce the final result. |
| **Tiling**                      | Set an **X** and **Y** UV tile rate for all of the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to tile these Textures across the Material’s surface, in object space. |
| **Offset**                      | Set an **X** and **Y** UV offset for all of the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to offset these Textures across the Material’s surface, in object. |

### Detail Inputs

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Detail Map**                   | Assign a [channel-packed Texture](Glossary.html#ChannelPacking) that HDRP uses to add micro details into the Material. The Detail Map uses the following channel settings:<br />&#8226; **Red**: Stores the grey scale as albedo.<br />&#8226; **Green**: Stores the green channel of the detail normal map.<br />&#8226; **Blue**: Stores the detail smoothness.<br />&#8226; **Alpha**: Stores the red channel of the detail normal map. |
| **Detail UV Mapping**            | Use the drop-down to set the type of UV map to use for the **Detail Map**. If the Material’s **Base UV mapping** property is set to **Planar** or **Triplanar**, the **Detail UV Mapping** is also set to **Planar** or **Triplanar**.<br />The **Detail Map** Texture modifies the appearance of the Material so, by default, HDRP applies the **Tiling** and **Offset** of the **Base UV Map** to the **Detail Map** to synchronize the **Detail Map** and the rest of the Material Textures. HDRP then applies the **Detail Map** **Tiling** and **Offset** properties on top of the **Base Map Tiling** and **Offset**. For example, on a plane, if the **Tiling** for **Base UV Mapping** is 2, and this value is also 2, then the **Detail Map** Texture tiles by 4 on the plane.<br />This workflow allows you to change the **Tiling** of the Texture on the Material, without having to set the **Tiling** of the **Detail UV** too.<br />To separate the **Detail UV Map** from the **Base UV Map** to set it independently, disable the **Lock to Base Tiling/Offset** checkbox. |
| **- Lock to Base Tiling/Offset** | Enable the checkbox to make the **Base UV Map**’s **Tiling** and **Offset** properties affect the **Detail Map**. HDRP multiplies these properties by the **Detail UV Map**’s **Tiling** and **Offset** properties respectively. To separate the **Detail UV Map** from the **Base UV Map** to set it independently, disable this checkbox. |
| **Tiling**                       | Set an **X** and **Y** tile rate for the **Detail Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Detail Map** across the Material’s surface, in object space. |
| **Offset**                       | Set an **X** and **Y** offset for the **Detail Map** UV. HDRP uses the **X** and **Y** values to offset  the Texture assigned to the **Detail Map** across the Material’s surface, in object space. |
| **Detail Albedo Scale**          | Use the slider to modulate the albedo of the detail map (red channel) between 0 and 2. This is an overlay effect. |
| **Detail Normal Scale**          | Use the slider to modulate the intensity of the detail normal map, between 0 and 2. The default value is 1 and has no scale. |
| **Detail Smoothness Scale**      | Use the slider modulate the smoothness of the detail map (blue channel) between 0 and 2, like an overlay effect. The default value is 1 and has no scale. |

### Transparency Inputs

Unity exposes this section if you select **Transparent** from the **Surface Type** drop-down. For information on the properties in this section, see the [Surface Type documentation](Surface-Type.html#TransparencyInputs).

<a name="EmissionInputs></a>

### Emission inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. When enabled, this exposes the **Emission Intensity** property. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission UV Mapping**         | Use the drop-down to select the type of UV mapping that HDRP uses for the **Emission Map**.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom.<br />&#8226; **Triplanar**: A planar projection in three directions:<br />X-axis: Left to right<br />Y-axis: Top to bottom<br />Z-axis: Front to back<br /><br />Unity blends these three projections together to produce the final result. |
| **- Tiling**                    | Set an **X** and **Y** tile rate for the **Emission Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **- Offset**                    | Set an **X** and **Y** offset for the **Emission Map** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material.<br />Use the drop-down to select one of the following [physical light units](Physical-Light-Units.html) to use for intensity:<br />&#8226; [Luminance](Physical-Light-Units.html#Luminance)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.html#EV) |
| **Exposure Weight**             | Use the slider to set how much effect the exposure has on the emission power. For example, if you create a neon tube, you would want to apply the emissive glow effect at every exposure. |
| **Emission Multiply with Base** | Enable the checkbox to make HDRP use the base color of the Material when it calculates the final color of the emission. When enabled, HDRP multiplies the emission color by the base color to calculate the final emission color. |
| **Emission**                    | Enable the checkbox to make the emission color affect global illumination. |
| **- Global Illumination**       | Use the drop-down to choose how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |

### Advanced options

| **Property**                            | **Description**                                              |
| --------------------------------------- | ------------------------------------------------------------ |
| **Enable GPU instancing**               | Enable the checkbox to tell HDRP to render meshes with the same geometry and Material/Shader in one batch when possible. This makes rendering faster. HDRP can not render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you can not static-batch GameObjects that have an animation based on the object pivot, but the GPU can instance them. |
| **Specular Occlusion from Bent normal** | Enable the checkbox to make HDRP use the Bent Normal Map to process specular occlusion for Reflection Probes. |