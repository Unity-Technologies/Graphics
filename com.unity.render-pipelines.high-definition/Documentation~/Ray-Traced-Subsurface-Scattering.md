# Ray-Traced Subsurface Scattering

Ray-Traced Subsurface Scattering is a ray tracing feature in the High Definition Render Pipeline (HDRP). It is an alternative, more accurate, ray-traced solution to [Subsurface-Scattering](Subsurface-Scattering.md) that can make use of off screen data.

![](Images/RayTracedSubsurfaceScattering.png)

**Perry head rendered with ray traced subsurface scattering**

## Using ray traced subsurface scattering

Ray traced subsurface scattering uses the [Volume](Volumes.md) framework, so to enable this feature and modify its properties, you need to add a Subsurface Scattering override to a [Volume](Volumes.md) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override > Ray Tracing** and click on **Subsurface Scattering**.
3. In the Inspector for the Subsurface Scattering Volume Override, enable Ray Tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md).

## Properties

| Property       | Description                                                  |
| -------------- | ------------------------------------------------------------ |
| **Sample Count**  | Defines the number of samples that are cast per pixel to evaluate the subsurface scattering lighting. |

## Limitations
* Emissive surfaces are incompatible with ray traced sub-surface scattering.
