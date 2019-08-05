# Overview

When using [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universalrp@latest/index.html) (UniversalRP) with the **2D Renderer** selected, the **Light 2D** component introduces a way to apply 2D optimized lighting to Sprites. 

You can choose from several different light types with the **Light 2D** component. The light types currently available in the package are:

- [Freeform](LightTypes.html#freeform) 
- [Sprite](LightTypes.html#sprite)
- [Parametric](LightTypes.html#parametric)
- [Point](LightTypes.html#point)
- [Global](LightTypes.html#global)

![](images\image_1.png)

The package includes the __2D Renderer Data__ Asset which contains the __Blend Styles__ parameters, and allows you to create up to four custom Light Operations for your Project.  


**Note:** If you have the experimental 2D Renderer enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), some of the options related to 3D rendering in the UniversalRP Asset don't have any impact on your final app or game.

