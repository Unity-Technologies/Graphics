# Motion Blur

The Motion Blur effect simulates the blur that occurs in an image when a real-world camera films objects moving faster than the camera’s exposure time. This is usually due to rapidly moving objects, or a long exposure time.

## Using Motion Blur

The Motion Blur effect uses velocities from HDRP's velocity buffer. This means that for Motion Blur to have an effect, you must enable Motion Vectors in your Unity Project. For information on how to enable Motion Vectors, see the [Motion Vectors documentation](Motion-Vectors.md).

**Motion Blur** uses the [Volume](Volumes.md) framework, so to enable and modify **Motion Blur** properties, you must add a **Motion Blur** override to a [Volume](Volumes.md) in your Scene. To add **Motion Blur** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Motion Blur**. HDRP now applies **Motion Blur** to any Camera this Volume affects.

Motion Blur includes [more options](More-Options.md) that you must manually expose.

[!include[](snippets/volume-override-api.md)]

## Properties

![](Images/Post-processingMotionBlur1.png)

| **Property**              | **Description**                                              |
| ------------------------- | ------------------------------------------------------------ |
| **Intensity**             | Set the strength of the Motion Blur effect. This scales the magnitude of the velocities present in the velocity buffer. Set this value to 0 to disable Motion Blur. |
| **Quality**               | Specifies the quality level to use for this effect. Each quality level applies different preset values. Unity also stops you from editing the properties that the preset overrides. If you want to set your own values for every property, select **Custom**. |
| **Sample Count**          | Set the maximum number of sample points HDRP uses to compute the Motion Blur effect. Higher values increase the quality and produce a smoother blur. Higher values also increase the resource intensity of the effect. |
| **Maximum Velocity**      | Use the slider to set the maximum velocity, in pixels, that HDRP allows for all sources of motion blur except Camera rotation. This clamps any value above this threshold to the threshold value. Higher values result in a more intense blur, and an increase in resource intensity. |
| **Minimum Velocity**      | Use the slider to set the minimum velocity, in pixels, that triggers motion blur. Higher values mean that HDRP does not calculate Motion Blur for slow-moving GameObjects. This decreases the resource intensity. |
| **Camera Motion Blur**    | Indicates whether camera movement contributes to motion blur. Disable this property to stop camera movement from contributing to motion blur. |
| **Camera Clamp Mode**     | Specifies the method HDRP uses to clamp the motion vectors that derive from camera movement/rotation. Note that although this specifically clamps the motion vectors that come from the camera, this may change motion vector velocities relative to the camera too. For example, a GameObject that has the camera as a parent (and thus moves and rotates with the camera) might not have a zero motion vector when the camera moves. The options are:<br />&#8226; **None**: The motion vector component derived from the Camera is not treated differently.<br />&#8226;**Rotation: **The motion vector component derived from the Camera rotation is clamped separately.<br />&#8226;**Translation**: The motion vector component derived from the Camera translation is clamped separately.<br />&#8226;**Separate Translation And Rotation**: The motion vector components derived from the Camera rotation and translation are clamped separately with separate clamp options.<br />&#8226;**Full Camera Motion Vector**: The full motion vector component derived from the Camera full movement (rotation and translation) is clamped separate from the Object motion, but with a single threshold. |
| **- Rotation Clamp**      | Use the slider to set the maximum velocity that HDRP allows Camera rotation to contribute to the velocities of GameObjects. This value is expressed in terms of screen fraction. Higher values result in Camera rotation giving wider blurs. <br/>This property is only relevant if you set **Camera Clamp Mode** to **Rotation** or **Separate Translation And Rotation**. |
| **- Translation Clamp**   | Use the slider to set the maximum velocity that HDRP allows Camera translation to contribute to the velocities of GameObjects. This value is expressed in terms of screen fraction. Higher values result in Camera rotation giving wider blurs. <br/>This property is only relevant if you set **Camera Clamp Mode** to **Translation** or **Separate Translation And Rotation**. |
| **- Motion Vector Clamp** | Use the slider to set the maximum velocity that HDRP allows Camera transform changes to contribute to the velocities of GameObjects. This value is expressed in terms of screen fraction. Higher values result in Camera rotation giving wider blurs.  <br/>This property is only relevant if you set **Camera Clamp Mode** to **Full Camera Motion Vector**. |

## Details

There are multiple options available to decrease the performance impact of Motion Blur. Listed in order of effectiveness, you can:

1. Reduce the **Sample Count**. A lower sample count directly translates to higher performance. However, it is important to keep in mind that the algorithm clamps the maximum amount of samples so that no two samples are less than a pixel apart, so a high sample count does not affect slowly moving GameObjects very much.
2. Increase the **Minimum Velocity**. Increase this threshold to make HDRP blur less of the screen. If many GameObjects are at a velocity below this threshold, HDRP does not calculate Motion Blur for them, and the resource intensity of the effect decreases.
3. Decrease the **Maximum Velocity** and the **Camera Clamp** parameters. This gives a less intense blur, which leads to an access pattern that is more friendly to the GPU.

If you select a **Camera Clamp Mode** other than **None**, motion vectors that are usually relative to camera motion no longer are. For example, if an object has the camera as a parent and thus perfectly follows the camera, normally that object has a motion vector with a length close to zero. However, if the camera uses a different clamping method to object, the final velocity length is likely not zero if the clamping point is reached.
