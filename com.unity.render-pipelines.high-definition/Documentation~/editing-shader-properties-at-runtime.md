# Editing shader properties at runtime

For High Definition Render Pipeline (HDRP) non-Shader Graph shaders, such as the [Lit](Lit-Shader.md), [Unlit](Unlit-Shader.md) shaders, changing a property at runtime, in some cases, does not have any effect. This is because if the property is not enabled before runtime, the specific shader variant associated with this property is not included in the shader (and is also not included when bulding a player). To include the shader variant, the proper keywords need to be enabled on the material before editing. 

For example, to assign an emissive texture in an already blank emissive [Lit](Lit-Shader.md) shader at runtime, before setting the texture, this specific keyword needs to be [enabled](https://docs.unity3d.com/ScriptReference/Material.EnableKeyword.html) for the emissive color map variant to be included. 

```
this.GetComponent<Renderer>().material.EnableKeyword("_EMISSIVE_COLOR_MAP");
```

The list of the keywords can be found by right-clicking on the shader itself (in the material inspector header) and select "Edit Shader".
