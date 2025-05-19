# Use Reflection probes

[Reflection Probes](Reflection-Probe.md) and [Planar Reflection Probes](Planar-Reflection-Probe.md) provide indirect specular lighting.

A Reflection Probe captures its surroundings whereas a Planar Reflection Probe only captures one direction.

For more information on probes and how they work, see [Reflection Probes](Reflection-Probes-Intro.md).

## Create a reflection probe

To create a **Reflection Probe** in the Unity Editor, select **GameObject > Light > Reflection Probe** or **Planar Reflection Probe**.

You can customize the behavior of a Reflection Probe in the Inspector. Both types of HDRP Reflection Probe are separate components, but share many of the same properties. For information on each Reflection Probe’s properties, see the [Reflection Probe](Reflection-Probe.md) and [Planar Reflection Probe](Planar-Reflection-Probe.md) documentation.

To make sure HDRP does not apply post-processing effects twice, once in a Reflection Probe's capture and once in a Camera's capture of the reflection, HDRP does not apply post-processing to the Reflection Probe capture.

## Control the influence of a probe

The influence of a probe determines which pixels it affects and by how much.

Use the following tools to control the influence of a reflection probe on a pixel:

* [Influence volume](#use-an-influence-volume): The probe affects any pixel inside this volume.
* [Blend distance](#blend-influence): The probe affects pixels near the border of the Influence volume less.
* [Blend normal distance](#blend-normal-influence): The probe doesn't affect pixels near the border with an invalid normal.

### Use an influence volume

Use this volume to include or exclude pixels from the probe's influence.

**Note**: When a pixel is inside an influence volume, the probe still processes it even if the specular value the probe provides isn't significant. This is important to handle the performance of probes.

![A Venn diagram. A gray circle representing the reflection probe sits at the center of a yellow square symbolizing its influence volume. A mesh object is depicted as a gray rectangle that partially overlaps with the square. Pixels affected by the reflection probe are those located within the intersection of the square and rectangle.](Images/ReflectionProbe_Influence.svg)

### Blend influence

Unity linearly weights the specular lighting value the probe provides between the influence volume and the blend volume.
Use blending to create smooth transitions at the border of the probe's influence, or when probes overlap.

![A Venn diagram. A reflection probe, shown as a gray circle, sits at the center of a green square representing the blend volume. The blend influence decreases from 1 at the outer border of the green square to zero at the outer border of a surrounding yellow square, with the space between the two borders defining the blend distance. A gray rectangle, symbolizing the mesh object, partially overlaps both the green and yellow squares.](Images/ReflectionProbe_InfluenceBlend.svg)
![Two light probes, A and B, from left to right, are represented by two gray circles. Each sits at the center of a green square which represents their respective blend volumes. The right border of the left green square touches the left green rectangle of the right square. Each green square sits at the center of a larger yellow square, which represents the influence volumes of the light probes. They partially overlap horizontally. A mesh object is depicted as a gray rectangle that partially overlaps all squares. The intersection of the gray rectangle and the intersection of the two yellow circles, excluding the green rectangles, is the area of overlapping influence.](Images/ReflectionProbe_InfluenceBlendOverlap.svg)

### Blend normal influence

Sometimes, a probe can influence a pixel that isn't consistent with the scene layout.

For example, when a light ray can't reach a pixel due to occlusion, but the pixel is inside the influence volume.

You can set a blend normal distance similarly to a blend distance. The probe doesn't influence pixels that are inside the influence volume, but outside of the blend normal distance, if their normal points away from the probe.

![A Venn diagram. A gray circle representing the reflection probe sits at the center of a grey house shape. A yellow square representing the influence volume encloses the house shape. A blue square representing the blend normal volume is delineated by the two walls, and the roof and floor of the house shape, which are not visible. The space between the border of the blue square and the border of the enclosing yellow square defines the blend distance. Pixels on the inner side of the roof, whose normals point towards the probe, are influenced by the probe; pixels on the outer side of the roof, whose normals point away from the probe, are discarded.
](Images/ReflectionProbe_InfluenceBlendNormal.svg)
