# Refraction in the High Definition Render Pipeline

The High Definition Render Pipeline (HDRP) uses a refraction algorithm to simulate light deviation and absorption within Materials. To speed up computation, HDRP uses the following assumptions about the path that light travels :

- Light first travels through air, then through the Material, and then through air again. This means that the algorithm calculates light deviation at both interfaces with the Material: air to Material, and Material to air.
- A simple shape can approximate the surface of the object. This shape is defined in the [Refraction Model](#RefractionModel).

HDRP uses the refraction model to determine the deviated light direction and the distance that light travels within the Material. HDRP then ray cast against a probe proxy volume ([Proxy Raycasting](Reflection-Proxy-Volume.md))  to find the hit point of the refracted light ray.

## Using Refraction

To set up refraction on your Material, you need to do the following:

1. Click on your Material to open it in the Inspector.
2. Click the **Surface Type** drop-down and select **Transparent**. This exposes the **Transparency Inputs** section in the Inspector.
3. Click the **Refraction Model** drop-down to select the [Refraction Model](#RefractionModel) for the Material.
4. Make sure the alpha value for the **Base Map** is less than **1** to make the Material refractive. A value of **0** means that the Material is fully refractive.

For more information on the properties that control refraction, see [Surface Type](Surface-Type.md). 

Note that, intuitively, the less smooth the material is for the refracting object the blurrier the refraction will be.

Settings up a Probe Proxy Volume is also necessary if you want to use screen space refraction effectively. This is because screen space refraction uses the Probe Proxy Volume to approximate the scene and find the correct refracted color. To obtain the best results, the proxy volume should approximate as much of the Scene where refracted rays are intended to land as possible. For more information on proxy volumes, see  the [Reflection Proxy Volume](Reflection-Proxy-Volume.md) page. 

## Refraction calculation

HDRP uses these techniques to calculate light refraction:

- [Screen space refraction](#ScreenSpaceRefraction).
- Realtime and baked [Reflection Probe](#ReflectionProbes) data.

To help you decide which techniques to use in your Unity Project, the following table shows the resource intensity of each technique.

| **Technique**                  | **Description**                                              | **Resource intensity at runtime**                           |
| ------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Screen space refractions**   | Screen space solution that captures all GameObjects in real time. | Low.                                                         |
| **Baked Reflection Probes**    | Manually placed, local Reflection Probe that only captures static GameObjects during the baking process. | Low.                                                         |
| **Realtime Reflection Probes** | Manually placed, local Reflection Probe that captures all GameObjects in real time. | Medium-High (this depends on the resolution of the GameObject capture). |

 

<a name="RefractionHierarchy"></a>

## Refraction hierarchy

To produce the highest quality refractions, HDRP selects which refraction technique gives the best accuracy for each pixel and uses that for calculating refraction, while ensuring it blends with all the other techniques.

To do this, HDRP checks the available techniques in a specific order, called the Refraction hierarchy. The order of the Refraction hierarchy is:

1. [Screen space refraction](Override-Screen-Space-Refraction.md).
2. Sampling [standard](Reflection-Probe.md) and [Planar](Planar-Reflection-Probe.md) Reflection Probes.

This means that, if screen space refraction does not return information for a pixel, HDRP uses Reflection Probes for that pixel. 

<a name="ScreenSpaceRefraction"></a>

### Screen space refraction

The first tier of the refraction hierarchy is a screen space solution. To calculate screen space refraction, the algorithm traces a ray starting from the refractive object. It then refracts the ray according to the properties of the material. To compute the refracted ray, the algorithm assumes that the refractive object can be approximated as a simple shape ([Refraction Model](#RefractionModel)) .

The refracted ray will be then intersected against the closest probe proxy volume to find the pixel in screen space that best approximates the result of the refracted ray. If there is no reflection probe proxy available, HDRP will fallback to a projection at infinite.

<a name="ReflectionProbes"></a>

### Reflection Probes

The second level of the refraction hierarchy uses [Reflection Probes](Reflection-Probes-Intro.md). When screen space refraction does not manage to produce useful refraction data for a pixel, possibly because the area it reflects is off screen, HDRP uses Reflection Probes. 
Reflection Probes capture the Scene from their point of view, and store the result as a Texture. Refractive Materials in range of a Probe can query that Probe’s Texture and then use it to simulate accurate refraction. 

Unlike screen space refraction, you must set up Reflection Probes manually.

For more information on Reflection Probes, see:

- [Reflection Probes introduction](Reflection-Probes-Intro.md)
- [Reflection Probe component documentation](Reflection-Probe.md) 
- [Planar Reflection Probe component documentation](Planar-Reflection-Probe.md)

<a name="RefractionModel"></a>

## Refraction Model

HDRP uses simple shapes to approximate the surface of GameObjects:

- **Sphere**: Approximates the surface as a sphere.
- **Box**: Approximates the surface as a hollow box. In this case think of the thickness as being the distance between two parallel faces of the box.
- **Thin**: Approximates the surface as a hollow box with a fixed thickness of 5cm.

### Examples

- For a solid GameObject, use the **Sphere** Refraction Model with a thickness that is approximately the size of the GameObject that the Material is for. To set the thickness, use the **Refraction Thickness**.

  ![](Images/RefractionInHDRP1.png)

- For a hollow refractive GameObject (for example, a bubble), use the **Thin** refraction Model, or **Box** with a small thickness value. To set the thickness, use the **Refraction Thickness**.

  ![](Images/RefractionInHDRP2.png)
