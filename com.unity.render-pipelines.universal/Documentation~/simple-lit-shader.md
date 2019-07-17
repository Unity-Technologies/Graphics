# Simple Lit Shader 

Use this Shader when performance is more important than photorealism. This Shader uses a simple approximation for lighting. Because this Shader [does not calculate for physical correctness and energy conservation](shading-model.md#simple-shading), it renders quickly. 


## Using the Simple Lit Shader in the Editor 

To select and use this Shader:

1. In your Project, create or find the Material you want to use the Shader on.  Select the __Material__. A Material Inspector window opens. 
2. Click __Shader__, and select __Universal Render Pipeline__ > __Simple Lit__.

## UI overview 

The Inspector window for this Shader contains these elements: 

- __[Surface Options](#surface-options)__
- __[Surface Inputs](#surface-inputs)__
- __[Advanced](#advanced)__

![Inspector for the Lit Shader](Images/Inspectors/Shaders/SimpleLit.png)


### Surface Options 

The __Surface Options__ control how the Material is rendered on a screen. 

| Property           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| __Surface Type__   | Use this drop-down to apply an __Opaque__ or __Transparent__ surface type to the Material. This determines which render pass UniversalRP renders the material in. __Opaque__ surface types are always fully visible, regardless of what’s behind them. UniversalRP renders opaque Materials first. __Transparent__ surface types are affected by their background, and they can vary according to which type of transparent surface type you choose. UniversalRP renders transparent Materials in a separate pass after opaque objects.  If you select __Transparent__, the __Blending Mode__ drop-down appears. |
| __Blending Mode__  | Use this drop-down to determine how UniversalRP calculates the color of each pixel of the transparent Material by blending the Material with the background pixels.<br/>__Alpha__ uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds.<br/>__Premultiply__ applies a similar effect to the Material as __Alpha__, but preserves reflections and highlights, even when your surface is transparent. This means that only the reflected light is visible. For example, imagine transparent glass.<br/>__Additive__ adds an extra layer to the Material, on top of another surface. This is good for holograms. <br/>__Multiply__ multiplies the color of the Material with the color behind the surface. This creates a darker effect, like when you look through colored glass. |
| __Render Face__    | Use this drop-down to determine which sides of your geometry to render.<br/>__Front Face__ renders the front face of your geometry and [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back face. This is the default setting. <br/>__Back Face__ renders the front face of your geometry and culls the front face. <br/>__Both__ makes UniversalRP render both faces of the geometry. This is good for small, flat objects, like leaves, where you might want both sides visible. |
| __Alpha Clipping__ | Makes your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) Shader. Use this to create a transparent effect with hard edges between the opaque and transparent areas. For example, to create blades of grass. To achieve this effect, UniversalRP does not render alpha values below the specified __Threshold__, which appears when you enable __Alpha Clipping__.  You can set the __Threshold__ by moving the slider, which accepts values from 0 to 1. All values above your threshold are fully opaque, and all values below your threshold are invisible. For example, a threshold of 0.1 means that UniversalRP doesn't render alpha values below 0.1. The default value is 0.5. |




### Surface Inputs

The __Surface Inputs__ describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth. 

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| __Base Map__     | Adds color to the surface, also known as the diffuse map. To assign a Texture to the __Base Map__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the Textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned Texture. To assign another tint, you can click this color swatch. If you select __Transparent__ or __Alpha Clipping__ under __Surface Options__, your Material uses the Texture’s alpha channel or color. |
| __Specular Map__ | Controls the color of your specular highlights from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). To assign a Texture to the __Specular Map__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). <br/>In __Source__, you can select a Texture in your Project to act as a source for the smoothness. By default, the source is the Alpha channel for this Texture. <br/>You can use the __Smoothness__ slider to control the spread of highlights on the surface. 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between produce semi-glossy looks. For example, 0.5 produces a plastic-like glossiness. |
| __Normal Map__   | Adds a normal map to the surface. With a [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html?), you can add surface details like bumps, scratches and grooves. To add the map, click the object picker next to it. The normal map picks up ambient lighting in the environment. |
| __Emission__     | Makes the surface look like it emits lights. When enabled, the  __Emission Map__ and __Emission Color__ settings appear.<br/>To assign an __Emission Map__, click the object picture next to it. This opens the Asset Browser, where you can select from the textures in your Project.<br/>For __Emission Color__, you can choose the color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html) to assign a tint on top of the color. This can be more than 100% white, which is useful for effects like lava, that shines brighter than white while still being another color.<br/>If you have not assigned an __Emission Map__, the __Emission__ setting only uses the tint you’ve assigned in __Emission Color__.<br/>If you do not enable __Emission__, UniversalRP sets the emission to black and does not calculate emission. |
| __Tiling__       | A 2D multiplier value that scales the Texture to fit across a mesh according to the U and V axes. This is good for surfaces like floors and walls. The default value is 1, which means no scaling. Set a higher value to make the Texture repeat across your mesh. Set a lower value to stretch the Texture. Try different values until you reach your desired effect. |
| __Offset__       | The 2D offset that positions the Texture on the mesh.  To adjust the position on your mesh, move the Texture across the U or V axes. |

### Advanced 

The __Advanced__ settings affect the underlying calculations of your rendering. They do not have a visible effect on your surface.

| Property                  | Description                                                  |
| ------------------------- | ------------------------------------------------------------ |
| __Enable GPU Instancing__ | Makes UniversalRP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. UniversalRP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__              | Use this slider to determine the chronological rendering order for a Material. UniversalRP renders Materials with higher values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. |
