# Refraction in the High Definition Render Pipeline

The High Definition Render Pipeline (HDRP) uses a refraction algorithm to simulate light deviation and absorption within Materials. To speed up computation, HDRP uses the following assumptions about the path that light travels :

- Light first travels through air, then through the Material, and then through air again. This means that the algorithm calculates light deviation at both interfaces with the Material: air to Material, and Material to air.
- A simple shape can approximate the surface of the object. This shape is the [Refraction Model](#RefractionModel).

The refraction model allows the refraction algorithm to calculate light deviation and the distance that light travels within the Material. HDRP then uses [Proxy Raycasting](Reflection-Proxy-Volume.html) to compute a raycast and find the hit point of the deviated light ray.

## Refraction calculation

HDRP uses these techniques to calculate light refraction:

- [Screen space refraction](#ScreenSpaceRefraction).
- Realtime and baked [Reflection Probe](#ReflectionProbes) sampling.

To help you decide which techniques to use in your Unity Project, the following table shows the resource intensity of each technique.

| **Technique**                  | **Description**                                              | **Resource intensity at run time**                           |
| ------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Screen space refractions**   | Screen space solution that captures all GameObjects in real time. | Low.                                                         |
| **Baked Reflection Probes**    | Manually placed, local Reflection Probe that only captures static GameObjects during the baking process. | Low.                                                         |
| **Realtime Reflection Probes** | Manually placed, local Reflection Probe that captures all GameObjects in real time. | Medium-High (this depends on the resolution of the GameObject capture). |

 

<a name=”RefractionHierarchy”></a>

## Refraction hierarchy

To produce the highest quality refractions, HDRP selects which refraction technique gives the best accuracy for each pixel and uses that for calculating refraction, while ensuring it blends with all the other techniques.

To select the best refraction technique for a given pixel, HDRP checks the available techniques in a specific order, called the Refraction hierarchy. The order of the Refraction hierarchy is:

1. [Screen space refraction](Override-Screen-Space-Refraction.html).
2. Sampling [standard](Reflection-Probe.html) and [Planar](Planar-Reflection-Probe.html) Reflection Probes.

If screen space refraction does not return information for a pixel, HDRP uses Reflection Probes for that pixel. 

<a name="ScreenSpaceRefraction"></a>

### Screen space refraction

The first tier of the refraction hierarchy is a screen space solution. To calculate screen space refraction (SSR), the algorithm traces a ray in screen space until it finds an intersection with a refractive Material. It then travels through the GameObject to calculate the light deviation. The algorithm assumes that a simple shape (the Refraction Model) can approximate the surface of the GameObject. The algorithm then finds the point that the deviated light ray hits the GameObject, and uses the color at that point for the point that the ray entered the refractive Material.

<a name="ReflectionProbes"></a>

### Reflection Probes

The second level of the reflection hierarchy uses [Reflection Probes](Reflection-Probes-Intro.html). When screen space refraction does not manage to produce useful refraction data for a pixel, possibly because the area it reflects is off screen, HDRP uses Reflection Probes. Reflection Probes capture the Scene from their point of view, and store the result as a Texture. Refractive Materials in range of a Probe can query that Probe’s Texture and then use it to simulate accurate refraction. 

Unlike screen space refraction, you must set up Reflection Probes manually.

For more information on Reflection Probes, see:

- [Reflection Probes introduction](Reflection-Probes-Intro.html)
- [Reflection Probe component documentation](Reflection-Probe.html) 
- [Planar Reflection Probe component documentation](Planar-Reflection-Probe.html)

<a name="RefractionModel"></a>

## Refraction Model

HDRP uses simple shapes to approximate the surface of GameObjects:

- **Sphere**: Approximates the surface as a sphere.
- **Box**: Approximates the surface as a box.

### Examples

- For a filled GameObject, use a **Sphere** Refraction Model with a thickness that is approximately the size of the GameObject that the Material is for. To set the thickness, use the **Refraction Thickness** and **Refraction Thickness Multiplier** properties.
  ![](Images/RefractionInHDRP1.png)
- For a hollow refractive GameObject (for example, a bubble), use a **Box** refraction Mode with a small value for thickness. To set the thickness, use the **Refraction Thickness** and **Refraction Thickness Multiplier** properties.

![](Images/RefractionInHDRP2.png)