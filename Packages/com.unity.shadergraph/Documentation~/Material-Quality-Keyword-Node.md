# Material Quality Keyword node

The Material Quality Keyword node adds and uses global keywords MATERIAL_QUALITY_HIGH, MATERIAL_QUALITY_MEDIUM and MATERIAL_QUALITY_LOW and enables different behaviors for each one of the available quality types.

**Note**:
* Adding this keyword increases the amount of shader variants.
* These quality keywords are only available in URP and HDRP, they are not available at the Built-in Render Pipeline.

To manually set the quality level from a C# script, use the `UnityEngine.Rendering.MaterialQualityUtilities.SetGlobalShaderKeywords(...)` function.

## Additional resources:

* [Keywords](Keywords.md)
