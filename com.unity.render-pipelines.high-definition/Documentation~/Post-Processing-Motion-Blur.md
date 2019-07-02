# Motion Blur

The Motion Blur effect simulates the blur that occurs in an image when a real-world camera films objects moving faster than the camera’s exposure time. This is usually due to rapidly moving objects, or a long exposure time.

## Using Motion Blur

The Motion Blur effect uses velocities from HDRP's velocity buffer. This means that for Motion Blur to have an effect, you must enable Motion Vectors in your Unity Project. For information on how to enable Motion Vectors, see the [Motion Vectors documentation](Motion-Vectors.html).

**Motion Blur** uses the [Volume](Volumes.html) framework, so to enable and modify **Motion Blur** properties, you must add a **Motion Blur** override to a [Volume](Volumes.html) in your Scene. To add **Motion Blur** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Post-processing** and click on **Motion Blur**. HDRP now applies **Motion Blur** to any Camera this Volume affects.

Motion Blur includes some [advanced properties](Advanced-Properties.html) that you must manually expose.

## Properties

![](Images/Post-processingMotionBlur1.png)

| **Property**                       | **Description**                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **Intensity**                      | Set the strength of the Motion Blur effect. This scales the magnitude of the velocities present in the velocity buffer. Set this value to 0 to disable Motion Blur. |
| **Sample Count**                   | Set the maximum number of sample points HDRP uses to compute the Motion Blur effect. Higher values increase the quality and produce a smoother blur. Higher values also increase the resource intensity of the effect. |
| **Maximum Velocity**               | Use the slider to set the maximum velocity, in pixels, that HDRP allows for all sources of motion blur except Camera rotation. This clamps any value above this threshold to the threshold value. Higher values result in a more intense blur, and an increase in resource intensity. |
| **Minimum Velocity**               | Use the slider to set the minimum velocity, in pixels, that triggers motion blur. Higher values mean that HDRP does not calculate Motion Blur for slow-moving GameObjects. This decreases the resource intensity. |
| **Camera Rotation Velocity Clamp** | Use the slider to set the maximum velocity that HDRP allows Camera rotation to contribute to the velocities of GameObjects. This value is expressed in terms of screen fraction. Higher values result in Camera rotation giving wider blurs. |

## Details

There are multiple options available to decrease the performance impact of Motion Blur. Listed in order of effectiveness, you can: 

1. Reduce the **Sample Count**. A lower sample count directly translates to higher performance. However, it is important to keep in mind that the algorithm clamps the maximum amount of samples so that no two samples are less than a pixel apart, so a high sample count does not affect slowly moving GameObjects very much. 
2. Increase the **Minimum Velocity**. Increase this threshold to make HDRP blur less of the screen. If many GameObjects are at a velocity below this threshold, HDRP does not calculate Motion Blur for them, and the resource intensity of the effect decreases.
3. Decrease the **Maximum Velocity** and the **Camera Rotation Velocity Clamp**. This gives a less intense blur, which leads to an access pattern that is more friendly to the GPU. 