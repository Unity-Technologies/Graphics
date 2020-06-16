# Indirect Lighting Controller

The **Indirect Lighting Controller** is a Volume component that allows you to globally control the intensity of baked or precomputed indirect lighting.

This override is useful in situations where you need to animate your lighting globally.

## Using the Indirect Lighting Controller

The **Indirect Lighting Controller** uses the [Volume](Volumes.html) framework, so to enable and modify **Indirect Lighting Controller** properties, you must add an **Indirect Lighting Controller** override to a [Volume](Volumes.html) in your Scene. To add **Indirect Lighting Controller** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click on **Indirect Lighting Controller**. You can now use the **Indirect Lighting Controller** to control baked or precomputed indirect lighting.

## Properties

![](Images/Override-IndirectLightingController1.png)

| Property                        | Description                                                  |
| ------------------------------- | ------------------------------------------------------------ |
| **Indirect Diffuse Lighting Multiplier**  | A multiplier for lightmaps, Light Probes, Light Probe Volumes, Screen-Space Global Illumination, and [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.md). HDRP multiplies the light data from all of these by this value. |
| **Indirect Diffuse Lighting Layers** | Specifies the [Light Layers](Light-Layers.md) for indirect diffuse lighting. If you enable Light Layers, you can use them to decouple Meshes in your Scene from the above multiplier. |
| **Reflection Lighting Multiplier**  | A multiplier for baked, realtime, custom [Reflection Probes](Reflection-Probe.md) and [Planar Probes](Planar-Reflection-Probe.md), [Screen-Space Reflection](Override-Screen-Space-Reflection.md), [Ray-Traced Reflection](Ray-Traced-Reflections.md), and Sky Reflection. HDRP multiplies the light data from all of these by this value. |
| **Reflection Lighting Layers**  | LSpecifies the [Light Layers](Light-Layers.md) for reflection lighting. If you enable Light Layers, you can use them to decouple Meshes in your Scene from the above multiplier. |
| **Reflection Probe Intensity Multiplier**  | A multiplier for baked, realtime, and custom [Reflection Probes](Reflection-Probe.md) and [Planar Probes](Planar-Reflection-Probe.md). HDRP multiplies the Reflection Probe data by this value. |

## Details

An example of a situation where an **Indirect Lighting Controller** would be useful is when your Camera is in a dark area and you want to light the area suddenly. To create this effect:

1. Create a **Scene Settings** GameObject (menu: **GameObject > Rendering > Scene Settings**) and add an **Indirect Lighting Controller** (click on the Scene Settings GameObject and then, in the Volume component in the Inspector, click **Add Override** and select **Indirect Lighting Controller**). 
2. Add a Collider to the Scene Settings GameObject and enable the **Is Trigger** checkbox. Set the **Size** of the Collider to be the size of the area you want to change the indirect lighting for.
3. In the **Indirect Lighting Controller**, set the **Indirect Diffuse Intensity** and **Indirect Specular Intensity** to 0. This dims all indirect lighting to black.
4. When you light the area, animate (using Timeline or an Animation) the Volume's **Weight** property to transition from 1 to 0. This will progressively interpolate between the values set inside the Volume and the values from other Volumes that affect your Camera.
5. As a result, the Indirect lighting globally fades in. Alter the length of the animation to speed up or slow down the fade.