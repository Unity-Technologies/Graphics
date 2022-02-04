# Mips and mipmaps

A mip or mip level is a version of a Texture with a specific resolution. Mips exist in sets called mipmaps. Mipmaps contain progressively smaller and lower resolution versions of a single Texture.

For example, a mipmap might contain 4 versions of a Texture, from the original Texture (Mip 0), to Mip 1, Mip 2, and Mip 3:

![An image displaying a checkerboard Texture in different sizes. The first Texture, Mip 0, is the original Texture and has a resolution of 128×128. The second Texture is Mip 1, and has a resolution of 64×64. The third Texture is Mip 2, and has a resolution of 32×32. The last Texture is Mip 3, and has a resolution of 16×16.](images/sg-mipmaps-example.png)

Mipmaps are commonly used for rendering objects in 3D scenes, where textured objects can vary in distance from the camera. A higher mip level is used for objects closer to the camera, and lower mip levels are used for more distant objects.

Mipmaps can speed up rendering operations and reduce rendering artifacts in situations where the GPU renders a Texture at less than its full resolution. A mip level is effectively a cached, downsampled version of the original Texture. Instead of performing many sampling operations on the original, full resolution Texture, the GPU can perform a smaller number of operations on the already downsampled version.

Sometimes, mipmaps aren't beneficial. Mipmaps increase the size of a Texture by 33%, both on disk and in memory. They also provide no benefit when a Texture is only rendered at its full resolution, such as a UI Texture that isn't scaled.

You can create a mipmap for a Texture manually, or you can instruct Unity to generate a mipmap for you. To automatically generate a mipmap, you should make sure that your original Texture's resolution is a power of two value, as shown in the example mipmap image.

For more information on generating mipmaps in Unity, see [Texture Import Settings](https://docs.unity3d.com/Manual/class-TextureImporter.html#advanced) in the Unity User Manual.

## How the GPU samples mip levels

When the GPU samples a Texture, it determines which mip level to use based on the UV coordinates for the current pixel, and two internal values that the GPU calculates: DDX and DDY.

DDX and DDY provide information about the UV coordinates of the pixels beside and above the current pixel, including distances and angles.

The GPU uses these values to determine how much of a Texture's detail is visible to the camera. A greater distance and a more extreme angle between the current pixel and its neighbors means that the GPU should pick a lower resolution mip; a shorter distance and less extreme angle means that the GPU should pick a mip with a higher resolution.

The GPU can also blend the Texture information from two mips together when using Deep Learning Super Sampling (DLSS) or dynamic resolution scaling (DRS). Blending mips while sampling can reduce undesirable results, like aliasing, when displaying GameObjects in 3D space.

The GPU takes a specific percentage of Texture information from one mip and the rest from another mip. The exact percentage is determined by the mip bias.

## Mip bias

A GPU's global mip bias tells it to prefer one mip over another by an exact percentage when combining samples from different mips. The global mip bias is applied automatically when using systems like DLSS and DRS.

For example, when selecting a mip, the GPU's calculations could return a value of `0.5`. The `0.5` value tells the GPU to take 50% of the Texture information it needs from one mip, and the remaining 50% from the next mip in the mipmap. With an added mip bias of `0.2`, the `0.5` value would change to `0.7`, and the GPU would take 70% of the Texture information from the first mip and only 30% from the second.

Textures can have an individual mip bias, and some Unity features allow you to specify your own mip bias.

When sampling using the Sample Texture 2D Array or Sample Texture 2D node in Shader Graph, you can also set your own bias value to use in your mip calculations. Depending on your settings, your specified bias increases, decreases, or completely replaces the global mip bias added to any sampling. For more information, see [Sample Texture 2D Array node](Sample-Texture-2D-Array-Node.md) or [Sample Texture 2D node](Sample-Texture-2D-Node.md).
