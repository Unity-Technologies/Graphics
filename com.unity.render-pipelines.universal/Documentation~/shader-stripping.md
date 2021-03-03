# Shader Stripping

Unity compiles many Shader Variants from a single Shader source file. The number of Shader Variants depends on how many keywords you’ve included in the Shader. In the default Shaders, the Universal Render Pipeline (URP) uses a set of keywords for lighting and shadows. URP can exclude some Shader variants, depending on which features are active in the [URP Asset](universalrp-asset.md).

When you disable certain features in the URP Asset, the pipeline “strips” the related Shader variants from the build. Stripping your Shaders gives you smaller build sizes and shorter build times. This is useful if your project is never going to use certain features or keywords.

For example, you might have a project where you never use shadows for directional lights. Without Shader stripping, Shader variants with directional shadow support remain in the build. If you know you won't use these shadows at all, you can uncheck **Cast Shadows** in the URP Asset for main or additional direction lights. URP then strips these Shader Variants from the build.

For more information about stripping Shader Variants in Unity, see [this blog post by Christophe Riccio](https://blogs.unity3d.com/2018/05/14/stripping-scriptable-shader-variants/).
