# Reflection Probes

A Reflection Probe acts in a similar way to a **Camera**. Each Reflection Probe captures a view of its surroundings and stores the results. Materials with reflective surfaces can use these results to produce accurate reflections of their surroundings that change as the Camera’s viewing angle changes. The view the Reflection Probe takes and the format of the result depends on the type of Reflection Probe.

The High Definition Render Pipeline (HDRP) allows you to use two different Reflection Probes:

- [Reflection Probes](Reflection-Probe.html) captures a view of its surroundings in all directions and stores the result as a cubemap, similar to the Reflection Probe in built-in render pipeline.
- [Planar Reflection Probes](Planar-Reflection-Probe.html) captures a view in a direction calculated from a reflection of the Camera’s position and rotation, then stores the result in a 2D RenderTexture. By default, the reflected Camera calculates its field of view by setting the center of its projection to the Probe’s **Mirror Position**, and then expands it until it includes the Probe’s **Influence Volume**, as shown here:  

![](Images/ReflectionProbeIntro1.png)

To create a **Reflection Probe** in the Unity Editor, select **GameObject > Light > Reflection Probe** or **Planar Reflection Probe**.

You can customize the behavior of a Reflection Probe in the Inspector. Both types of HDRP Reflection Probe are separate components, but share many of the same properties. For information on each Reflection Probe’s properties, see the [Reflection Probe](Reflection-Probe.html) and [Planar Reflection Probe](Planar-Reflection-Probe.html) documentation.

To make sure HDRP does not apply post-processing effects twice, once in a Reflection Probe's capture and once in a Camera's capture of the reflection, HDRP does not apply post-processing to the Reflection Probe capture.