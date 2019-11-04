# Shadow Matte Shader

The Shadow Matte Shader lets you create Materials that only shadows affect. This means that lighting does not affect Materials that use this Shader. It includes options for the Surface Type, and GPU Instancing. For more information about Materials, Shaders, and Textures, see the[ Unity User Manual](https://docs.unity3d.com/Manual/Shaders.html).

![](Images/HDRPFeatures-ShadowMatte.png)

## Creating a Shadow Matte Material

New Materials in HDRP use the [Lit Shader](Lit-Shader.html) by default. To create a Shadow Matte Material, you need to create a Material and then make it use the Shadow Matte Shader. To do this:

1. In the Unity Editor, navigate to your Project's Asset window.
2. Right-click the Asset Window and select **Create > Material**. This adds a new Material to your Unity Project’s Asset folder.
3. Click the **Shader** drop-down at the top of the Material Inspector, and select to **HDRP > ShadowMatte**.

## Properties

### Surface Options

**Surface Options** control the overall look of your Material's surface and how Unity renders the Material on-screen.

| **Property**      | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Surface type**  | Use the drop-down to define whether your Material supports transparency or not. Materials with a **Transparent Surface Type** are more resource intensive to render than Materials with an **Opaque** **Surface Type**. HDRP exposes more properties, depending on the **Surface Type** you select. For more information about the feature and for the list of properties each **Surface Type** exposes, see the [Surface Type documentation](Surface-Type.html). |
| **- Render Pass** | Use the drop-down to set the rendering pass that HDRP processes this Material in. For information on this property, see the Surface [Surface Type documentation](Surface-Type.html). |
| **Double-Sided**  | Enable the checkbox to make HDRP render both faces of the polygons in your geometry. For more information about the feature and for the list of properties this feature exposes, see the [Double-Sided documentation](Double-Sided.html). |

### Shadow

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Shadow**                     | The Texture and shadow tint of the surface. The RGB values define the color and the alpha channel defines the opacity. If you set a Texture to this field, HDRP blends the Texture and the shadow tint color to produce the final shadow color. When HDRP blends the Texture and shadow tint color, it uses the alpha values of both to determine their contribution to the final color. If you do not set a Texture to this field then HDRP only uses the shadow tint color to produce the final shadow color. |
| **- Tiling**                   | HDRP uses the **X** and **Y** values of this property to tile the Texture set as the **Shadow** property along the object space x-axis and y-axis respectively. |
| **- Offset**                   | HDRP uses the **X** and **Y** values of this property to offset the Texture set as the **Shadow** property along the object space x-axis and y-axis respectively. |
| **- Point Light Shadow**       | Specifies whether the surface of this Material receives shadows from [point Lights](Light-Component.html#PointLight). |
| **- Directional Light Shadow** | Specifies whether the surface of this Material receives shadows from [directional Lights](Light-Component.html#DirectionalLight). |
| **- Area Light Shadow**        | Specifies whether the surface of this Material receives shadows from [area Lights](Light-Component.html#AreaLights). |

### Surface Inputs

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Color**    | The Texture and base color of the Material. The RGB values define the color and the alpha channel defines the opacity. If you set a Texture to this field, HDRP multiplies the Texture by the color. If you do not set a Texture in this field then HDRP only uses the base color to draw Meshes that use this Material. |
| **- Tiling** | HDRP uses the **X** and **Y** values of this property to tile the Texture set as the **Color** property along the object space x-axis and y-axis respectively. |
| **- Offset** | HDRP uses the **X** and **Y** values of this property to offset the Texture set as the **Color** property along the object space x-axis and y-axis respectively. |

### Advanced Options

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Enable GPU instancing** | Enable the checkbox to tell HDRP to render Meshes with the same geometry and Material in one batch when possible. This makes rendering faster. HDRP cannot render Meshes in one batch if they have different Materials, or if the hardware does not support GPU instancing. For example, you cannot [static-batch](<https://docs.unity3d.com/Manual/DrawCallBatching.html>) GameObjects that have an animation based on the object pivot point, but the GPU can instance them. |