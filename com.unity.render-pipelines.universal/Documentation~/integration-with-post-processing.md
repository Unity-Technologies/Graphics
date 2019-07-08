#Post Processing in the Universal Render Pipeline

For post-processing, the Universal Render Pipeline (UniversalRP) uses the Unity Post Processing Stack version 2 (PPv2). This package is included by default in any project that has UniversalRP installed.

For detailed information about steps to configure the post-processing, the effects that are included, how to use them, and how to debug issues, see the [PPv2 documentation](<https://docs.unity3d.com/Packages/com.unity.postprocessing@2.1/manual/index.html>).



##Effects that UniversalRP does not support

Most of the effects that come with PPv2 work with UniversalRP by default. However, when you use post-processing in UniversalRP, keep in mind that UniversalRP doesn’t support the following:

- Motion Vector-based effects, including Motion Blur and Temporal Anti-aliasing. 

- Screen Space Reflections (SSR), because they require a G-Buffer and expensive rendering calculations. To be able to scale across hardware, UniversalRP doesn’t use SSR.
- Compute-based effects by default, including Auto-exposure, Ambient Occlusion (MSVO) and Debug Monitors. You _can_ use these effects in UniversalRP, but only if your target hardware supports [Compute Shaders](<https://docs.unity3d.com/Manual/class-ComputeShader.html>). If your target hardware does not support compute shaders, Unity disables the post-processing effect at run time.

## Post-processing in UniversalRP for mobile devices

Post-processing effects can take up a lot of frame time. If you’re using UniversalRP for mobile devices, these effects are the most “mobile-friendly” in the PPv2 stack:

- Anti-aliasing (FXAA - Fast mode)
- Bloom (Fast mode)
- Chromatic Aberration (Fast mode)
- Color Grading (with LDR)
- Lens Distortion
- Vignette

## Post-processing in UniversalRP for VR

In VR apps, certain post-processing effects can cause nausea and disorientation. To reduce motion sickness on fast-paced or high-speed games, Unity recommends that you use the Vignette effect for VR. 