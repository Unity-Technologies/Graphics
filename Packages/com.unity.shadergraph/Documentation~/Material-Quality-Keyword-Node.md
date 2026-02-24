# Material Quality Keyword node

The Material Quality Keyword node adds and uses global keywords MATERIAL_QUALITY_HIGH, MATERIAL_QUALITY_MEDIUM and MATERIAL_QUALITY_LOW and enables different behaviors for each one of the available quality types.

**Note**:
* Adding this keyword increases the amount of shader variants.
* These quality keywords are only available in URP and HDRP, they are not available at the Built-in Render Pipeline.

> [!WARNING]
> Built-In Render Pipeline (BiRP) support in Shader Graph is deprecated and will be removed in a future version. Unity recommends using Shader Graph with the [Universal Render Pipeline (URP)](https://docs.unity3d.com/Manual/urp/urp-introduction.html) or the [High Definition Render Pipeline (HDRP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest) instead.

To manually set the quality level from a C# script, use the `UnityEngine.Rendering.MaterialQualityUtilities.SetGlobalShaderKeywords(...)` function.

## Additional resources:

* [Keywords](Keywords.md)
