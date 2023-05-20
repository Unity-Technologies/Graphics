# Simple Lit Shader

Use this Shader when performance is more important than photorealism. This Shader uses a simple approximation for lighting. Because this Shader [does not calculate for physical correctness and energy conservation](shading-model.md#simple-shading), it renders quickly.

## Using the Simple Lit Shader in the Editor

To select and use this Shader:

1. In your Project, create or find the Material you want to use the Shader on.  Select the **Material**. A Material Inspector window opens.
2. Click **Shader**, and select **Universal Render Pipeline** > **Simple Lit**.

## UI overview

The Inspector window for this Shader contains these elements:

* **[Surface Options](#surface-options)**
* **[Surface Inputs](#surface-inputs)**
* **[Advanced](#advanced)**

![Inspector for the Lit Shader](Images/Inspectors/Shaders/SimpleLit.png)

### Surface Options

The **Surface Options** control how the Material is rendered on a screen.

| Property           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| **Surface Type**   | Use this drop-down to apply an **Opaque** or **Transparent** surface type to the Material. This determines which render pass URP renders the material in. **Opaque** surface types are always fully visible, regardless of what’s behind them. URP renders opaque Materials first. **Transparent** surface types are affected by their background, and they can vary according to which type of transparent surface type you choose. URP renders transparent Materials in a separate pass after opaque objects.  If you select **Transparent**, the **Blending Mode** drop-down appears. |
| **Blending Mode**   | Select how Unity calculates the color of each pixel of a transparent Material when it blends the Material with the background.<br/><br/>In the context of Blending Modes, Source refers to the transparent Material where the Blending Mode is set and Destination refers to anything that Material overlaps with. |
|&#160;&#160;&#160;&#160;Alpha | ![Alpha blending mode example](./Images/blend-modes/blend-mode-alpha.png)<br/>*Alpha blending mode.*<br/><br/>**Alpha** uses the Material's alpha value to change how transparent an object is. 0 is fully transparent. 255 is fully opaque, this is translated to a value of 1 when used with the blending equations. The Material is always rendered in the Transparent render pass regardless of it's alpha value.<br/>This mode lets you use the [Preserve Specular Lighting](#preserve-specular) property.<br/><br/>Alpha equation:<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Premultiply | ![Premultiply blending mode example](./Images/blend-modes/blend-mode-premultiply.png)<br/>*Premultiply blending mode.*<br/><br/>**Premultiply** first multiplies the RGB values of the transparent Material by its alpha value then applies a similar effect to the Material as **Alpha**.<br/>The equation for **Premultiply** also allows areas of the transparent Material with an alpha value of 0 to have an additive blend effect. This can help reduce artifacts that may appear at the edge of the overlap between opaque and transparent pixels.<br/><br/>Premultiply equation:<br/>*OutputRGBA* = *SourceRGB* + *DestinationRGB* &#215; (1 &#8722; *SourceAlpha*) |
|&#160;&#160;&#160;&#160;Additive | ![Additive blending mode example](./Images/blend-modes/blend-mode-additive.png)<br/>*Additive blending mode.*<br/><br/>**Additive** adds the color values of the Materials together to create the blend effect. The alpha value determines the strength of the source Material's color before the blend is calculated.<br/>This mode lets you use the [Preserve Specular Lighting](#preserve-specular) property.<br/><br/>Additive equation:<br/>*OutputRGBA* = (*SourceRGB* &#215; *SourceAlpha*) + *DestinationRGB* |
|&#160;&#160;&#160;&#160;Multiply | ![Multiply blending mode example](./Images/blend-modes/blend-mode-multiply.png)<br/>*Multiply blending mode.*<br/><br/>**Multiply** multiplies the color of the Material with the color behind the surface. This creates a darker effect, like when you look through colored glass.<br/>This mode uses the Material’s alpha value to adjust how much the colors blend. An alpha value of 1 results in unadjusted multiplication of the colors while lower values blend the colors towards white.<br/><br/>Multiply equation:<br/>*OutputRGBA* = *SourceRGB* &#215; *DestinationRGB* <a name="preserve-specular"></a> |
| **Preserve Specular Lighting** | Indicates whether Unity preserves specular highlights on a GameObject or not. This applies even when your surface is transparent which means that only the reflected light is visible.<br/><br/>This property only appears when the **Surface Type** is set to Transparent and **Blending Mode** is set to either Alpha or Additive.<br/><br/>![Material with Preserve Specular Lighting disabled](./Images/blend-modes/preserve-specular-lighting-off.png)<br/>*Material with **Preserve Specular Lighting** disabled.*<br/><br/>![Material with Preserve Specular Lighting enabled](./Images/blend-modes/preserve-specular-lighting-on.png)<br/>*Material with **Preserve Specular Lighting** enabled.* |
| **Render Face**    | Use this drop-down to determine which sides of your geometry to render.<br/>**Front Face** renders the front face of your geometry and [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back face. This is the default setting. <br/>**Back Face** renders the back face of your geometry and culls the front face. <br/>**Both** makes URP render both faces of the geometry. This is good for small, flat objects, like leaves, where you might want both sides visible. |
| **Alpha Clipping** | Makes your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) Shader. Use this to create a transparent effect with hard edges between the opaque and transparent areas. For example, to create blades of grass. To achieve this effect, URP does not render alpha values below the specified **Threshold**, which appears when you enable **Alpha Clipping**.  You can set the **Threshold** by moving the slider, which accepts values from 0 to 1. All values above your threshold are fully opaque, and all values below your threshold are invisible. For example, a threshold of 0.1 means that URP doesn't render alpha values below 0.1. The default value is 0.5. |

### Surface Inputs

The **Surface Inputs** describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth.

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| **Base Map**     | Adds color to the surface, also known as the diffuse map. To assign a Texture to the **Base Map** setting, click the object picker next to it. This opens the Asset Browser, where you can select from the Textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned Texture. To assign another tint, you can click this color swatch. If you select **Transparent** or **Alpha Clipping** under **Surface Options**, your Material uses the Texture’s alpha channel or color. |
| **Specular Map** | Controls the color of your specular highlights from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). To assign a Texture to the **Specular Map** setting, click the object picker next to it. This opens the Asset Browser, where you can select from the textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). <br/>In **Source**, you can select a Texture in your Project to act as a source for the smoothness. By default, the source is the Alpha channel for this Texture. <br/>You can use the **Smoothness** slider to control the spread of highlights on the surface. 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between produce semi-glossy looks. For example, 0.5 produces a plastic-like glossiness. |
| **Normal Map**   | Adds a normal map to the surface. With a [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html?), you can add surface details like bumps, scratches and grooves. To add the map, click the object picker next to it. The normal map picks up ambient lighting in the environment. |
| **Emission**     | Makes the surface look like it emits lights. When enabled, the  **Emission Map** and **Emission Color** settings appear.<br/>To assign an **Emission Map**, click the object picture next to it. This opens the Asset Browser, where you can select from the textures in your Project.<br/>For **Emission Color**, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html) to assign a tint on top of the color. This can be more than 100% white, which is useful for effects like lava, that shines brighter than white while still being another color.<br/>If you have not assigned an **Emission Map**, the **Emission** setting only uses the tint you’ve assigned in **Emission Color**.<br/>If you do not enable **Emission**, URP sets the emission to black and does not calculate emission. |
| **Tiling**       | A 2D multiplier value that scales the Texture to fit across a mesh according to the U and V axes. This is good for surfaces like floors and walls. The default value is 1, which means no scaling. Set a higher value to make the Texture repeat across your mesh. Set a lower value to stretch the Texture. Try different values until you reach your desired effect. |
| **Offset**       | The 2D offset that positions the Texture on the mesh.  To adjust the position on your mesh, move the Texture across the U or V axes. |

### Advanced

The **Advanced** settings affect the underlying calculations of your rendering. They do not have a visible effect on your surface.

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| **Specular Highlights** | Enable this to allow your Material to have specular highlights from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). This means that your Material reflects the shine from these light sources. Disable this to leave out these highlight calculations, so your Shader renders faster. By default, this feature is enabled.|
**Enable GPU Instancing** | Makes URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| **Sorting Priority**              | Use this slider to determine the chronological rendering order for a Material. URP renders Materials with lower values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. |
