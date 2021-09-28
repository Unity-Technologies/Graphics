# Layered Lit Shader

The Layered Lit Shader allows you to stack up to four Materials on the same GameObject in the High Definition Render Pipeline (HDRP). The Materials that it uses for each layer are HDRP [Lit Materials](Lit-Shader.md). This makes it easy to create realistic and diverse Materials in HDRP. The **Main Layer** is the undermost layer and can influence upper layers with albedo, normals, and height. HDRP renders **Layer 1**, **Layer 2**, and **Layer 3** in that order on top of the **Main Layer**. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

The Layered Lit Shader is perfect for photogrammetry. For a tutorial on how to use it in a photogrammetry workflow, see the [Photogrammetry with the Layered Shader Expert Guide](<https://unity3d.com/files/solutions/photogrammetry/Unity-Photogrammetry-Workflow-Layered-Shader_v2.pdf?_ga=2.199967142.94076306.1557740919-264818330.1555079437>).

![](Images/HDRPFeatures-LayeredLitShader.png)

## Creating a Layered Lit Material

To create a new Layered Lit Material, navigate to your Project's Asset window, right-click in the window and select **Create > Material**. This adds a new Material to your Unity Project's Asset folder. Materials use the [Lit Shader](Lit-Shader.md) by default. To make Materials use the Layered Lit Shader:

1. Click on the Material to view it in the Inspector.
2. In the **Shader** drop-down, select **HDRP > LayeredLit**.

## Properties

### Surface Options

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Surface Type**          | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.md). |
| **- Render Pass**         | Use the drop-down to set the rendering pass that HDRP processes this Material in. For more information on this property, see the [Surface Type documentation](Surface-Type.md). |
| **Double-Sided**          | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.md). |
| **Alpha Clipping**        | Enable the checkbox to make this Material act like a [Cutout Shader](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html). Enabling this feature exposes more properties. For more information about the feature and for the  list of properties this feature exposes, see the [Alpha Clipping documentation](Alpha-Clipping.md). |
| **Material Type**         | Allows you to give your Material a type, which allows you to customize it with different settings depending on the **Material Type** you select. For Layered Lit Materials, you can only use the **Subsurface Scattering**, **Standard**, or **Translucent** **Material Type**. For more information about the feature and for the list of properties each **Material Type** exposes, see the [Material Type documentation](Material-Type.md). |
| **Receive Decals**        | Enable the checkbox to allow HDRP to draw decals on this Material’s surface. |
| **Receive SSR (Transparent)** | Enable the checkbox to make HDRP include this Material when it processes the screen space reflection pass. There is a separate option for transparent Surface Type.|
| **Geometric Specular AA** | Enable the checkbox to tell HDRP to perform geometric anti-aliasing on this Material. This modifies the smoothness values on surfaces of curved geometry in order to remove specular artifacts. For more information about the feature and for the  list of properties this feature exposes, see the [Geometric Specular Anti-aliasing documentation](Geometric-Specular-Anti-Aliasing.md). |
| **Displacement Mode**     | Use this drop-down to select the method that HDRP uses to alter the height of the Material’s surface. For more information about the feature and for the list of properties each **Displacement Mode** exposes, see the [Displacement Mode documentation](Displacement-Mode.md). |

### Vertex Animation

| **Property**                           | **Description**                                              |
| -------------------------------------- | ------------------------------------------------------------ |
| **Motion Vector For Vertex Animation** | Enable the checkbox to make HDRP write motion vectors for GameObjects that use vertex animation. This removes the ghosting that vertex animation can cause. |

### Surface Inputs

[!include[](snippets/shader-properties/surface-inputs/layered-surface-inputs.md)]


### Layer List

[!include[](snippets/shader-properties/layer-list.md)]

### Layers

[!include[](snippets/shader-properties/layers.md)]

### Emission Inputs

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Use Emission Intensity**      | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. When enabled, this exposes the **Emission Intensity** property. |
| **Emission Map**                | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **Emission UV Mapping**         | Use the drop-down to select the type of UV mapping that HDRP uses for the **Emission Map**.<br />&#8226; Unity manages four UV channels for a vertex: **UV0**, **UV1**, **UV2**, and **UV3**.<br />&#8226; **Planar:** A planar projection from top to bottom.<br />&#8226; **Triplanar**: A planar projection in three directions:<br />X-axis: Left to right<br />Y-axis: Top to bottom<br />Z-axis: Front to back<br /><br />Unity blends these three projections together to produce the final result.<br />&#8226; **Same as Base**: Unity will use the **Base UV Mapping** selected in the **Surface Inputs**. If the Surface has **Pixel displacement** enabled, this option will apply displacement on the emissive map too. |
| **- Tiling**                    | Set an **X** and **Y** tile rate for the **Emission Map** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **- Offset**                    | Set an **X** and **Y** offset for the **Emission Map** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Emission Map** across the Material’s surface, in object space. |
| **Emission Intensity**          | Set the overall strength of the emission effect for this Material. Use the drop-down to select one of the following [physical light units](Physical-Light-Units.md) to use for intensity:<br />&#8226; [Nits](Physical-Light-Units.md#Nits)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.md#EV) |
| **Exposure Weight**             | Use the slider to set how much effect the exposure has on the emission power. For example, if you create a neon tube, you would want to apply the emissive glow effect at every exposure. |
| **Emission Multiply with Base** | Enable the checkbox to make HDRP use the base color of the Material when it calculates the final color of the emission. When enabled, HDRP multiplies the emission color by the base color to calculate the final emission color. |
| **Emission**                    | Toggles whether emission affects global illumination.        |
| **- Global Illumination**       | The mode HDRP uses to determine how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |

### Advanced Options
<table>
<tr>
<th>Property</th>
<th>Description</th>
</tr>
[!include[](snippets/shader-properties/advanced-options/enable-gpu-instancing.md)]
[!include[](snippets/shader-properties/advanced-options/specular-occlusion-mode.md)]
[!include[](snippets/shader-properties/advanced-options/add-precomputed-velocity.md)]
</table>
