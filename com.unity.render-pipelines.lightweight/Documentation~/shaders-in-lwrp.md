# Shaders in LWRP

The Lightweight Render Pipeline uses a different shading approach than the Unity built-in Render Pipeline. As a result, built-in Lit and custom Lit shaders do not work with the LWRP. Instead, LWRP has a new set of standard shaders. LWRP provides the following shaders for the most common use case scenarios:

- [Lit](lit-shader.md)
- [Simple Lit](simple-lit-shader.md)
- [Baked Lit](baked-lit-shader.md)
- [Unlit](unlit-shader.md)
- [Particles Lit](particles-lit-shader.md)
- [Particles Simple Lit](particles-simple-lit-shader.md)
- [Particles Unlit](particles-unlit-shader.md)
- Autodesk Interactive
- Autodesk Interactive Transparent
- Autodesk Interactive Masked

**Upgrade advice:** If you upgrade your current Project to LWRP, you can [upgrade](upgrading-your-shaders.md) built-in shaders to the new ones. Unlit shaders from the built-in render pipeline still work with LWRP.

**Note:** Unlit shaders from the Unity built-in render pipeline work in LWRP.

## Choosing a Shader 

With the Lightweight Render Pipeline, you can have real-time lighting wither either Physically Based Shaders (PBS) and non-Physically Based Rendering (PBR).

For PBS, use the [Lit shader](lit-shader.md). You can use it on all platforms. The shader quality scales, depending on the platform, but keeps physically based rendering on all platforms. This gives you realistic graphics across hardware. The Unity [Standard Shader](<https://docs.unity3d.com/Manual/shader-StandardShader.html>) and the [Standard (Specular setup)](https://docs.unity3d.com/Manual/StandardShaderMetallicVsSpecular.html) Shaders both map to the Lit Shader in LWRP. For a list of Shader mappings, see [Shader mappings](upgrading-your-shaders.md#shaderMappings)

If you’re targeting less powerful devices, or just would like simpler shading, use the [Simple Lit Shader](simple-lit-shader.md), which is non-PBR. 

If you don’t need real-time lighting, or would rather only use [baked lighting](https://docs.unity3d.com/Manual/LightMode-Baked.html) and sample global illumination, choose a Baked Lit Shader. 

If you don’t need lighting in on a material at all, you can choose the an Unlit Shader. 
