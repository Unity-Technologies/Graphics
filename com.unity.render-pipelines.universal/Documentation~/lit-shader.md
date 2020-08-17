# Lit Shader

The Lit Shader lets you render real-world surfaces like stone, wood, glass, plastic, and metals in photo-realistic quality. Your light levels and reflections look lifelike and react properly across various lighting conditions, for example bright sunlight, or a dark cave. This Shader uses the most computationally heavy [shading model](shading-model.md) in the Universal Render Pipeline (URP). 

## Using the Lit Shader in the Editor 

To select and use this Shader:

1. In your Project, create or find the Material you want to use the Shader on.  Select the __Material__. A Material Inspector window opens. 
2. Click __Shader__, and select __Universal Render Pipeline__ > __Lit__.

## UI overview  

The Inspector window for this Shader contains these elements: 

- __[Surface Options](#surface-options)__
- __[Surface Inputs](#surface-inputs)__
- __[Advanced](#advanced)__

![Inspector for the Lit Shader](Images/Inspectors/Shaders/Lit.png)



### Surface Options 

The __Surface Options__ control how URP renders the Material on a screen. 

| Property            | Description                                                  |
| ------------------- | ------------------------------------------------------------ |
| __Workflow Mode__   | Use this drop-down menu to choose a workflow that fits your Textures, either  [__Metallic__](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterMetallic.html) and [__Specular__](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterSpecular.html).<br/>When you have made your choice, the main Texture options in the rest of the Inspector now follow your chosen workflow. For information on metallic or specular workflows, see [this Manual page for the Standard built-in Shader in Unity](https://docs.unity3d.com/Manual/StandardShaderMetallicVsSpecular.html). |
| __Surface Type__    | Use this drop-down to apply an __Opaque__ or __Transparent__ surface type to the Material. This determines which render pass URP renders the material in. __Opaque__ surface types are always fully visible, regardless of what’s behind them. URP renders opaque Materials first. __Transparent__ surface types are affected by their background, and they can vary according to which type of transparent surface type you choose. URP renders transparent Materials in a separate pass after opaque objects.  If you select __Transparent__, the __Blending Mode__ drop-down appears. |
| __Blending Mode__   | Use this drop-down to determine how URP calculates the color of each pixel of the transparent Material by blending the Material with the background pixels.<br/>__Alpha__ uses the Material’s alpha value to change how transparent an object is. 0 is fully transparent. 1 appears fully opaque, but the Material is still rendered during the Transparent render pass. This is useful for visuals that you want to be fully visible but to also fade over time, like clouds.<br/>__Premultiply__ applies a similar effect to the Material as __Alpha__, but preserves reflections and highlights, even when your surface is transparent. This means that only the reflected light is visible. For example, imagine transparent glass.<br/>__Additive__ adds an extra layer to the Material, on top of another surface. This is good for holograms. <br/>__Multiply__ multiplies the color of the Material with the color behind the surface. This creates a darker effect, like when you look through colored glass. |
| __Render Face__     | Use this drop-down to determine which sides of your geometry to render.<br/>__Front Face__ renders the front face of your geometry and [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the back face. This is the default setting. <br/>__Back Face__ renders the front face of your geometry and culls the front face. <br/>__Both__ makes URP render both faces of the geometry. This is good for small, flat objects, like leaves, where you might want both sides visible. |
| __Alpha Clipping__  | Makes your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) Shader. Use this to create a transparent effect with hard edges between the opaque and transparent areas. For example, to create blades of grass. To achieve this effect, URP does not render alpha values below the specified __Threshold__, which appears when you enable __Alpha Clipping__.  You can set the __Threshold__ by moving the slider, which accepts values from 0 to 1. All values above your threshold are fully opaque, and all values below your threshold are invisible. For example, a threshold of 0.1 means that URP doesn't render alpha values below 0.1. The default value is 0.5. |
| __Receive Shadows__ | Tick this box to enable your GameObject to have shadows cast upon it by other objects. If you untick this box, the GameObject will not have shadows on it. |



### Surface Inputs

The __Surface Inputs__ describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth. 

**Note:** If you are used to the [Standard Shader](https://docs.unity3d.com/Manual/Shader-StandardShader.html) in the built-in Unity render pipeline, these options are similar to the Main Maps settings in the [Material Editor](https://docs.unity3d.com/Manual/StandardShaderContextAndContent.html).



| Property                    | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| __Base Map__                | Adds color to the surface, also known as the diffuse map. To assign a Texture to the __Base Map__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the Textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned Texture. To assign another tint, you can click this color swatch. If you select __Transparent__ or __Alpha Clipping__ under __Surface Options__, your Material uses the Texture’s alpha channel or color. |
| __Metallic / Specular Map__ | Shows a map input for your chosen __Workflow Mode__ in the __Surface Options__.<br/>For the [__Metallic Map__](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterMetallic.html) workflow, the map gets the color from the __Base Map__ assigned above. Use the slider to control how metallic the surface appears. 1 is fully metallic, like silver or copper, and 0 is fully dielectric, like plastic or wood. You can generally use values in between 0 and 1 for dirty or corroded metals.<br/>For the [__Specular Map__](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterSpecular.html) setting, you can assign a texture to it by clicking the object picker next to it. This opens the Asset Browser, where you can select from the Textures in your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html).<br/>For both workflows, you can use the __Smoothness__ slider to control the spread of highlights on the surface. 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between produce semi-glossy looks. For example, 0.5 produces a plastic-like glossiness.<br/>Use the __Source__ drop-down menu to select where the shader samples a smoothness map from. Options are: __Metallic Alpha__ (alpha channel from the metallic map), and __Albedo Alpha__ (alpha channel from the base map). The default value is __Metallic Alpha__.<br/>If the selected source has the alpha channel, the shader samples the channel and multiplies each sample by the __Smoothness__ value. |
| __Normal Map__              | Adds a normal map to the surface. With a [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html?), you can add surface details like bumps, scratches and grooves. To add the map, click the object picker next to it. The normal map picks up ambient lighting in the environment. <br/>The float value next to the setting is a multiplier for the effect of the  __Normal Map__. Low values decrease the effect of the normal map. High values create stronger effects. |
| __Height Map__              | URP implements the parallax mapping technique which uses the [height map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterHeightMap.html?) to achieve surface-level occlusion effect by shifting the areas of the visible surface texture. To add the map, click the object picker next to it. <br/>The float value next to the setting is a multiplier for the effect of the __Height Map__. Low values decrease the effect of the height map. High values create stronger effects. |
| __Occlusion Map__           | Select an [occlusion map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterOcclusionMap.html).  This simulates shadows from ambient light and reflection, which makes lighting look more realistic as less light reaches corners and crevices of objects. To select the occlusion map, click the object picker next to it. |
| __Clear Coat Map__          | Adds a second material layer which simulates a transparent and thin coating on top of the base material. The feature is enabled by checking the Clear Coat checkbox. Setting the mask to 0 does not disable the feature. Clear coat has roughly twice the cost of a base material as lighting is evaluated once per layer. Clear coat has effect mask (intensity) feature. It's IOR is fixed to 1.5. The clear coat can be controlled using a map where the red channel represents the mask property. If a map is provided then the constant values for mask is used as a multiplier for the map's per pixel values. The clear coat affects slightly the color and the smoothness values of the underlying base material. Clear coat support on OpenGL ES is restricted to 3.1 and up.|
| __Emission__                | Makes the surface look like it emits lights. When enabled, the  __Emission Map__ and __Emission Color__ settings appear.<br/>To assign an __Emission Map__, click the object picture next to it. This opens the Asset Browser, where you can select from the textures in your Project.<br/>For __Emission Color__, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html) to assign a tint on top of the color. This can be more than 100% white, which is useful for effects like lava, that shines brighter than white while still being another color.<br/>If you have not assigned an __Emission Map__, the __Emission__ setting only uses the tint you’ve assigned in __Emission Color__.<br/>If you do not enable __Emission__, URP sets the emission to black and does not calculate emission. |
| __Tiling__                  | A 2D multiplier value that scales the Texture to fit across a mesh according to the U and V axes. This is good for surfaces like floors and walls. The default value is 1, which means no scaling. Set a higher value to make the Texture repeat across your mesh. Set a lower value to stretch the Texture. Try different values until you reach your desired effect. |
| __Offset__                  | The 2D offset that positions the Texture on the mesh.  To adjust the position on your mesh, move the Texture across the U or V axes. |

### Detail Inputs

Use the __Detail Inputs__ settings to add extra details to the surface.

__Requirement__: GPU supporting shader model 2.5 or higher.

| Property                    | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| __Mask__                    | Select a texture that defines areas where Unity overlays the Detail maps over the Surface Inputs maps. The mask uses the alpha channel of the selected texture. The __Tiling__ and __Offset__ settings have no effect on the mask. |
| __Base Map__                | Select the texture containing the surface details. Unity blends this map with the Surface Base Map using the overlay mode. |
| __Normal Map__              | Select the texture containing the normal vector data. Use a [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html?) to add surface details like bumps, scratches and grooves.<br/>Use the slider next to the setting to change the intensity of the effect of the map. The default value is 1. |
| __Tiling__                  | Use this setting to scale the __Base Map__ and the __Normal Map__ on the mesh along the U and V axes, so that the maps fit the mesh best. The default value is 1. Select a value higher than one to make the maps repeat themselves across the mesh. Set a value lower than 1 to stretch the maps. |
| __Offset__                  | The offset that moves the __Base Map__ and the __Normal Map__ on the mesh along the U and V axes. |

### Advanced

The __Advanced__ settings affect the underlying calculations of your rendering. They do not have a visible effect on your surface.

| Property                    | Description                                                  |
| --------------------------- | ------------------------------------------------------------ |
| __Specular Highlights__     | Enable this to allow your Material to have specular highlights from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). This means that your Material reflects the shine from these light sources. Disable this to leave out these highlight calculations, so your Shader renders faster. By default, this feature is enabled. |
| __Environment Reflections__ | Sample reflections using the nearest [Reflection Probe](https://docs.unity3d.com/Manual/class-ReflectionProbe.html), or, if you have set one in the [Lighting](https://docs.unity3d.com/Manual/GlobalIllumination.html) window, the [Lighting Probe](https://docs.unity3d.com/Manual/LightProbes.html). If you disable this, you will have fewer Shader calculations, but this also means that your surface has no reflections. |
| __Enable GPU Instancing__   | Makes URP render meshes with the same geometry and Material in one batch, when possible. This makes rendering faster. URP cannot render Meshes in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Priority__                | Use this slider to determine the chronological rendering order for a Material. URP renders Materials with higher values first. You can use this to reduce overdraw on devices by making the pipeline render Materials in front of other Materials first, so it doesn't have to render overlapping areas twice. This works similarly to the [render queue](https://docs.unity3d.com/ScriptReference/Material-renderQueue.html) in the built-in Unity render pipeline. |


## Channel packing

This Shader uses [channel packing](http://wiki.polycount.com/wiki/ChannelPacking), so you can use a single RGBA texture for the metallic, smoothness and occlusion properties. When you use texture packing, you only have to load one texture into memory instead of three separate ones. When you write your texture maps in a program like Substance or Photoshop, you can pack the maps like this:

| Channel   | Property   |
| :-------- | ---------- |
| **Red**   | Metallic   |
| **Green** | Occlusion  |
| **Blue**  | None       |
| **Alpha** | Smoothness |
