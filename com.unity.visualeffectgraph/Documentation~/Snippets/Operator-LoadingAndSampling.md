## Loading and sampling

In the Visual Effect Graph, there are multiple Operators that can read texel values from a texture. In the underlying (HLSL), some of them use Load() and others use Sample().

The differences between the Operators that use Load() and Operators that use Sample() is as follows:

* Load() does not apply any filtering to the final texel value whereas Sample() uses the same **Filter Mode** as the target **Texture**'s [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).
* Load() does not apply any wrapping and instead returns 0 for coordinates that specify a texel outside the texture. Sample() uses the same **Wrap Mode** as the target **Texture**'s [import settings](https://docs.unity3d.com/Manual/class-TextureImporter.html).
* Load() uses texel coordinates (in the range of 0 to the texture's width/height minus 1) whereas Sample() uses UV coordinates (in the range of 0-1).
