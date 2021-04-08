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

### Ray Traced Reflection with Lit Shader Clear Coat

A clear coat simulates a thin transparent layer on top of the material. It is particularly useful for materials with a thin translucent layer over a base layer. Real world examples of such materials include car paints, soda cans, lacquered wood, and acrylic.

If you use a [Lit material](Lit-Shader.md) with Ray Traced Reflection, HDRP uses ray tracing

 to render indirect specular reflection for the base layer (if that material's **Smoothness** is above the minimal smoothness specified in the override).

If the material's **Coat Mask** value is greater than zero, HDRP uses ray tracing only for the transparent smooth clear coat specular reflection. The specular reflection of the base layer of the material falls back to the next reflection method in the [reflection hierarchy](Reflection-in-HDRP.md#reflection-hierarchy).

The same principle applies to the [StackLit Shader Graph](master-stack-stacklit.md) when you enable **Coat**.

For an example of a 75% smooth Lit material with different **Coat Mask** values, see the following images:

![](Images/ray-traced-reflection-clear-coat-1.png)
*A Lit material with a Coat Mask value of 0.*

![](Images/ray-traced-reflection-clear-coat-2.png)
*A Lit material with a Coat Mask value of 0.1.*

![](Images/ray-traced-reflection-clear-coat-3.png)
*A Lit material with a Coat Mask value of 1.0.*

## Properties

HDRP implements ray-traced reflection on top of the Screen Space Reflection override. For information on the properties that control this effect, see [Ray-traced properties](Override-Screen-Space-Reflection.md#ray-traced).

