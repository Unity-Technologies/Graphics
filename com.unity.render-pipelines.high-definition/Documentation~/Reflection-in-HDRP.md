# Reflection in the High Definition Render Pipeline

The High Definition Render Pipeline (HDRP) uses the following techniques to calculate reflections:

- [Screen space reflections](#ScreenSpaceReflection).
- Realtime and baked [Reflection Probe](#ReflectionProbes) sampling.
- Sky reflection

To help you decide which techniques to use in your Unity Project, the following table shows the resource intensity of each technique.

| **Technique**                  | **Description**                                              | **Resource Intensity at run time**                           |
| ------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Screen space reflection**    | Screen space solution. Captures all GameObjects in real time. | High.                                                        |
| **Realtime Reflection Probes** | Manually placed, local Reflection Probe. Captures all GameObjects in real time. | Medium-High (this depends on the resolution of the capture). |
| **Baked Reflection Probes**    | Manually placed, local Reflection Probe. Only captures static GameObjects during the baking process. | Low.                                                         |
| **Sky reflection**             | Reflective Materials show a reflection of the sky.           | Low.                                                         |

<a name=”ReflectionHierarchy”></a>

## Reflection hierarchy

To produce the highest quality reflections, HDRP uses the reflection technique that gives the best accuracy for each pixel, while ensuring it blends with all the other techniques. To do this, HDRP evaluates all lighting techniques until it reaches an overall **weight** of 1.

- Screen space reflection controls its own weight.
- Reflection Probes have a **Weight** property which you can edit manually. This allows you set weights for overlapping Reflection Probes to blend them properly.
- Sky reflection has a fixed weight of 1.

To select the best reflection technique for a given pixel, HDRP checks the available techniques in a specific order, called the Reflection Hierarchy. The order of the Reflection Hierarchy is:

1. [Screen space reflection](#ScreenSpaceReflection).
2. Realtime and baked [Reflection Probe](#ReflectionProbes) sampling.
3. Sky reflection.

If screen space reflection has a weight of 1, then HDRP uses that information and does not evaluate any other technique. If screen space reflection does not have a weight of 1, HDRP falls back to the next technique in the hierarchy. HDRP continues this pattern until it either reaches a weight of 1 or it reaches the lowest level of the hierarchy, which uses sky reflection. This means that screen space reflection falls back to a Reflection Probe if there are any, or falls back to sky reflection if not. A Reflection Probe can fallback to other Reflection Probes with a lower priority. Currently, HDRP calculates the priority of a Reflection Probe based on the size of its **Influence Volume**. The smaller the **Influence Volume**, the higher the priority.

<a name="ScreenSpaceReflection"></a>

### Screen space reflection

The first level of the reflection hierarchy is a screen space solution with a high resource intensity that captures everything rendered in the Scene. HDRP uses the [Volume](Volumes.html) framework to handle screen space reflection. The [Screen Space Reflection](Screen-Space-Reflection.html) Volume [override](Volume-Components.html) contains the properties and controls the screen space reflection effect. To calculate screen space reflection, the algorithm traces a ray in screen space until it finds an intersection with the depth buffer. It then looks up the color of the pixel from the previous frame, and uses that to compute the reflected lighting.

This screen-space technique limits the reflective effect because it can only reflect GameObjects that actually visible on screen. Also, because this technique only uses a single layer of the depth buffer, tracing rays behind GameObjects is difficult for it to handle. If this technique does not find an intersection, HDRP falls back to the next technique in the [reflection hierarchy](#ReflectionHierarchy).

**Note**: Screen space reflection only works for opaque Materials and, because it is a screen space effect, it only reflects GameObjects that are visible on the screen.

For information on how to use screen space reflection in your Unity Project, see the [Screen Space Reflection](Override-Screen-Space-Reflection.html) documentation.

<a name="ReflectionProbes"></a>

### Reflection Probes

The second level of the reflection hierarchy uses [Reflection Probes](Reflection-Probes-Intro.html). When screen space reflection does not manage to produce useful reflection data for a pixel, possibly because the area it reflects is off screen, HDRP uses Reflection Probes. These Reflection Probes capture the Scene from their point of view and store the result as a Texture. Reflective Materials in range of a Probe can query that Probe’s Texture and then use it to produce accurate reflections. Be aware that metallic Materials that use **baked** Reflection Probes do not show specular lighting. Instead, HDRP uses the ambient color to approximates the specular color.

Unlike screen space reflection, you must set up Reflection Probes manually.

For more information on Reflection Probes, see:

- [Reflection Probes introduction](Reflection-Probes-Intro.html)
- [Reflection Probe component documentation](Reflection-Probe.html)
- [Planar Reflection Probe component documentation](Planar-Reflection-Probe.html) documentation

### Sky reflection

For the final level of the reflection hierarchy, HDRP falls back to sky reflections if screen space reflection and Reflection Probes provide no useful reflective information for a pixel. If a pixel uses this technique to calculate reflections, it queries the sky to produce a reflection of the sky at that point.

## Reflection Proxy Volumes and reprojection

Reflection Materials in HDRP use [Reflection Proxy Volumes](Reflection-Proxy-Volume.html) as a reprojection volume to apply a reflection from a [Reflection Probe](Reflection-Probes-Intro.html). HDRP uses reprojection to correct parallax issues that arise due to the capture point not being at the same position as the surface point using the captured information.

If you do not use a proxy volume, when a reflective Material uses environment information captured by a Reflection Probe, it only provides a perfect reflection for the pixels in the same position as the Reflection Probes capture point. This means that Reflection Probes do not provide a perfect reflection for most of the reflective Materials on the screen and, instead, renders the reflected environment in a slightly different position. If you want an accurate reflection for a pixel that is not in the same position as the Reflection Probe’s capture point, use a proxy volume. HDRP uses reprojection to fix this displacement issue. HDRP projects capture information on to the Reflection Probe’s proxy volume and then reprojects it onto the surface of reflective Materials. This does not give a perfect projection, but it is much closer to the actual reflection than what an infinite projection gives. However, proxy volumes can potentially introduce some artifacts depending on the shape of the volume.

- A **Box** volume can potentially introduce resolution loss and incorrect angles at the edges of the box.
- A **Sphere** volume provides uniform quality, but you can sometimes see incorrect reflections on long surfaces that the proxy shape does not follow. For example, for a long plane, the reflections become less accurate the further they are from the Reflection Probe’s capture point.