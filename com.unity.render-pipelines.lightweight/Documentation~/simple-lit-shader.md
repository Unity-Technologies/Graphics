**Note:** This page is subject to change during the 2019.1 beta cycle.

# Simple Lit shader 

The Simple Lit shader is perfect for when performance is more important than photorealism. This shader uses a simple approximation for lighting. Because this shader [does not calculate for physical correctness and energy conservation](lighting_model.md#non-physically_based_shaders), it renders quickly.


## Using the Simple Lit shader in the Editor 
You can either create a new Material with the shader or select the shader from the Material inspector.

To create a new Material with the shader:
1. In your Project window, click __Create__ > __Material__. Select the __Simple Lit__ shader.

To select the shader in the Material inspector:
1. In your Project, select the __Material__ Inspector. 
2. Click __Shader__, and select __Lightweight Render Pipeline__ > __Simple Lit__.


## UI overview 
The Inspector window contains these elements: 

* __[Surface Options](#surface-options)__
* __[Surface Inputs](#surface-inputs)__
* __[Advanced](#advanced)__

![Inspector for the Simple Lit shader](Images/Inspectors/Shaders/StdSimpleLighting.png)

### Surface Options 

The __Surface Options__ control how the Material is rendered on a screen. 

| Property | Description |
| ------------ | --- |
| __Surface Type__ | In this drop-down menu, choose between an __Opaque__ or __Transparent__ surface type. __Opaque__ surface types are fully visible, without any considerations of what’s behind them. __Transparent__ surface types take their background into account, and they can vary according to which type of transparent surface type, you choose. If you select __Transparent__, a second dropdown appears (see Transparent property description below). |
| __Transparent__ surface type | __Alpha__ uses the alpha value to change how visible an object is. 1 is fully opaque, 0 is fully transparent.<br/> __Premultiply__ applies a similar effect as __Alpha__, but only keeps reflections and highlights, even when your surface is transparent. This means that only the reflected light is visible. For example, imagine transparent glass.<br/> __Additive__ adds an extra layer on top of another surface. This is good for holograms. <br/> __Multiply__ multiplies the colors behind the surface, like colored glass. |
| __Two Sided__ | Enable this to render on both sides of your geometry. When disabled, Unity [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the backface of your geometry and only renders the frontface. For example, Two Sided rendering is good for small, flat objects, like leaves, where you might want both sides visible. By default, this setting is disabled, so that Unity culls backfaces. |
| __Alpha Clip__ | Enable this to make your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) shader. With this, you can create a transparent effect with hard edges between the opaque and transparent areas. For example, to create straws of grass. Unity achieves this effect by not rendering Alpha values below the specified __Clip Threshold__, which appears when you enable __Alpha Clip__.|

### Surface Inputs

The __Surface Inputs__ describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth. 

| Property | Description |
| ------------ | --- |
| __Base__ | This is the texture of the surface, also known as the diffuse map. To assign a texture to the __Base__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the textures on your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned texture. To assign another tint, you can click this color swatch. If you select __Transparent__ or __Alpha Clip__ under __Render Properties__, your __Material__ uses the texture’s alpha channel or color. |
| __Specular__ | Enable this to allow your Material to have specular highlights from direct lighting, for example [Directional, Point, and Spot lights](https://docs.unity3d.com/Manual/Lighting.html). This means that your Material reflects the shine from these light sources. Disable this to leave out these highlight calculations, so your shader renders faster. By default, this feature is enabled. When __Specular__ is enabled, you can configure the following properties: __Specular Map__, __Glossiness Source__, and __Shininess__. __Specular Map__  controls the color of your specular highlights. In__Glossiness Source__, you can select a texture in your Project. The Alpha channel for this texture controls the glossiness. The __Shininess__ slider controls the spread of highlights on the surface . 0 gives a wide, rough highlight. 1 gives a small, sharp highlight like glass. Values in between produce semi-glossy looks. For example, 0.5 produces a plastic-like glossiness. |
| __Normal Map__ | Assign a tangent-space [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html), similar to the one in the Standard Shader in the built-in render pipeline. To read more about tangent-space normal maps, [see this article on Polycount](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Tangent-Space_vs._Object-Space). The float value next to the setting is a multiplier for the effect of the  __Normal Map__. |
| __Emission__ | Make the surface look like it emit lights. When enabled, the  __Color__ and __HDR__ settings appear. To assign a texture to your __Color__, click the object picture next to it. This opens the Asset Browser, where you can select from the textures in your Project.  For __HDR__, you can choose the color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html) to assign a HDR tint on top of the color. This can be more than 100% white, which is useful for effects like lava, that shines brighter than white while still being another color. If you have not assigned a __Color__ texture, the __Emission__ setting uses the tint you’ve assigned in HDR.  If you do not enable __Emission__, Unity sets the emission to black and does not calculate emission. |
| __Tiling__ | A 2D multiplier value that scales the texture to fit across a mesh according to the U and V axes. This is good for surfaces like floors and walls. The default value is 1, which means no scaling. Set a higher value to make the texture repeat across your mesh. Set a lower value to stretch the texture. Try different values until you reach your desired effect. |
| __Offset__ | The 2D offset that positions the texture on the mesh.  To adjust the map position on your mesh, move the texture across the U or V axes. |

### Advanced 

The __Advanced__ settings affect behind-the-Scenes rendering. They do not have a visible effect on your surface, but on underlying calculations.

| Property | Description |
| ------------ | --- |
| __GPU Instancing__ | Make Unity render meshes with the same geometry and Material/shader in one batch, when possible. This makes rendering faster.  Meshes cannot be rendered in one batch if they have different Materials or if the hardware does not support GPU instancing. |
| __Double-sided Global Illumination__ | Make the surface act double-sided du ring lightmapping. When enabled, backfaces bounce light like frontfaces, but Unity still doesn’t render them. |





