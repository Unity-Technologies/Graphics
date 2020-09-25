# Post-processing in the Universal Render Pipeline

The Universal Render Pipeline (URP) includes an integrated implementation of [post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html) stack for optimal performace, so you do not need to install any other package. URP is not compatible with the [post-processing version 2](https://docs.unity3d.com/Packages/com.unity.postprocessing@2.2/manual/index.html) package. 

The images below show a Scene with and without URP post-processing.

Without post-processing:

![](Images/AssetShots/Beauty/SceneWithoutPost.png)

With post-processing:

![](Images/AssetShots/Beauty/SceneWithPost.png)

## Enabling Post-processing

In order to use post-processing you have enable it by toggling the `Post Processing` in the camera and add a [Volume](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.0/manual/Volumes.html) component. You can add post-processing effects to your Camera by adding [Volume Overrides](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@10.0/manual/VolumeOverrides.html).

![](Images/post-proc/camera-post-processing.jpg)

**Note:** Post-processing is not supported when using GLES2.

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