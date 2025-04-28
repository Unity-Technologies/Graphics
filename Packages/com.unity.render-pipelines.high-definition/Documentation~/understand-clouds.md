# Understand clouds

The High Definition Render Pipeline (HDRP) includes two cloud solutions that you can use in your Unity Project. The two solutions are:

- Volumetric clouds
- Cloud Layer

You can use both solutions together. In this case, you can use Volumetric Clouds for closer, interactable (receives fog and volumetric light) clouds, and Cloud Layer for distant clouds that are part of the skybox.

You can also [create custom cloud effects](create-custom-cloud-effects.md).

__Note:__ Cloud Layer and Volumetric Clouds do not support Planet Center Position.

## Volumetric clouds

![A vast snowy mountainous landscape, with large fluffy white and grey clouds in a clear blue sky.](Images/volumetric-clouds-1.png)

Volumetric clouds are interactable clouds that can render shadows, and receive fog and volumetric light.

To generate and render volumetric clouds, HDRP uses:

* A cloud lookup table - defines properties such as the altitude, density, and lighting.
* A cloud volume - describes the area in the Scene that HDRP generates the clouds in.
* A cloud map - acts like a top down view of the scene. It defines which areas of the cloud volume have clouds and what kind of cloud they are.

Using these three things, HDRP generates volumetric clouds in a two-step process:

1. **Shaping**: HDRP uses large scale noise to create general cloud shapes.
2. **Erosion**: Using the clouds generated in the shaping stage, HDRP applies a smaller scale noise to them to add local details to their edges.

Refer to [Create realistic clouds (volumetric clouds)](create-realistic-clouds-volumetric-clouds.md) for more information about how to enable and use volumetric clouds.

### Limitations

* Volumetric clouds have the same behavior and limitations as transparent objects in the Before Refraction render queue.
* By default, volumetric clouds are disabled on [Planar Reflection Probes](Planar-Reflection-Probe.md) and realtime [Reflection Probes](Reflection-Probe.md) because of the performance cost.
* When enabled for [Reflection Probes](Reflection-Probe.md), the volumetric clouds are rendered at low resolution, without any form of temporal accumulation for performance and stability reasons.
* By default volumetric clouds are enabled on the baked [Reflection Probes](Reflection-Probe.md) if the asset allows it. They are rendered at full resolution without any form of temporal accumulation.
* Volumetric clouds do not appear in ray-traced effects.
* Transmittance is not applied linearly on the camera color to provide a better blending with the sun light (or high intensity pixels). If [Multi-sample anti-aliasing (MSAA)](Anti-Aliasing.md#MSAA) is enabled on the camera, due to internal limitations, a different blending profile is used that may result in darker cloud edges.

## Cloud Layer

A Cloud Layer is a simple representation of clouds in the High Definition Render Pipeline (HDRP). The cloud layer is a 2D texture rendered on top of the sky that can be animated using a flowmap. You can also project cloud shadows on the ground.

Refer to [Create simple clouds (Cloud Layer)](create-simple-clouds-cloud-layer.md) for more information about how to enable and use Cloud Layer.
