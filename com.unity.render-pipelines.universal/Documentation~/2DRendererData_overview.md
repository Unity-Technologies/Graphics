# 2D Renderer Data Asset

![The 2D Renderer Data Asset property settings](images\2dRendererData_properties_updated.png)

The __2D Renderer Data__ Asset contains the settings that affect the way Light is applied to lit Sprites. You can set the way Lights emulate HDR lighting with the [HDR Emulation Scale](HDREmulationScale), or customize your own [Light Blend Styles](LightBlendStyles). 

## Use Depth/Stencil Buffer

This option is enabled by default. Clear this option to disable the Depth/[Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html) Buffer. Doing so might improve your project’s performance, especially on mobile platforms. You should clear this option if you are not using any features that require the Depth/Stencil Buffer (such as [Sprite Mask](https://docs.unity3d.com/Manual/class-SpriteMask.html)). 

## Post-processing Data

Unity automatically assigns a default Asset to this property that contains the resources (such as Textures and Shaders) that the post-processing effects require. If you want to use your own post-processing Shaders or lookup Textures, replace the default Asset.