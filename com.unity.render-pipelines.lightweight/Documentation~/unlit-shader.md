**Note:** This page is subject to change during the 2019.1 beta cycle.

# Unlit shader 

Use this shader for stylised games or apps that do not require direct lighting. Because of the lack of light calculations, you can expect high performance from these games.


## Using the Unlit shader in the Editor

To create a new Material with the shader:
1. In your Project window, click __Create__ > __Material__. Select the __Unlit__ shader.

To select and use this shader:
1. In your Project, create or find the Material you want to use the shader on.  Select the __Material__. A Material Inspector window opens. 
2. Click __Shader__, and select __Lightweight Render Pipeline__ > __Unlit__.

## UI overview 

The Inspector window contains these elements: 

* __[Surface Options](#surface-options)__
* __[Surface Inputs](#surface-inputs)__
* __[Advanced](#advanced)__

You can read more about each section in the following overviews.

![Inspector for the Unlit shader](Images/Inspectors/Shaders/StdUnlit.png)

### Surface Options 

The __Surface Options__ control how the Material is rendered on a screen.

| Property | Description |
| ------------ | --- |
| __Surface Type__ | In this drop-down menu, choose between an __Opaque__ or __Transparent__ surface type. __Opaque__ surface types are fully visible, without any considerations of what’s behind them. __Transparent__ surface types take their background into account, and they can vary according to which type of transparent surface type, you choose. If you select __Transparent__, a second dropdown appears (see Transparent property description below). |
| __Transparent__ surface type | __Alpha__ uses the alpha value to change how visible an object is. 1 is fully opaque, 0 is fully transparent.<br/> __Premultiply__ applies a similar effect as __Alpha__, but only keeps reflections and highlights, even when your surface is transparent. This means that only the reflected light is visible. For example, imagine transparent glass.<br/> __Additive__ adds an extra layer on top of another surface. This is good for holograms. <br/> __Multiply__ multiplies the colors behind the surface, like colored glass. |
| __Two Sided__ | Enable this to render on both sides of your geometry. When disabled, Unity [culls](https://docs.unity3d.com/Manual/SL-CullAndDepth.html) the backface of your geometry and only renders the frontface. For example, __Two Sided__ rendering is good for small, flat objects, like leaves, where you might want both sides visible. By default, this setting is disabled, so that Unity culls backfaces. |
| __Alpha Clip__ | Enable this to make your Material act like a [Cutout](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterRenderingMode.html) shader. With this, you can create a transparent effect with hard edges between the opaque and transparent areas. For example, to create straws of grass. Unity achieves this effect by not rendering Alpha values below the specified __Clip Threshold__, which appears when you enable __Alpha Clip__.|

### Surface Inputs
The __Surface Inputs__ describe the surface itself. For example, you can use these properties to make your surface look wet, dry, rough, or smooth. 

| Property | Description |
| ------------ | --- |
| __MainTex__ | This is the color of the surface, also known as the diffuse map. To assign a texture to the __MainTex__ setting, click the object picker next to it. This opens the Asset Browser, where you can select from the textures on your Project. Alternatively, you can use the [color picker](https://docs.unity3d.com/Manual/EditingValueProperties.html). The color next to the setting shows the tint on top of your assigned texture. To assign another tint, you can click this color swatch. If you select __Transparent__ or __Alpha Clip__ under __Render Properties__, your __Material__ uses the texture’s alpha channel or color. |
| __Sample GI__ | With this enabled, you can create a surface that uses [Light Probe](https://docs.unity3d.com/Manual/LightProbes.html) data to add ambient lighting. When enabled, the  __Normal map__ setting appears. Here, you can assign a tangent-space [normal map](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterNormalMap.html), similar to the one in the Standard Shader in the built-in render pipeline. To read more about tangent-spaced normal maps, [see this article on Polycount](http://wiki.polycount.com/wiki/Normal_Map_Technical_Details#Tangent-Space_vs._Object-Space). If you do not enable __Sample GI__, the render pipeline considers ambient as white and skips sampling the Light Probe data. |
| __Tiling__ | A 2D multiplier value that scales the texture to fit across a mesh according to the U and V axes. This is good for surfaces like floors and walls. The default value is 1, which means no scaling. Set a higher value to make the texture repeat across your mesh. Set a lower value to stretch the texture. Try different values until you reach your desired effect. |
| __Offset__ | The 2D offset that positions the texture on the mesh.  To adjust the map position on your mesh, move the texture across the U or V axes. |


### Advanced

The __Advanced__ settings affect the underlying calculations of your rendering. They do not have a visible effect on your surface.

Property | Description
---|---
__GPU Instancing__ | Make Unity render meshes with the same geometry and Material/shader in one batch, when possible. This makes rendering faster.  Meshes cannot be rendered in one batch if they have different Materials or if the hardware does not support GPU instancing. 
__Double Sided Global Illumination__ | Make the surface act double-sided during lightmapping. When enabled, backfaces bounce light like frontfaces, but Unity still doesn’t render them. 


