# Probe Volumes Options Override reference

To add a Probe Volumes Options Override, do the following:

1. Add a [Volume](set-up-a-volume.md) to your Scene and make sure its area overlaps the position of the camera.
2. Select **Add Override**, then select **Lighting** > **Probe Volumes Options**.

Refer to [Fix issues with Probe Volumes](probevolumes-fixissues.md) for more information about using the Probe Volumes Options Override.

| **Property**                           | **Description** |
|------------------------------------|-------------|
| **Normal Bias**   | Enable to move the position used by shaded pixels when sampling Light Probes. The value is in meters. This affects how sampling is moved along the pixel's surface normal. |
| **View Bias**  | Enable to move the sampling position towards the camera when sampling Light Probes. The results of **View Bias** vary depending on the camera position. The value is in meters. |
| **Scale Bias with Min Probe Distance** | Scale the **Normal Bias** or **View Bias** so it's proportional to the spacing between Light Probes in a [brick](probevolumes-concept.md#how-probe-volumes-work). |
| **Sampling Noise** | Enable to increase or decrease the amount of noise URP adds to the position used by shaded pixels when sampling Light Probes. This can help [fix seams](probevolumes-fixissues.md#fix-seams) between bricks. |
| **Animate Sampling Noise** | Enable to animate sampling noise when Temporal Anti-Aliasing (TAA) is enabled. This can make noise patterns less visible. |
| **Leak Reduction Mode** | Enable to choose the method Unity uses to reduce leaks. Refer to [Fix light leaks](probevolumes-fixissues.md#fix-light-leaks).<br/>Options:<br/>&#8226; **Validity and Normal Based**: Enable to make URP prevent invalid Light Probes contributing to the lighting result, and give Light Probes more weight than others based on the GameObject pixel's sampling position.<br/>&#8226; **None**: No leak reduction.
| **Min Valid Dot Product Value** | Enable to make URP reduce a Light Probe's influence on a GameObject if the direction towards the Light Probe is too different to the GameObject's surface normal direction. The value is the minimum [dot product](https://docs.unity3d.com/ScriptReference/Vector3.Dot.html) between the two directions where URP will reduce the Light Probe's influence. |
