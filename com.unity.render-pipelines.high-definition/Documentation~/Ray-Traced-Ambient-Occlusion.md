# Ray-Traced Ambient Occlusion

Ray-Traced Ambient Occlusion is a ray tracing feature in the High Definition Render Pipeline (HDRP). It is an alternative to HDRP's screen space ambient occlusion, with a more accurate ray-traced solution that can use off-screen data.

![](Images/RayTracedAmbientOcclusion1.png)

**Screen space ambient occlusion**

![](Images/RayTracedAmbientOcclusion2.png)

**Ray-traced ambient occlusion**

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Using Ray-Traced Ambient Occlusion

Because this feature is an alternative to the [Ambient Occlusion](Override-Ambient-Occlusion.md) Volume Override, the initial setup is very similar.  To setup ray traced ambient occlusion, first follow the [Enabling Ambient Occlusion](Override-Ambient-Occlusion.md#enabling-ambient-occlusion) and [Using Ambient Occlusion](Override-Ambient-Occlusion.md#using-ambient-occlusion) steps. After you setup the Ambient Occlusion override, to make it use ray tracing:

1. In the Frame Settings for your Cameras, enable **Ray Tracing**.
2. Select the [Ambient Occlusion](Override-Ambient-Occlusion.md) override and, in the Inspector, enable **Ray Tracing**. If you do not see a **Ray Tracing** option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Properties

HDRP implements ray-traced ambient occlusion on top of the Ambient Occlusion override. For information on the properties that control this effect, see [Ray-traced properties](Override-Ambient-Occlusion.md#ray-traced).

