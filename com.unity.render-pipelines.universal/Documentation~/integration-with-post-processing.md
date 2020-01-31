# Post-processing in the Universal Render Pipeline

The Universal Render Pipeline (URP)  contains its own integrated post-processing solution. This version of URP also supports the Post Processing Version 2 (PPV2) package, for backwards compatibility with existing Projects.

Both post-processing solutions will be supported in the versions of URP that are compatible with Unity 2019.4 LTS. From Unity 2020.1, only the integrated solution will be supported.

## PPV2
Full documentation for PPV2 can be found in [the PPV2 documentation microsite](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest).

## URP's integrated post-processing solution
This implementation uses the same [Volume](http://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Volumes.html) system as the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?preview=1). You can add post-processing effects to your Camera in the same way you add any other [Volume Override](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@6.9/manual/Volume-Components.html).

The images below show a Scene with and without URP post-processing.

Without post-processing:

![](Images/AssetShots/Beauty/SceneWithoutPost.png)

With post-processing:

![](Images/AssetShots/Beauty/SceneWithPost.png)


### Post-processing in URP for mobile devices

Post-processing effects can take up a lot of frame time. If you’re using URP for mobile devices, these effects are the most “mobile-friendly” by default:

- Bloom (with __High Quality Filtering__ disabled)
- Chromatic Aberration
- Color Grading
- Lens Distortion
- Vignette

**Note:** For depth-of field, Unity recommends that you use Gaussian Depth of Field for lower-end devices. For console and desktop platforms, use Bokeh Depth of Field.

**Note:** For anti-aliasing on mobile platforms, Unity recommends that you use FXAA. 

### Post-processing in URP for VR
In VR apps and games, certain post-processing effects can cause nausea and disorientation. To reduce motion sickness in fast-paced or high-speed apps, use the Vignette effect for VR, and avoid the effects Lens Distortion, Chromatic Aberration, and Motion Blur for VR.
