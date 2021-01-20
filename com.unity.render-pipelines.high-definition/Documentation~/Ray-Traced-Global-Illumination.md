# Ray-Traced Global Illumination

Ray-Traced Global Illumination is a ray tracing feature in the High Definition Render Pipeline (HDRP). It is a more accurate alternative to [Screen Space Global Illumination](Override-Screen-Space-GI.md), Light Probes and lightmaps.

![](Images/RayTracedGlobalIllumination1.png)

**Ray-Traced Global Illumination off**

![](Images/RayTracedGlobalIllumination2.png)

**Ray-Traced Global Illumination on**

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Using Ray-Traced Global Illumination

1. Because this feature replaces the [Screen Space Global Illumination](Override-Screen-Space-GI.md) Volume override, the initial setup is very similar. To setup ray traced global illumination, first follow the [Enabling Screen Space Global Illumination](Override-Screen-Space-GI.md#enabling-screen-space-global-illumination) and [Using Screen Space Global Illumination](Override-Screen-Space-GI.md#using-screen-space-global-illumination) steps. After you setup the Screen Space Global Illumination override, to make it use ray tracing:
   1. In the Frame Settings for your Cameras, enable **Ray Tracing**.
   2. Select the [Screen Space Global Illumination](Override-Screen-Space-GI.md) override and, in the Inspector, enable **Ray Tracing**. If you do not see a **Ray Tracing** option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Properties

HDRP implements ray-traced global illumination on top of the Screen Space Global Illumination override. For information on the properties that control this effect, see [Ray-traced properties](Override-Screen-Space-GI.md#ray-traced).
