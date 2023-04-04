# What's new in HDRP version 15 / Unity 2023.1

This page contains an overview of new features, improvements, and issues resolved in version 15 of the High Definition Render Pipeline (HDRP), embedded in Unity 2023.1.

## Added

### Temporal Anti-Aliasing Sharpening Mode
![](Images/TAA-Sharpening-header.png)

Starting from HDRP 15, two new options are available to perform sharpening with Temporal Anti-aliasing. The first new option is a post-process pass that will offer higher quality sharpening, control on how much sharpening will happen and an option to reduce possible ringing artifacts. The second option is to run Contrast Adaptive Sharpening from AMD FidelityFX.

### Specular Fade

Specular light can now completely be faded when using a specular color workflow using the **HDRP/Lit** and **HDRP/StackLit** shaders by toggling a new option that can be found in the HDRP Global Settings under **Miscellaneous/Specular Fade**.

|        With Specular         |        Faded Specular        |
|:----------------------------:|:----------------------------:|
| ![](Images/WithSpecular.png) | ![](Images/KillSpecular.png) |

### Screen Space Lens Flare
![](Images/ScreenSpaceLensFlare-header.png)

To complete [Lens Flare Data Driven](shared/lens-flare/lens-flare-component.md), HDRP 15 introduces a new Screen Space Lens Flare post-process override.
This effect allows you to use what is on screen to create multiple types of flares based on the [Bloom](Post-Processing-Bloom.md) texture. This is especially useful for view-dependent bright spots (specular reflection, overexposed areas) which doesn't have a defined position in world space.

### Material Type in ShaderGraph

From HDRP 15 and above, the **Material Type** property of the Lit ShaderGraph is now exposed to the material. This means that in the ShaderGraph you can select which **Material Types** will be available in the material and on the material side you can dynamically switch between the exposed material types.

![](Images/ExposedMaterialType.png)

### SSS Improvements

Starting from HDRP 15, dual lobe and diffuse power are available on Diffusion Profiles for materials using Subsurface Scattering.
When simulating skin, it is common to use two specular lobes to account for the thin oily layer covering the epidermis.
The diffuse power is used as a way to simulate diffuse lighting on non lambertian surfaces, such as those that exhibit strong subsurface scattering.

![](Images/profile_dual_lobe.png)

## Updated

### Rendering Layers

HDRP 15.0 introduces updates to the managment of Rendering Layers. Light Layers and Decal Layers will now share the first 16 Rendering Layers, instead of using 8 separate bits each.
Additionally, an option was added in the HDRP Asset to allow access to a fullscreen buffer containing the Rendering Layers Masks of rendered Objects. That buffer can be sampled from the ShaderGraph through the __HD Sample Buffer__ node, and be used to implement custom effects, like outlining objects on a specific rendering layer.

### Adaptive Probe Volumes (APV)

From HDRP 15.0, the APV control interface moved from its own panel to the lighting window under the Probe Volume tab and it has been revamped to provide a better experience. This new UI will also include a Single Scene mode that will provide a simplified interface for when your project is comprised with single scenes that are not loaded together.

![](Images/APVUX.PNG)

Also, with HDRP 15.0 a new debugging mode has been added to better understand how the sampling of APV data happens. This can be found in Rendering Debugger -> Probe Volume -> Debug Probe Sampling

![](Images/APVSamplingDebug.png)

### Water System

HDRP 15.0 improves significantly the feature set of the water system, among the major additions:
- Improved geometry (Instanced quads, custom mesh renderers, non horizontal surfaces, etc.).
- Local currents
- Deformers (Shore waves, Bow Waves, etc).
- Water exclusion.
- A local foam system.
- An improved underwater effect (water line, water fog, caustics, etc.)
- Timeline support.
- Various debug modes (water mask, foam mask, deformation, foam).
- Proper interaction between the water and the cloud system (camera above the clouds).
![](Images/Water2023-1.png)


### Volumetric Clouds

HDRP 15.0 adds several improvements to the already existing volumetric clouds feature:
- A third level of noise to achieve even more convincing visual results.
- Improved anti-ghosting for scenarios where the camera flies through the clouds at high speed.
- Peformance improvements for the "Simple" clouds mode.
![](Images/Volumetric-Clouds-2023-1.png)

### Real-time Ray Tracing

HDRP 15.0 improves the overall real-time ray tracing feature fixing several issues related to the ray traced shadows, mixed tracing lighting. It also significantly improves the fallback lighting mechanic with an integration with the Adaptative Probe Volume feature for Ray Traced Reflection, Ray Traced Global Illumination and Recursive Ray Tracing.
