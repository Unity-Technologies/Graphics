**Note:** This page is subject to change during the 2019.1 beta cycle.

# Shader Stripping

Unity compiles many shader variants from a single shader source file. The number of shader variants depends on how many keywords you’ve included in the shader. In the default shaders, the Lightweight Render Pipeline uses a set of keywords for lighting and shadows. LWRP can exclude some shader variants, depending on which features are active in the [LWRP Asset](lwrp-asset.md).

When you disable [certain features](shader-stripping-keywords.md) in the LWRP Asset, the pipeline “strips” the related shader variants from the build. Stripping your shaders gives you smaller build sizes and shorter build times. This is useful if your project is never going to use certain features or keywords.

For example, you might have a project where you never use shadows for directional lights. Without shader stripping, shader variants with directional shadow support remain in the build. In that case, you can uncheck  **Cast Shadows** in the LWRP Asset for either main or additional direction lights. LWRP then strips these variants from the build.

For more information about stripping shader variants in Unity, see [this blog post by Christophe Riccio](https://blogs.unity3d.com/2018/05/14/stripping-scriptable-shader-variants/).