# Reflection in the High Definition Render Pipeline

The High Definition Render Pipeline (HDRP) uses the following techniques to calculate reflections:

- [Screen space reflections](#ScreenSpaceReflection).
- Realtime and baked [Reflection Probe](#ReflectionProbes) sampling.
- Sky reflection

To help you decide which techniques to use in your Unity Project, the following table shows the resource intensity of each technique.

| **Technique**                  | **Description**                                              | **Resource Intensity at runtime**                           |
| ------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Screen space reflection**    | Screen space solution. Captures all GameObjects in real time. | High.                                                        |
| **Realtime Reflection Probes** | Manually placed, local Reflection Probe. Captures all GameObjects in real time. | Medium-High (this depends on the resolution of the capture). |
| **Baked Reflection Probes**    | Manually placed, local Reflection Probe. Only captures static GameObjects during the baking process. | Low.                                                         |
| **Sky reflection**             | Reflective Materials show a reflection of the sky.           | Low.                                                         |

<a name="ReflectionHierarchy"></a>

## Reflection hierarchy

To produce the highest quality reflections, HDRP uses the reflection technique that gives the best accuracy for each pixel, while ensuring it blends with all the other techniques. To do this, HDRP evaluates all lighting techniques until it reaches an overall **weight** of 1.

- Screen space reflection controls its own weight.
- Reflection Probes have a **Weight** property which you can edit manually. This allows you set weights for overlapping Reflection Probes to blend them properly.
- Sky reflection has a fixed weight of 1.

To select the best reflection technique for a given pixel, HDRP checks the available techniques in a specific order, called the [reflection hierarchy](reflection-refraction-hierarchy.md).

## Reflection Proxy Volumes and reprojection

Reflection Materials in HDRP use [Reflection Proxy Volumes](Reflection-Proxy-Volume.md) as a reprojection volume to apply a reflection from a [Reflection Probe](Reflection-Probes-Intro.md). HDRP uses reprojection to correct parallax issues that arise due to the capture point not being at the same position as the surface point using the captured information.

If you do not use a proxy volume, when a reflective Material uses environment information captured by a Reflection Probe, it only provides a perfect reflection for the pixels in the same position as the Reflection Probes capture point. This means that Reflection Probes do not provide a perfect reflection for most of the reflective Materials on the screen and, instead, renders the reflected environment in a slightly different position. If you want an accurate reflection for a pixel that is not in the same position as the Reflection Probe’s capture point, use a proxy volume. HDRP uses reprojection to fix this displacement issue. HDRP projects capture information on to the Reflection Probe’s proxy volume and then reprojects it onto the surface of reflective Materials. This does not give a perfect projection, but it is much closer to the actual reflection than what an infinite projection gives. However, proxy volumes can potentially introduce some artifacts depending on the shape of the volume.

- A **Box** volume can potentially introduce resolution loss and incorrect angles at the edges of the box.
- A **Sphere** volume provides uniform quality, but you can sometimes see incorrect reflections on long surfaces that the proxy shape does not follow. For example, for a long plane, the reflections become less accurate the further they are from the Reflection Probe’s capture point.
