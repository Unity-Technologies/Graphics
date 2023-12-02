# Shaders and Materials

URP provides the following Shaders for the most common use case scenarios:

* [Complex Lit](shader-complex-lit.md)
* [Lit](lit-shader.md)
* [Simple Lit](simple-lit-shader.md)
* [Baked Lit](baked-lit-shader.md)
* [Unlit](unlit-shader.md)
* [Terrain Lit](shader-terrain-lit.md)
* [Particles Lit](particles-lit-shader.md)
* [Particles Simple Lit](particles-simple-lit-shader.md)
* [Particles Unlit](particles-unlit-shader.md)
* [SpeedTree](speedtree.md)
* [Decal](decal-shader.md)
* Autodesk Interactive
* Autodesk Interactive Transparent
* Autodesk Interactive Masked

## Shader compatibility

Lit and custom Lit shaders written for the Built-in Render Pipeline are not compatible with URP.

Unlit shaders written for the Built-in Render Pipeline are compatible with URP.

For information on converting shaders written for the Built-in Render Pipeline to URP shaders, refer to the documentation on [Converting your shaders](upgrading-your-shaders.md).

## Choosing a shader

The Universal Render Pipeline implements Physically Based Rendering (PBR).

The pipeline provides pre-built shaders that can simulate real world materials.

PBR materials provide a set of parameters that let artists achieve consistency between different material types and under different lighting conditions.

The URP [Lit shader](lit-shader.md) is suitable for modeling most of the real world materials. The [Complex Lit shader](shader-complex-lit.md) is suitable for simulating advanced materials that require more complex lighting evaluation, such as the clear coat effect.

URP provides the [Simple Lit shader](simple-lit-shader.md) as a helper to convert non-PBR projects made with the Built-in Render Pipeline to URP. This shader is non-PBR and is not supported by Shader Graph.

If you don’t need real-time lighting, or would rather only use [baked lighting](https://docs.unity3d.com/Manual/LightMode-Baked.html) and sample global illumination, choose a Baked Lit Shader.

If you don’t need lighting on a Material at all, you can choose the Unlit Shader.

## SRP Batcher compatibility

To ensure that a Shader is SRP Batcher compatible:
* Declare all Material properties in a single CBUFFER called `UnityPerMaterial`.
* Declare all built-in engine properties, such as `unity_ObjectToWorld` or `unity_WorldTransformParams`, in a single CBUFFER called `UnityPerDraw`.

For more information on the SRP Batcher, refer to the documentation on the [Scriptable Render Pipeline (SRP) Batcher](https://docs.unity3d.com/Manual/SRPBatcher.html).
