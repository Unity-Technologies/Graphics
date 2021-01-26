The following are 2D related features that utilize the __Universal Render Pipeline__(URP):

When using URP with the **2D Renderer** selected, the **Light 2D** component introduces a way to apply 2D optimized lighting to Sprites.

You can choose from several different Light Types with the **Light 2D** component. The Light Types currently available in the package are:

- [Freeform](LightTypes.html#freeform)
- [Sprite](LightTypes.html#sprite)
- [Spot](LightTypes.html#spot) (**Note:** The **Point** Light Type has been renamed to the **Spot** Light Type from URP 11 onwards.)
- [Global](LightTypes.html#global)

**Important:** The [Parametric Light Type](LightTypes.html#parametric) is deprecated from URP 11 onwards. To convert existing Parametric lights to Freeform lights, go to **Edit > Render Pipeline > Universal Render Pipeline > Upgrade Project/Scene Parametric Lights to Freeform**

![](Images/2D/2d-lights-gameobject-menu.png)

The package includes the __2D Renderer Data__ Asset which contains the __Blend Styles__ parameters, and allows you to create up to four custom Light Operations for your Project.

**Note:** If you have the experimental 2D Renderer enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), some of the options related to 3D rendering in the URP Asset don't have any impact on your final app or game.
