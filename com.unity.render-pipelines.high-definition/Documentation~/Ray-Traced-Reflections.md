# Ray-Traced Reflections

Ray-Traced Reflections is a ray tracing feature in the High Definition Render Pipeline (HDRP). It is an alternative, more accurate, ray-traced solution to [Screen Space Reflection](Override-Screen-Space-Reflection.md) that can make use of off screen data.

![](Images/RayTracedReflections1.png)

**Screen-space reflections**

![](Images/RayTracedReflections2.png)

**Ray-traced reflections**

For information about ray tracing in HDRP, and how to set up your HDRP Project to support ray tracing, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Using Ray-Traced Reflections

Because this feature replaces the [Screen Space Reflection](Override-Screen-Space-Reflection.md) Volume override, the initial setup is very similar. To setup ray traced reflections, first follow the [Enabling Screen Space Reflection](Override-Screen-Space-Reflection.md#enabling-screen-space-reflection) and [Using Screen Space Reflection](Override-Screen-Space-Reflection.md#using-screen-space-reflection) steps. After you setup the Screen Space Reflection override, to make it use ray tracing:

1. In the Frame Settings for your Cameras, enable **Ray Tracing**.
2. Select the [Screen Space Reflection](Override-Screen-Space-Reflection.md) override and, in the Inspector, enable **Ray Tracing**. If you do not see a **Ray Tracing** option, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Properties

HDRP implements ray-traced reflection on top of the Screen Space Reflection override. For information on the properties that control this effect, see [Ray-traced properties](Override-Screen-Space-Reflection.md#ray-traced).

