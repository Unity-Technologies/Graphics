# Post-processing in the Universal Render Pipeline

The Universal Render Pipeline (URP) includes an integrated implementation of [post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html) effects. If you use URP, it's not necessary to install an extra package for post-processing effects. URP is not compatible with the [Post&nbsp;Processing&nbsp;Stack&nbsp;v2](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest/index.html) package. 

The images below show a Scene with and without URP post-processing.

Without post-processing:

![](Images/AssetShots/Beauty/SceneWithoutPost.png)

With post-processing:

![](Images/AssetShots/Beauty/SceneWithPost.png)

## How to configure Post-processing in URP

To add post-processing effects to a Camera:

1. Select a Camera, and select the **Post Processing** check box.

    ![Select a Camera, select the Post Processing check box.](Images/post-proc/camera-post-proc-check.png)

2. In the Hierarchy, add a [Volume](Volumes.md) component, and add a Volume profile to the Volume.

3. Add post-processing effects to the Camera by adding [Volume Overrides](VolumeOverrides.md) to the Volume component.

    ![Add post-processing effects to the Camera by adding Volume Overrides to the Volume component.](Images/post-proc/volume-with-post-proc.png)

> **Note:** URP does not support Post-processing on OpenGL&nbsp;ES&nbsp;2.0.

## Post-processing in URP for mobile devices

Post-processing effects can take up a lot of frame time. If you’re using URP for mobile devices, these effects are the most “mobile-friendly” by default:

- Bloom (with __High Quality Filtering__ disabled)
- Chromatic Aberration
- Color Grading
- Lens Distortion
- Vignette

**Note:** For depth-of field, Unity recommends that you use Gaussian Depth of Field for lower-end devices. For console and desktop platforms, use Bokeh Depth of Field.

**Note:** For anti-aliasing on mobile platforms, Unity recommends that you use FXAA. 

## Post-processing in URP for VR
In VR apps and games, certain post-processing effects can cause nausea and disorientation. To reduce motion sickness in fast-paced or high-speed apps, use the Vignette effect for VR, and avoid the effects Lens Distortion, Chromatic Aberration, and Motion Blur for VR.
