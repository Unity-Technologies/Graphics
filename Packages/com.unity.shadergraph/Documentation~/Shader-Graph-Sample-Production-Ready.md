# Production Ready Shaders

The Shader Graph Production Ready Shaders sample is a collection of Shader Graph shader assets that are ready to be used out of the box or modified to suit your needs.  You can take them apart and learn from them, or just drop them directly into your project and use them as they are. The sample also includes a step-by-step tutorial for how to combine several of the shaders to create a forest stream environment.

In URP, in order to see the content correctly, please make the following changes to your project's settings:
* Open your Project Settings (Edit > Project Settings) and select the ShaderGraph tab. Set Shader Variant Limit to 513. (This will allow the URP Lit shader to work correctly.)
* Select your project's SRP Settings asset and enable Depth Texture and Opaque Texture. (This will allow the water shaders to render correctly.)
* Select your project's Renderer Data asset. Hit the Add Renderer Feature button at the bottom and add the Decal feature. (This will allow the decal shaders to render correctly.)

The sample content is broken into the following categories:

| Topic | Description   |
|:------|:--------------|
| **[Lit shaders](Shader-Graph-Sample-Production-Ready-Lit.md)** | Introduces Shader Graph versions of the HDRP and URP Lit shaders. Users often want to modify the Lit shaders but struggle because they’re written in code. Now you can use these instead of starting from scratch. |
| **[Decal shaders](Shader-Graph-Sample-Production-Ready-Decal.md)** | Introduces shaders that allow you to enhance and add variety to your environment. Examples include running water, wetness, water caustics, and material projection. |
| **[Detail shaders](Shader-Graph-Sample-Production-Ready-Detail.md)** | Introduces shaders that demonstrate how to create efficient [terrain details](https://docs.unity3d.com/Manual/terrain-Grass.html) that render fast and use less texture memory. Examples include clover, ferns, grass, nettle, and pebbles. |
| **[Rock](Shader-Graph-Sample-Production-Ready-Rock.md)** | A robust, modular rock shader that includes base textures, macro and micro detail, moss projection, and weather effects. |
| **[Water](Shader-Graph-Sample-Production-Ready-Water.md)** | Water shaders for ponds, flowing streams, lakes, and waterfalls. These include depth fog, surface ripples, flow mapping, refraction and surface foam. |
| **[Post-Process](Shader-Graph-Sample-Production-Ready-Post.md)** | Shaders to add post-processing effects to the scene, including edge detection, half tone, rain on the lens, an underwater look, and VHS video tape image degradation. |
| **[Weather](Shader-Graph-Sample-Production-Ready-Weather.md)** | Weather effects including rain drops, rain drips, procedural puddles, puddle ripples, and snow. |
| **[Miscellaneous](Shader-Graph-Sample-Production-Ready-Misc.md)** | A couple of additional shaders - volumetric ice, and level blockout shader. |
| **[Forest Stream Construction Tutorial](Shader-Graph-Sample-Production-Ready-Tutorial.md)** | A tutorial that describes how to combine multiple assets from this sample to create a forest stream. |
