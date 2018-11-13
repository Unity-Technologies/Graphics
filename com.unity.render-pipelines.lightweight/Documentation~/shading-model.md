**Note:** This page is subject to change during the 2019.1 beta cycle.

# Shading Models in LWRP

A shading model defines how the object’s color varies depending on factors such as surface orientation, viewer direction, and lighting. Your choice of a shading model depends on the artistic direction and performance budget of your application. The Lightweight Render Pipeline provides shaders with the following shading models:

- [Physically Based Shading](#physically-based-shading)
- [Simple Shading](#simple-shading)
- [Baked Lit Shading](#baked-lit-shading)

## Physically based shading

Physically based shading (PBS) simulates how objects look in real life by computing the amount of light reflected from the surface based on principles of physics. This lets your create photo-realistic objects and surfaces.

This PBS model follows two principles: _energy conservation_, and _microgeometry_. 

_Energy conservation_ means that the surface never reflects more light than the total incoming light. The only exception to this is when an object emits light. For example, think of a neon sign. 

At a microscopic level, surfaces have _microgeometry_. Some objects have smooth microgeometry, which gives them a mirror-like appearance. Other objects have rough microgeometry, which makes them look more dull. In LWRP, you can can mimic the level of smoothness of an object’s surface. 

[img showing comparison between roughness vs smoothness]

When light hits an object surface, part of the light is reflected and part is refracted. The reflected light is called _specular reflection_. This varies depending on the viewer direction and the at which the light hits a surgace, also called the [angle of incidence](<https://en.wikipedia.org/wiki/Angle_of_incidence_(optics)>). In this shading model, the shape of specular highlight is approximated with a [GGX function](https://blogs.unity3d.com/2016/01/25/ggx-in-unity-5-3/). 

For metal objects, the surface absorbs and changes the light. For non-metallic objects, also called [dialetic](<https://en.wikipedia.org/wiki/Dielectric>) objects, the surface reflects parts of the light.

[img showing metal vs non-metal and their diffuse + specular reflection]

These LWRP shaders use Physically Based Shading:

- [Lit](#lit-shader.md)

- TerrainLit

- ParticlesLit


**Note:** This shading model is not suitable for low-end mobile hardware. If you’re targeting this hardware, use shaders with a [Simple Shading](#simple-shading) model.

To read more about Physically Based Rendering, see [this walkthrough by Joe Wilson on Marmoset](https://marmoset.co/posts/physically-based-rendering-and-you-can-too/). 

## ## Simple shading

This shading model is suitable for stylized games or for games that run on less powerful platforms. With this shading model, objects do not appear truly photorealistic. The shaders are not energy-conserving. This shading model is based on [Blinn-Phong](https://en.wikipedia.org/wiki/Blinn%E2%80%93Phong_shading_model) model. 

In this model, objects reflect diffuse and specular light and there’s no correlation between these two. The amount of diffuse and specular light reflected depends on properties selected in the material and the total reflected light can therefore exceed the total incoming light. Specular reflection varies only with viewer direction only.

[img showing diffuse and specular reflection]

These LWRP shaders use Simple Shading:

- [Simple Lit](simple-lit-shader.md)
- Particles Simple Lit

## ## Baked Lit shading 

The Baked Lit shading model doesn’t have real-time lighting. Objects can receive [baked lighting](https://docs.unity3d.com/Manual/LightMode-Baked.html) from either [Lightmaps](https://docs.unity3d.com/Manual/Lightmapping.html) or [Light Probes](<https://docs.unity3d.com/Manual/LightProbes.html>). This adds some depth to your Scenes at a small performance cost. Games with this shading model can run on less powerful platforms. 

[img showing unlit objects]

These LWRP shaders use Baked Lit shading:

- Baked Lit
- Particles Baked Lit