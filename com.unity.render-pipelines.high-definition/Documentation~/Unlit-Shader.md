# Unlit Shader

The Unlit Shader lets you create Materials that are not affected by lighting. It includes options for the Surface Type, Emissive Color, and GPU Instancing. For more information about Materials, Shaders and Textures, see the [Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/HDRPFeatures-UnlitShader.png)

## Creating an Unlit Material

New Materials in HDRP use the [Lit Shader](Lit-Shader.html) by default. To create an Unlit Shader, you need to create a Material and then make it use the Unlit Shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select __Create > Material__. This adds a new Material to your Unity Project’s Asset folder. 

3. To use the Unlit Shader with your Material, click the __Shader__ drop-down at the top of the Material Inspector, and select to __HDRP > Unlit__.

![](Images/UnlitShader1.png)

## Properties

### Surface Options

Surface options control the overall look of your Material's surface and how Unity renders the Material on screen.

| Property| Description |
|:---|:---|
| **Surface type** | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Render Pass** | Use the drop-down to set the rendering pass that HDRP processes this Material in. For information on this property, see the [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**   | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the  list of properties enabling this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |
| **Alpha Clipping** | Enable the checkbox to make this Material act like a Cutout Shader. Enabling this feature exposes more properties. For more information about the feature and for the  list of properties enabling this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |


### Surface Inputs

| Property| Description |
|:---|:---|
| **Color** | The texture and base color of the Material. The RGB values define the color and the alpha channel defines the opacity. If you set a texture to this field, HDRP multiplies the texture by the color. If you do not set a texture in this field then HDRP only uses the base color to draw Meshes that use this Material.|
| **- Tiling** | HDRP uses the **X** and **Y** values of this property to tile the texture from the **Color** property on the object space x-axis and y-axis respectively. |
| **- Offset** | HDRP uses the **X** and **Y** values of this property to offset the texture from the **Color** property on the object space x-axis and y-axis respectively. |

### Emission Inputs

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Use Emission Intensity** | Enable the checkbox to use a separate LDR color and intensity value to set the emission color for this Material. Disable this checkbox to only use an HDR color to handle the color and emission color intensity. When enabled, this exposes the **Emission Intensity** property. |
| **Emissive Color**         | Assign a Texture that this Material uses for emission. You can also use the color picker to select a color that HDRP multiplies by the Texture. If you do not set an emission texture then HDRP only uses the HDR color to calculate the final emissive color of the Material. You can set the intensity of the HDR color within the HDR color picker. |
| **- Tiling**               | Set an **X** and **Y** tile rate for the **Emissive Color** UV. HDRP uses the **X** and **Y** values to tile the Texture assigned to the **Emissive Color** across the Material’s surface, in object space. |
| **- Offset**               | Set an **X** and **Y** offset for the **Emissive Color** UV. HDRP uses the **X** and **Y** values to offset the Texture assigned to the **Emissive Color** across the Material’s surface, in object space. |
| **Emission Intensity**     | Set the overall strength of the emission effect for this Material.<br />Use the drop-down to select one of the following [physical light units](Physical-Light-Units.html) to use for intensity:<br />&#8226; [Luminance](Physical-Light-Units.html#Luminance)<br />&#8226; [EV<sub>100</sub>](Physical-Light-Units.html#EV) |
| **Exposure Weight**        | Use the slider to set how much effect the exposure has on the emission power. For example, if you create a neon tube, you would want to apply the emissive glow effect at every exposure. |
| **Emission**               | Enable the checkbox to make the emission color affect global illumination. |
| **- Global Illumination**  | Use the drop-down to choose how color emission interacts with global illumination.<br />&#8226; **Realtime**: Select this option to make emission affect the result of real-time global illumination.<br />&#8226; **Baked**: Select this option to make emission only affect global illumination during the baking process.<br />&#8226; **None**: Select this option to make emission not affect global illumination. |

### Transparency Inputs

Unity exposes this section if you select **Transparent** from the **Surface Type** drop-down. For information on the properties in this section, see the [Surface Type documentation](Surface-Type.html#TransparencyInputs).

### Advanced Options

| Property| Description |
|:---|:---|
| **Enable GPU instancing** | Tick this checkbox to tell HDRP to render meshes with the same geometry and Material/Shader in one batch when possible. This makes rendering faster. HDRP can not render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing.  |



