# Introduction to Lights 2D

When using [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) (URP) with the __2D Renderer__ selected, the __Light 2D__ component introduces a way to apply 2D optimized lighting to Sprites. 

You can choose from several different light types with the __Light 2D__ component. The light types currently available in the package are:

- [Freeform](LightTypes.html#freeform) 
- [Sprite](LightTypes.html#sprite)
- [Parametric](LightTypes.html#parametric)
- [Point](LightTypes.html#point)
- [Global](LightTypes.html#global)

![](Images/2D/image_1.png)

The package includes the __2D Renderer Data__ Asset which contains the __Blend Styles__ parameters, and allows you to create up to four custom Light Operations for your Project.  


__Note:__ If you have the experimental 2D Renderer enabled (menu: __Graphics Settings__ > add the 2D Renderer Asset under __Scriptable Render Pipeline Settings__), some of the options related to 3D rendering in the URP Asset don't have any impact on your final app or game.

