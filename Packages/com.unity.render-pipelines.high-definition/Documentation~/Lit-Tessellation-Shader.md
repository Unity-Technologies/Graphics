# Lit Tessellation Shader

The Lit Tessellation Shader allows you to create Materials that use tessellation to provide adaptive vertex density for meshes. This means that you can render more detailed geometry without the need to create a model that contains a lot of vertices. This Shader also includes options for effects like subsurface scattering, iridescence, vertex or pixel displacement, and decal compatibility. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/LitTessellationShader1.png)

**Tessellation Mode** set to **None** (off).

![](Images/LitTessellationShader2.png)

**Tessellation Mode** set to **Phong** (on).

## Creating a Lit Tessellation Material

To create a new Lit Tessellation Material:

1. Right-click in your Project's Asset window.
2. Select **Create** > **Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Select the Material and, in the Inspector, select the **Shader** drop-down.
4. Select **HDRP** > **LitTessellation**.

## Properties

### Surface Options

**Surface Options** control the look of your Material's surface and how Unity renders the Material on screen.

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Surface Type**          | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.md). |
| **- Rendering Pass**      | Use the drop-down to set the rendering pass that HDRP processes this Material in. For more information on this property, see the [Surface Type documentation](Surface-Type.md). |
| **Alpha Clipping**        | Enable the checkbox to make this Material act like a [Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html). Enabling this feature exposes more properties. For more information about the feature and for the  list of properties this feature exposes, see the [Alpha Clipping documentation](Alpha-Clipping.md). |
| **Double-Sided**          | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.md). |
| **Material Type**         | Allows you to give your Material a type, which allows you to customize it with different settings depending on the **Material Type** you select. For more information about the feature and for the list of properties each **Material Type** exposes, see the [Material Type documentation](Material-Type.md). |
| **Receive Decals**        | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR (Transparent)** | Enable the checkbox to make HDRP include this Material when it processes the screen space reflection pass. There is a separate option for transparent Surface Type. |
| **Geometric Specular AA** | Enable the checkbox to make HDRP perform geometric antialiasing on this Material. This modifies the smoothness values on surfaces of curved geometry to remove specular artifacts. For more information about the feature and for the list of properties this feature exposes, see the [Geometric Specular antialiasing documentation](Geometric-Specular-Anti-Aliasing.md). |
| **Displacement Mode**     | Use this drop-down to select the method that HDRP uses to alter the height of the Material’s surface. For more information about the feature and for the list of properties each **Displacement Mode** exposes, see the [Displacement Mode documentation](Displacement-Mode.md). |

### Tessellation Options

For information on the properties in this section, see the [Tessellation documentation](Tessellation.md).

### Surface Inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Base Map**                    | Assign a Texture that controls both the color and opacity of your Material. To assign a Texture to this field, click the radio button and select your Texture in the Select Texture window. Use the color picker to select the color of the Material. If you don't assign a Texture, this is the absolute color of the Material. If you do assign a Texture, the final color of the Material is a combination of the Texture you assign and the color you select. The alpha value of the color controls the transparency level for the Material if you select **Transparent** from the **Surface Type** drop-down. |
| **Metallic**                    | Use this slider to adjust how "metal-like" the surface of your Material is (between 0 and 1).<br />When a surface is more metallic, it reflects the environment more and its albedo color becomes less visible. At full metallic level, the surface color is entirely driven by reflections from the environment. When a surface is less metallic, its albedo color is clearer and any surface reflections are visible on top of the surface color, rather than obscuring it.<br />This property only appears when you unassign the Texture in the **Mask Map**. |
| **Smoothness**                  | Use the slider to adjust the smoothness of your Material.<br /> Every light ray that hits a smooth surface bounces off at predictable and consistent angles. For a smooth surface that reflects light like a mirror, set this to a value of 1. Less smooth surfaces reflect light over a wider range of angles (because the light hits the bumps in the microsurface), so the reflections have less detail and spread across the surface in a more diffused pattern.<br />This property only appears when you unassign the Texture in the **Mask Map**. |
| **Metallic Remapping**          | Use this min-max slider to remap the metallic values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Smoothness Remapping**        | Use this min-max slider to remap the smoothness values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Ambient Occlusion Remapping** | Use this min-max slider to remap the ambient occlusion values from the **Mask Map** to the range you specify. Rather than [clamping](https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html) values to the new range, Unity condenses the original range down to the new range uniformly.<br />This property only appears when you assign a **Mask Map**. |
| **Mask Map**                    | Assign a [channel-packed Texture](Glossary.md#ChannelPacking) with the following Material maps in its RGBA channels.<br />&#8226; **Red**: Stores the metallic map. <br />&#8226; **Green**: Stores the ambient occlusion map.<br />&#8226; **Blue**: Stores the detail mask map.<br />&#8226; **Alpha**: Stores the smoothness map.<br />For more information on channel-packed Textures and the mask map, see [mask map](Mask-Map-and-Detail-Map.md#MaskMap). |
| **Normal Map Space**            | Use this drop-down to select the Normal Map space that this Material uses.<br />&#8226; **TangentSpace**: Defines the normal map in [tangent space](Glossary.md#TangentSpaceNormalMap). use this to tile a Texture on a Mesh. The normal map Texture must be BC7, BC5, or DXT5nm format.<br />&#8226; **ObjectSpace**: Defines the normal maps in [object space](Glossary.md#ObjectSpaceNormalMap). Use this for planar-mapping GameObjects like the terrain. The normal map must be an RGB Texture . |
| **Normal Map**                  | Assign a Texture that defines the normal map for this Material in tangent space. Use the slider to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down. |
| **Normal Map OS**               | Assign a Texture that defines the object space normal map for this Material. Use the handle to modulate the normal intensity between 0 and 8.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Bent Normal Map**             | Assign a Texture that defines the bent normal map for this Material in tangent space. HDRP uses bent normal maps to simulate more accurate ambient occlusion. <br />**Note**: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **TangentSpace** from the **Normal Map Space** drop-down.. |
| **Bent Normal Map OS**          | Assign a Texture that defines the bent normal map for this Material in object space. HDRP uses bent normal maps to simulate more accurate ambient occlusion. <br />**Note**: Bent normal maps only work with diffuse lighting.<br />This property only appears when you select **ObjectSpace** from the **Normal Map Space** drop-down. |
| **Coat Mask**                   | Assign a Texture that defines the coat mask for this Material. HDRP uses this mask to simulate a clear coat effect on the Material to mimic Materials like car paint or plastics. The Coat Mask value is 0 by default, but you can use the handle to modulate the clear Coat Mask effect using a value between 0 and 1. |
| **Base UV Mapping**             | Use the drop-down to select the UV mapping type that HDRP uses to map Textures to this Material’s surface.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom.Tessellation does not work when you use the Planar UV mapping type.<br />&#8226; **Triplanar**: A planar projection in three directions:<br /> &#8226; X-axis: Left to right<br /> &#8226; Y-axis: Top to bottom<br /> &#8226; Z-axis: Front to back<br />Unity blends these three projections together to produce the final result. Tessellation does not work when you use the Triplanar UV mapping type. |
| **Tiling**                      | Set an **X** and **Y** UV tile rate for all the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to tile these Textures across the Material’s surface, in object space. |
| **Offset**                      | Set an **X** and **Y** UV offset for all the Textures in the **Surface Inputs** section. HDRP uses the **X** and **Y** values to offset these Textures across the Material’s surface, in object. |

### Detail Inputs

[!include[](snippets/shader-properties/detail-inputs.md)]

### Transparency Inputs

Unity exposes this section if you select **Transparent** from the **Surface Type** drop-down. For information on the properties in this section, see the [Surface Type documentation](Surface-Type.md#TransparencyInputs).

<a name="EmissionInputs"></a>

### Emission inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. When enabled, this exposes the **Emission Intensity** property. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you don't set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission UV Mapping**         | Use the drop-down to select the UV mapping type that HDRP uses for the **Emission Map**.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom. Tessellation does not work when you use the Planar UV mapping type.<br />&#8226; **Triplanar**: A planar projection in three directions:<br />&#8226; X-axis: Left to right<br />&#8226; Y-axis: Top to bottom<br />&#8226; Z-axis: Front to back<br />Unity blends these three projections together to produce the final result. Tessellation does not work when you use the Triplanar UV mapping type.<br />&#8226; **Same as Base**: Unity uses the **Base UV Mapping** selected in the **Surface Inputs**. |
| **- Tiling**                    | Set an **X** and **Y** tile rate for the **Emission Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **- Offset**                    | Set an **X** and **Y** offset for the **Emission Map** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material.<br />Use the drop-down to select one of the following [physical light units](Physical-Light-Units.md) to use for intensity:<br />&#8226; [Nits](Physical-Light-Units.md#Nits)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.md#EV) |
| **Exposure Weight**             | Use the slider to set how much effect the exposure has on the emission power. For example, if you create a neon tube, you would want to apply the emissive glow effect at every exposure. |
| **Emission Multiply with Base** | Enable the checkbox to make HDRP use the base color of the Material when it calculates the final color of the emission. When enabled, HDRP multiplies the emission color by the base color to calculate the final emission color. |
| **Emission**                    | Toggles whether emission affects global illumination.        |
| **- Global Illumination**       | The mode HDRP uses to determine how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |

### Advanced options

<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/advanced-options/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)]
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)]
</table>
