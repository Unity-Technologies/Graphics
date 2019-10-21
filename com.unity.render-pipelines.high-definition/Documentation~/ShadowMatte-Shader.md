# Shadow Matte

The Shadow Matte Shader lets you create Materials that are not affected by lighting but only by shadows. It includes options for the Surface Type, and GPU Instancing. For more information about Materials, Shaders and Textures, see the [Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/HDRPFeatures-ShadowMatte.png)

## Creating an Shadow Matte Material

New Materials in HDRP use the [Lit Shader](Lit-Shader.html) by default. To create an Shadow Matte Shader, you need to create a Material and then make it use the Shadow Matte Shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.

2. Right-click the Asset Window and select __Create > Material__. This adds a new Material to your Unity Project’s Asset folder. 

3. To use the Unlit Shader with your Material, click the __Shader__ drop-down at the top of the Material Inspector, and select to __HDRP > ShadowMatte__.

![](Images/ShadowMatteShader1.png)

## Properties

### Surface Options

Surface options control the overall look of your Material's surface and how Unity renders the Material on screen.

| Property| Description |
|:---|:---|
| **Surface type** | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Render Pass** | Use the drop-down to set the rendering pass that HDRP processes this Material in. For information on this property, see the [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**   | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the  list of properties enabling this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |

### Shadow

| Property| Description |
|:---|:---|
| **Shadow** | The texture and Shadow Tint of the surface. The RGB values define the color and the alpha channel defines the opacity. If you set a texture to this field. If you do not set a texture in this field then HDRP only uses the color to cast shadow with this color.|
| **- Tiling** | HDRP uses the **X** and **Y** values of this property to tile the texture from the **Color** property on the object space x-axis and y-axis respectively. |
| **- Offset** | HDRP uses the **X** and **Y** values of this property to offset the texture from the **Color** property on the object space x-axis and y-axis respectively. |
| **- Point Light Shadow** | HDRP uses value of this property set if this surface receive of not the shadow from the point lights. |
| **- Directional Light Shadow** | HDRP uses value of this property set if this surface receive of not the shadow from the directional light. |
| **- Area Light Shadow** | HDRP uses value of this property set if this surface receive of not the shadow from the area lights. |

### Surface Inputs

| Property| Description |
|:---|:---|
| **Color** | The texture and base color of the Material. The RGB values define the color and the alpha channel defines the opacity. If you set a texture to this field, HDRP multiplies the texture by the color. If you do not set a texture in this field then HDRP only uses the base color to draw Meshes that use this Material.|
| **- Tiling** | HDRP uses the **X** and **Y** values of this property to tile the texture from the **Color** property on the object space x-axis and y-axis respectively. |
| **- Offset** | HDRP uses the **X** and **Y** values of this property to offset the texture from the **Color** property on the object space x-axis and y-axis respectively. |

### Advanced Options

| Property| Description |
|:---|:---|
| **Enable GPU instancing** | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material/Shader in one batch when possible. This makes rendering faster. HDRP can not render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you can not static-batch GameObjects that have an animation based on the object pivot, but the GPU can instance them.  |
