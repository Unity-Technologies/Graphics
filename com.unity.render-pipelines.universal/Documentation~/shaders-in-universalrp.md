# Shaders and Materials

The Universal Render Pipeline uses a different shading approach than the Unity built-in Render Pipeline. As a result, built-in Lit and custom Lit Shaders do not work with the URP. Instead, URP has a new set of standard Shaders. URP provides the following Shaders for the most common use case scenarios:

- [Lit](lit-shader.md)
- [Simple Lit](simple-lit-shader.md)
- [Baked Lit](baked-lit-shader.md)
- [Unlit](unlit-shader.md)
- [Particles Lit](particles-lit-shader.md)
- [Particles Simple Lit](particles-simple-lit-shader.md)
- [Particles Unlit](particles-unlit-shader.md)
- [SpeedTree](speedtree.md)
- Autodesk Interactive
- Autodesk Interactive Transparent
- Autodesk Interactive Masked

**Upgrade advice:** If you upgrade your current Project to URP, you can [upgrade](upgrading-your-shaders.md) built-in Shaders to the new ones. Unlit Shaders from the built-in render pipeline still work with URP.

For [SpeedTree](https://docs.unity3d.com/Manual/SpeedTree.html) Shaders, Unity does not re-generate Materials when you re-import them, unless you click the Generate Materials or Apply & Generate Materials button.

**Note:** Unlit Shaders from the Unity built-in render pipeline work in URP.

## Choosing a Shader

With the Universal Render Pipeline, you can have real-time lighting with either Physically Based Shaders (PBS) and non-Physically Based Rendering (PBR).

For PBS, use the [Lit Shader](lit-shader.md). You can use it on all platforms. The Shader quality scales depending on the platform, but keeps physically based rendering on all platforms. This gives you realistic graphics across hardware. The Unity [Standard Shader](https://docs.unity3d.com/Manual/shader-StandardShader.html) and the [Standard (Specular setup) Shaders](https://docs.unity3d.com/Manual/StandardShaderMetallicVsSpecular.html) both map to the Lit Shader in URP. For a list of Shader mappings, see section [Shader mappings](upgrading-your-shaders.md#built-in-to-urp-shader-mappings).

If you are targeting less powerful devices, or your project has simpler shading, use the [Simple Lit shader](simple-lit-shader.md), which is non-PBR.

If you don’t need real-time lighting, or would rather only use [baked lighting](https://docs.unity3d.com/Manual/LightMode-Baked.html) and sample global illumination, choose a Baked Lit Shader.

If you don’t need lighting on a Material at all, you can choose the Unlit Shader.

## SRP Batcher compatibility

To ensure that a Shader is SRP Batcher compatible:
* Declare all Material properties in a single CBUFFER called `UnityPerMaterial`.
* Declare all built-in engine properties, such as `unity_ObjectToWorld` or `unity_WorldTransformParams`, in a single CBUFFER called `UnityPerDraw`.

For more information on the SRP Batcher, see the page [Scriptable Render Pipeline (SRP) Batcher](https://docs.unity3d.com/Manual/SRPBatcher.html).
