# Ray-Traced Global Illumination

Ray-Traced Global Illumination is a ray tracing feature in the High Definition Render Pipeline (HDRP). It's a more accurate alternative to [Screen Space Global Illumination](Override-Screen-Space-GI.md), Light Probes and lightmaps.

![](Images/RayTracedGlobalIllumination1.png)

**Ray-Traced Global Illumination off**

![](Images/RayTracedGlobalIllumination2.png)

**Ray-Traced Global Illumination on**

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

To troubleshoot this effect, HDRP provides a Global Illumination [Debug Mode](Ray-Tracing-Debug.md) and a Ray Tracing Acceleration Structure [Debug Mode](Ray-Tracing-Debug.md) in Lighting Full Screen Debug Mode.

## Using Ray-Traced Global Illumination

This feature replaces the [Screen Space Global Illumination](Override-Screen-Space-GI.md) Volume override, so the initial setup is similar. To setup ray traced global illumination on your Volume:

1. Follow the [Enabling Screen Space Global Illumination](Override-Screen-Space-GI.md#enabling-screen-space-global-illumination) and [Using Screen Space Global Illumination](Override-Screen-Space-GI.md#using-screen-space-global-illumination) steps to set up the Screen Space Global Illumination override on your Volume.
2. Go to **Edit** > **Project Settings** > **Frame Settings (Default Values)** > **Camera** **Rendering** and enable **Ray Tracing**.
3. Select your Volume in the Hierarchy.
4. In the Inspector, go to the [Screen Space Global Illumination](Override-Screen-Space-GI.md) override and enable **Ray Tracing**. If you don't see a **Ray Tracing** option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Properties

HDRP implements ray-traced global illumination on top of the Screen Space Global Illumination override. For information on the properties that control this effect, see [Ray-traced properties](Override-Screen-Space-GI.md#ray-traced).
