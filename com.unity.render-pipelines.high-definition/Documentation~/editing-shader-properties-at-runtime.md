# Editing shader properties at runtime

Each combination of shader feature [Keyword](https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html) used in a shader produces a different shader code to compile. This is called a shader variant. Unity tries to reduce the number of shader variants to compile by only considering the sets of kewords used in a material. Until a specific shader feature is used on a material, the corresponding shader variant does not exist. 

As a result, if one tries to edit a material at runtime and change properties that affects which keywords is enabled, Unity is not able to find the shader variant and the rendering of this shader feature fails. 
This is because Unity cannot find the corresponding shader variant since the correct keywords are not enabled in the shader. 

In the editor, enabling the correct keywords allows Unity to compile the corresponding shader variant on the fly. 
When building a player, only the shader variants corresponding to the keywords used on the project materials are compiled. Enabling the correct keywords in a player allows Unity to find the corresponding shader variant but only if it already exists. If not, the rendering of this shader feature fails.

For example, to assign a Normal Map texture in a blank [Lit](Lit-Shader.md) shader material at runtime in Editor, a specific keyword needs to be [enabled](https://docs.unity3d.com/ScriptReference/Material.EnableKeyword.html) for the normal map shader variant to be compiled on the fly. 

```
this.GetComponent<Renderer>().material.EnableKeyword("_NORMALMAP");
```

In a player, the keyword also needs to be enabled and the shader variant needs to exist because it cannot be compiled on the fly.

For High Definition Render Pipeline (HDRP), the list of keywords can be found by right-clicking on the shader itself (in the material inspector header), selecting "Edit Shader" and looking for lines starting with "#pragma shader_feature_local".
