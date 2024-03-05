# Post-processing in the Universal Render Pipeline

The Universal Render Pipeline (URP) includes an integrated implementation of [post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html) effects. If you use URP, it's not necessary to install an extra package for post-processing effects. URP is not compatible with the [Post&nbsp;Processing&nbsp;Stack&nbsp;v2](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest/index.html) package.

URP uses the [volume](Volumes.md) framework for post-processing effects.

The images below show a scene with and without URP post-processing.

Without post-processing:<br/>
![](Images/AssetShots/Beauty/SceneWithoutPost.png)

With post-processing:<br/>
![](Images/AssetShots/Beauty/SceneWithPost.png)

> **Note**: URP does not support Post-processing on OpenGL&nbsp;ES&nbsp;2.0.

## <a name="post-proc-how-to"></a>Add post-processing to a new scene

To add post-processing to a new scene:

1. Select a Camera, then in the Inspector window enable **Post Processing**.
2. Add a GameObject with a [Volume](Volumes.md) component in the scene. For example, select **GameObject** > **Volume** > **Global Volume**.
3. Select the GameObject, then in the **Volume** component select **New** to create a new [Volume Profile](Volume-Profile.md).
4. Select **Add Override**, then select a post-processing effect [Volume Override](VolumeOverrides.md), for example **Bloom**.

Now you can use the Volume Override to enable and adjust the settings for the post-processing effect.

> [!NOTE]
> The GameObject which contains the volume and the camera you wish to apply post-processing to must be on the same Layer.

Refer to [Understand Volumes](Volumes.md) for more information.

## Post-processing in URP for mobile devices

Post-processing effects can take up a lot of frame time. If you’re using URP for mobile devices, these effects are the most “mobile-friendly” by default:

* Bloom (with **High Quality Filtering** disabled)
* Chromatic Aberration
* Color Grading
* Lens Distortion
* Vignette

> **Note**: For depth-of field, Unity recommends that you use Gaussian Depth of Field for lower-end devices. For console and desktop platforms, use Bokeh Depth of Field.

**Note**: For anti-aliasing on mobile platforms, Unity recommends that you use FXAA.

## Post-processing in URP for VR

In VR apps and games, certain post-processing effects can cause nausea and disorientation. To reduce motion sickness in fast-paced or high-speed apps, use the Vignette effect for VR, and avoid the effects Lens Distortion, Chromatic Aberration, and Motion Blur for VR.
