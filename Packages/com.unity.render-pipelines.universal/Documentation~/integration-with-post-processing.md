# Post-processing in the Universal Render Pipeline

The Universal Render Pipeline (URP) includes an integrated implementation of [post-processing](https://docs.unity3d.com/Manual/PostProcessingOverview.html) effects. If you use URP, it's not necessary to install an extra package for post-processing effects. URP is not compatible with the [Post&nbsp;Processing&nbsp;Stack&nbsp;v2](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest/index.html) package.

URP uses the [Volume](Volumes.md) framework for post-processing effects.

The images below show a Scene with and without URP post-processing.

Without post-processing:<br/>
![](Images/AssetShots/Beauty/SceneWithoutPost.png)

With post-processing:<br/>
![](Images/AssetShots/Beauty/SceneWithPost.png)

> **Note:** URP does not support Post-processing on OpenGL&nbsp;ES&nbsp;2.0.

## <a name="post-proc-how-to"></a>How to configure post-processing effects in URP

This section describes how to configure Post-processing in URP.

### Using post-processing in the URP Template Scene

Post-processing is preconfigured in the SampleScene Scene in URP Template.

To see the preconfigured effects, select **Post-process Volume** in the Scene.

![Add post-processing effects to the Camera by adding Volume Overrides to the Volume component.](Images/post-proc/volume-with-post-proc.png)

To add extra effects, [add Volume Overrides to the Volume](VolumeOverrides.md#volume-add-override).

To configure location-based post-processing effects, refer to [How to use Local Volumes](Volumes.md#volume-local).

### Configuring post-processing in a new URP Scene

To configure post-processing in a new Scene:

1. Select a Camera, and select the **Post Processing** check box.

    ![Select a Camera, select the Post Processing check box.](Images/post-proc/camera-post-proc-check.png)

2. Add a GameObject with a [Volume](Volumes.md) component in the Scene. This instruction adds a Global Volume. Select **GameObject > Volume > Global Volume**.

3. Select the **Global Volume** GameObject. In the Volume component, create a new Profile by clicking **New** button on the right side of the Profile property.

    ![Create new Profile.](Images/post-proc/volume-new-scene-new-profile.png)

4. Add post-processing effects to the Camera by adding [Volume Overrides](VolumeOverrides.md#volume-add-override) to the Volume component.

Now you can use the Volume Override to enable and adjust the settings for the post-processing effect.

> [!NOTE]
> Post-processing effects from a volume apply to a camera only if a value in the **Volume Mask** property of the camera contains the layer that the volume belongs to.

Refer to [Understand Volumes](Volumes.md) for more information.

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
