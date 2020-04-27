# 2D Renderer Data Asset

![The 2D Renderer Data Asset property settings](Images/2D/2dRendererData_properties.png)

The __2D Renderer Data__ Asset contains the settings that affect the way __2D Lights__ are applied to lit Sprites. You can set the way Lights emulate HDR lighting with the [HDR Emulation Scale](HDREmulationScale), or customize your own [Light Blend Styles](LightBlendStyles). Refer to their respective pages for more information about their properties and options.

## Use Depth/Stencil Buffer

This option is enabled by default. Clear this option to disable the Depth/[Stencil](https://docs.unity3d.com/Manual/SL-Stencil.html) Buffer. Doing so might improve your project’s performance, especially on mobile platforms. You should clear this option if you are not using any features that require the Depth/Stencil Buffer (such as [Sprite Mask](https://docs.unity3d.com/Manual/class-SpriteMask.html)). 

## Post-processing Data

Unity automatically assigns a default Asset to this property that contains the resources (such as Textures and Shaders) that the post-processing effects require. If you want to use your own post-processing Shaders or lookup Textures, replace the default Asset.

## Default Material Type

![The 2D Renderer Data Asset property settings](Images/2D/Default_Material_Type.png)

Unity assigns a Material of the selected __Default Material Type__ to Sprites when they are created. The available options have the following properties and functions.

__Lit__:  Unity assigns a Material with the Lit type (default Material: Sprite-Lit-Default). 2D Lights affect Materials of this type. 

__Unlit__: Unity assigns a Material with the Unlit type (default Material:  Sprite-Lit-Default). 2D Lights do not affect Materials of this type.

__Custom__: Unity assigns a Material with the Custom type. When you select this  option, Unity shows the __Default Custom Material__ box. Assign the desired Material to this box.

![The 2D Renderer Data Asset property settings](Images/2D/Default_Custom_Material.png)