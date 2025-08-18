# Environment lighting

Simulate light from the surroundings of your scene, for example the sky or a lighting studio, in the High Definition Render Pipeline (HDRP).

HDRP does the following to create environment lighting:

1. Renders a background, for example an HDRI sky texture or a gradient. For more information, refer to [Sky](sky.md).

2. Uses the sky to calculate how the GameObjects in your scene receive [indirect ambient light](https://docs.unity3d.com/Manual/lighting-ambient-light.html). To control the light, use the [**Environment (HDRP)** tab of the Lighting window](reference-lighting-environment.md).

## How HDRP calculates ambient light

Depending on your settings and baked lighting, HDRP fetches the sky color for a GameObject from one of the following:

- The default [ambient light probe](https://docs.unity3d.com/6000.2/Documentation/ScriptReference/RenderSettings-ambientProbe.html), which captures either the static sky in the **Environment (HDRP)** tab of the Lighting window, or the dynamic sky at runtime from the **Visual Environment** volume override.
- Baked lightmap textures from [lightmapping](https://docs.unity3d.com/Manual/Lightmapping-baking-before-runtime.html).
- Realtime lightmap textures from [Enlighten Realtime Global Illumination](https://docs.unity3d.com/Manual/realtime-gi-using-enlighten-landing.html).
- [Adaptive Probe Volumes](probevolumes.md).
- [Screen space global illumination](Override-Screen-Space-GI.md) or [ray-traced global illumination](ray-traced-global-illumination.md).

**Note:** HDRP calculates the ambient Light Probe on the GPU, then uses asynchronous readback on the CPU, so the lighting is one frame late.

## Additional resources

- [Configure environment lighting](ambient-lighting-configure.md)
- [Ambient light](https://docs.unity3d.com/Manual/lighting-ambient-light.html)
