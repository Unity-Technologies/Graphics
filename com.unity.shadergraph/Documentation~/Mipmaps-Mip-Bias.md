# Mips and mipmaps

A mip or mip level is a specific version of a texture with a specific resolution or level of detail. Mips exist in chains called mipmaps, which are progressively smaller and lower resolution versions of a single texture.

For example, a mipmap could contain 4 versions of a texture, from the original texture (Mip 0), to Mip 1, Mip 2, and Mip 3:

![](images/sg-mipmaps-example.png)

The GPU uses a calculation based on the UV space between the current pixel and its neighboring pixels to determine which mip level to use for a texture. The GPU knows the UV coordinates for the current pixel, but needs to know how much of the texture to apply to that pixel, based on two values: DDX and DDY.

The DDX and DDY values provide information about the UV coordinate position of the pixels beside and above the current pixel. They also tell the GPU about the UV space between the current pixel and its neighbors, including distances and angles. A greater distance and a more extreme angle between the current pixel and its neighbors means that the GPU should pick a lower resolution mip; a shorter distance and less extreme angle means that the GPU should pick a mip with a higher resolution.

The GPU can also blend the texture information from two mips together, by taking a specific percentage of texture information from one mip and the rest from another mip. Blending mips while sampling can reduce undesirable results, like aliasing, when displaying GameObjects in 3D space.

You can create a mipmap for a texture manually, or you can instruct Unity to generate a mipmap for you. To automatically generate a mipmap, you should make sure that your original texture's resolution is a power of two value, as shown in the example mipmap image.

For more information on generating mipmaps in Unity, see [Texture Import Settings](https://docs.unity3d.com/Manual/class-TextureImporter.html#advanced) in the Unity User Manual.

## Mip bias

The percentage of texture information taken from one mip during sampling can be biased by the rendering pipeline when using systems like Dynamic Resolution Scaling (DRS). This mip bias tells the GPU to prefer one mip over another by an exact percentage when sampling.

For example, when selecting a mip, the GPU's calculations could return a value of `0.5`. The `0.5` value tells the GPU to take 50% of the texture information it needs from one mip, and the remaining 50% from the next mip in the mipmap. With an added mip bias of `0.2`, the `0.5` value would change to `0.7`, and the GPU would take 70% of the texture information from the first mip and only 30% from the second.

When sampling using the Sample Texture 2D Array or Sample Texture 2D node in Shader Graph, you can set your own bias value to use in your mip calculations. Depending on your settings, your specified bias increases, decreases, or completely replaces the Global Mip Bias added to any sampling. For more information, see [Sample Texture 2D Array node](Sample-Texture-2D-Array-Node.md) or [Sample Texture 2D node](Sample-Texture-2D-Node.md).
