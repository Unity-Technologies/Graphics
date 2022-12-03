# What's new in HDRP version 12 / Unity 2021.2

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Added

The following list of features are new to version 12 of the High Definition Render Pipeline (HDRP), as part of  Unity 2021.2.

### Tessellation support for ShaderGraph Master Stack

From HDRP 12.0, you can enable [tessellation](Tessellation.md) on any HDRP [Master Stack](master-stack-hdrp.md). The option is in the Master Stack settings and adds two new inputs to the Vertex Block:

* Tessellation Factor
* World Displacement

For more information about tessellation, see the [Tessellation documentation](Tessellation.md).

### Custom Velocity support for ShaderGraph Master Stack

From HDRP 12.0, you can enable custom Velocity on any HDRP [Master Stack](master-stack-hdrp.md). This option is in the Master Stack settings and adds one new input to the Vertex Block:

* Velocity

You can use this Vertex Block to create an additional velocity for procedural geometry (for example, generated hair strands) and to get a correct motion vector value.

### Cloud Layer System

![](Images/HDRPFeatures-CloudLayer.png)

HDRP 12.0 introduces a cloud system that you can control through the volume framework.

HDRP includes a Cloud Layer volume override which renders a cloud texture on top of the sky. For more information, see the [Cloud Layer](Override-Cloud-Layer.md) documentation.

For detailed steps on how to create custom clouds in your scene, see [creating custom clouds](Creating-Custom-Clouds.md).

### Volumetric Clouds volume override

HDRP now includes a Volumetric Clouds volume override which allows you to precisely control the cloud coverage in your scene. These clouds receive realistic lighting from the sun and sky. For more information, see the [Volumetric Clouds](Override-Volumetric-Clouds.md) documentation.

![](Images/volumetric-clouds-2.png)

![](Images/volumetric-clouds-1.png)

### Lens Flares

![](Images/LensFlareSamples2.png)

HDRP 12.0 includes a new Lens Flare system. You can attach a Lens Flare (SRP) component to any GameObject.

Some Lens Flare properties only appear when you attach a Lens Flare (SRP) component to a light. Each Lens Flare has optional multiple elements that you can control individually. HDRP also provides a [new lens flare asset](lens-flare-data-driven-asset.md) and a [new lens flare component](lens-flare-data-driven-component.md) that you can attach to any GameObject.

HDRP includes a new Lens Flare sample that uses presets and Textures. You can see an example of the Textures included in this sample in the image below:

![](Images/LensFlareTextures.png)

### Light Anchor

From HDRP 12.0, HDRP includes a new [Light Anchor](light-anchor.md) component. You can attach this component to any light to control the light in Main Camera view.

![](Images/LightAnchor0.png)

### Light List

HDRP version 2021.2 includes a new setting in `ShaderConfig.cs` called `FPTLMaxLightCount`. You can use this setting to set the maximum number of lights per tile on the GPU. To increase this value, you must generate a new Shader config project. For information on how to create a new Shader config project, see [HDRP-Config-Package]().

### New upsampling methods

HDRP includes the following upsample methods you can use to male lower resolution images appear sharper:

- [**Temporal Anti-Aliasing (TAA) Upscale**](#)
- [**NVIDIA’s Deep Learning Super Sampling (DLSS) v2.2**](#)
- [**AMD’s FidelityFX Super Resolution**](#)

For more information about upscaling methods in HDRP, see [Dynamic Resolution](Dynamic-Resolution.md).

#### Temporal Anti-Aliasing Upsampling

HDRP 12.0 introduces the **Temporal Anti-Aliasing (TAA) Upscale** filter in the Dynamic resolution system.

This method uses temporal integration to produce a sharp upscaled image without sacrificing visual quality at lower resolutions. This filter is compatible with any device supported by HDRP and uses the Temporal Anti-aliasing (TAA) step in the pipeline.

The image below displays the **Temporal Anti-Aliasing (TAA) Upscale** (A) next to the **Catmull-Rom** (B) upscaling method.

For more information, see [Dynamic Resolution](Dynamic-Resolution.md)

![A:TAA Upscale. B: Catmull-Rom.](Images/DynamicRes_SidebySide_AB.png)

#### NVIDIA’s Deep Learning Super Sampling

HDRP 12.0 includes production-ready integration and support for **NVIDIA’s Deep Learning Super Sampling (DLSS)** v2.2.

**NVIDIA Deep Learning Super Sampling (DLSS)** uses artificial intelligence to increase graphics performance and quality. You can use DLSS to:

- run real-time ray traced worlds at high frame rates and resolutions.
- Improve the performance and quality of rasterized graphics. This is particularly useful for virtual reality (VR) applications.
- run applications at higher frame rates. This helps to remove disorientation, nausea, and other negative effects that occur at lower frame rates.

For more information, see the [HDRP Guide on DLSS](deep-learning-super-sampling-in-hdrp.md).

#### AMD’s FidelityFX Super Resolution

HDRP 12.0 includes production-ready integration and support of **AMD’s FidelityFX Super Resolution**. This upscale filter uses a spatial super-resolution method that balances quality and performance. For more information, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx).

This upscale filter has no dependencies and runs at the end of the post-processing pipeline.

The filter also runs when at 100% screen resolution, because it can have beneficial sharpening effects.

To enable **AMD’s FidelityFX Super Resolution**.:

- Open the Project settings window (menu: **Edit** > **Project settings**).
- Expand the **Quality** drop down and select **HDRP.**
- Expand the **Dynamic resolution** section.
- Open the **Default Fallback Upscale Filter** drop down .
- Select **FidelityFX Super Resolution**.

![](Images/FidelityFX-Menu.png)



### Mip Bias Support

This HDRP version introduces a new setting called **Use Mip Bias.** This setting improves the detail of any upscaling filters you use in your scene.

To enable **Use Mip Bias**:

- Open the Project settings window (menu: **Edit** > **Project settings**).
- Expand the **Quality** drop down and select **HDRP.**
- Expand the **Dynamic resolution** section.
- Enable the **Use Mip Bias** checkbox.

![](Images/UseMipBias.png)

### ClearFlag

HDRP 2021.2 includes the new `ClearFlag.Stencil` function. Use this to clear all flags from a stencil.

From HDRP 2021.2, `ClearFlag.Depth` does not clear stencils.

### Probe Volumes Global Illumination (Experimental)

![](Images/ProbeVolumesGI.png)

HDRP 12.0 introduces an experimental version of the Probe Volume system that creates pre-computed probe-based global illumination. You can use this system to place light probes automatically and create per-pixel lighting.

The Probe Volume system supports volumetric fog. You can also use it to improve Screen Space Global Illumination results.
This is an experimental release which is intended to gather feedback. This means it is not recommended for use in a production context.

In its current state, use the Probe Volume system as a replacement for the per-object Light Probe system, not as a replacement for lightmaps. This feature works best in a scene that uses large, static GameObjects.

## Updated

### HDRP Global Settings

In HDRP 12.x the **Runtime Debug Display** toggle is now in the HDRP Global Settings Asset instead of the HDRP Asset. This toggle uses the currently active HDRP Asset as the source.

### Area Lights

The AxF shader and Fabric master stack now correctly support Area lights. Hair master stack supports Area Lights but with incorrect lighting models (GGX currently).

### Density Volume (Local Volumetric Fog)

HDRP 12.0 introduces multiple improvements to Density Volumes (Local Volumetric Fog):

- Density Volumes is now named **Local Volumetric Fog**. This new name removes confusion with [Volumes](Volumes.md) and makes it clear that this feature creates a fog effect.
- Local Volumetric Fog masks now support 3D RenderTextures as masks.
- 3D mask Textures now use all four RGBA channels. This allows volumetric fog to have different colors and density based on the 3D Texture.
- You can now change the mask Texture size limit (32x32x32). To do this, use the new setting in the [HDRP Asset](HDRP-Asset.md) **Lighting > Volumetrics** section, called **Max Local Volumetric Fog Resolution**. The upper limit for mask Textures is now 256x256x256.
  An information box below the **Max Local Volumetric Fog Resolution** field tells you how much memory Unity allocates to store these Textures. When you increase the resolution of the mask Texture, it doesn't always improve the quality of the volumetric fog. Instead, use this property to find a balance between the **Volumetrics** quality and the **Local Volumetric Fog** resolution.
- There is a new field to change the falloff HDRP applied when it blends a volume using the **Blend Distance** property. You can choose the **Linear** setting, which is the default and previous technique, or the **Exponential** setting, which is more realistic.
- The minimal value of the **Fog Distance** parameter is 0.05 instead of 1. You can use this value to create fog effects that appear more dense.

### Dynamic Resolution Scale

This version of HDRP introduces multiple improvements to Dynamic Resolution Scaling:

- The exposure and pixel to pixel quality now match between the software and hardware modes.
- New API in DynamicResolutionHandler to handle multi camera rendering for hardware mode. This allows you to change Cameras and reset scaling for each Camera.
- New API in DynamicResolutionHandler that you can use to set an upscale filter at runtime.
- Removed the Bilinear and Lanczos upscale filters.

This version of HDRP also introduces multiple fixes to Dynamic Resolution Scaling. For details, see [Fixed: Dynamic Resolution Scaling](#Fixed-Dynamic-Resolution-Scaling).

### AOV API

From HDRP 12.0, The AOV API includes the following improvements:

- You can now override the render buffer format that HDRP uses internally when you render **AOVs**. To do this, call `aovRequest.SetOverrideRenderFormat(true);`.
- This version of HDRP provides a world space position output buffer (see `DebugFullScreen.WorldSpacePosition`).
- The `MaterialSharedProperty.Specular` output now includes information for Materials that use the metallic workflow. To do this, the metallic parameter is now fresnel 0.

### RendererList API

From 2021.2, HDRP includes an updated `RendererList` API in the `UnityEngine.Rendering.RendererUtils` namespace. This API performs fewer operations than the previous version of the `RendererList` API when it submits the `RendererList` for drawing. Use this new version to query if the list of visible objects is empty.

You can use the previous version of the API in the `UnityEngine.Experimental.Rendering` namespace for compatibility purposes but it is now deprecated.

When you enable the **Dynamic Render Pass Culling** option in the HDRP Global Settings, HDRP uses the `RendererList` API to skip certain drawing passes based on the type of GameObjects that are currently visible. For example, if HDRP doesn’t draw any  GameObjects with distortion, HDRP skips the Render Graph passes that draw the distortion effect and their dependencies, like color pyramid generation.

### Additional Properties

From HDRP 12.0, **More Options** is now named **Additional Properties**. You can now access these properties in a new way: The cogwheel that was present in component headers is now an entry in the contextual menu. When you enable **Show Additional Properties**, Unity highlights the background of each additional property for a few seconds to show you where they are.

You can also choose whether to hide or display

**Additional Properties globally** in the  **Core Render Pipeline** settings (menu: **Edit > Preferences > Core Render Pipeline**). To do this, use the **Additional Properties > Visibility** ndrop down.

### Path tracing improvements

![](Images/HDRPFeatures-FabricPT.png)

HDRP’s path tracing now supports more materials:

- Fabric: Cotton/wool and silk variants.
- AxF: SVBRDF and car paint variants.
- Stacklit.

The Unlit material has also been updated with support for shadow mattes.

In addition, the sky visible from camera rays now matches exactly the rasterized version, rather than relying on lower quality environment textures.

### Top level menus

HDRP 12.0 includes changes to some top level menus. This is to make the top level menus more consistent between HDRP and the Universal Render Pipeline. The top level menus this change affects are:

* **Window**
  * **HD Render Pipeline Wizard** is now at **Window > Rendering > HDRP Wizard**
  * **Graphics Compositor** is now at **Window > Rendering > Graphics Compositor**
* **Assets**
  * **HDRP Shader Graphs** are now in **Assets > Create > Shader Graph > HDRP**
  * **Custom FullScreen Pass** is now at **Assets > Create > Shader > HDRP Custom FullScreen Pass**
  * **Custom Renderers Pass** is now at **Assets > Create > Shader > HDRP Custom Renderers Pass**
  * **Post Process Pass** is now at **Assets > Create > Shader > HDRP Post Process**
  * **High Definition Render Pipeline Asset** is now at **Assets > Create > Rendering > HDRP Asset**
  * **Diffusion Profile** is now at **Assets > Create > Rendering > HDRP Diffusion Profile**
  * **C# Custom Pass** is now at **Assets > Create > Rendering > HDRP C# Custom Pass**
  * **C# Post Process Volume** is now at **Assets > Create > Rendering > HDRP C# Post Process Volume**
* **GameObject**
  * **Density Volume** is now at **GameObject > Rendering > Local Volumetric Fog**
  * **Sky and Fog Volume** is now at **GameObject > Volume > Sky and Fog Global Volume**

### Decals

When you create a custom decal shader in HDRP 12.x, the accumulated normal value stored in the depth buffer represents the surface gradient instead of the tangent space normal. You can find an example of this implementation in `DecalUtilities.hlsl`.

### Decal normal blending

![](Images/HDRPFeatures-SurfGrad.png)

From HDRP 12.0 you can use a new option in the [HDRP Asset](HDRP-Asset.md) (**Rendering > Decals > Additive Normal Blending**) to additively blend decal normals with the GameObject's normal map.

In the image above, the example image on the left does not use this method. The image on the right uses the new additive normal blending method.

### Increased the default GPU light count

This version of HDRP increases the default number of lights a single pixel can get influence from to 63. You can use a new setting in `ShaderConfig.cs` called `FPTLMaxLightCount` to set the maximum number of lights per tile on the GPU.

To increase this value, you must generate a new Shader config project. For information on how to create a new Shader config project, see [HDRP-Config-Package](HDRP-Config-Package.md).

### Physical Camera

HDRP 12.0 includes the following physical Camera improvements:
- You can now animate many physical Camera properties with keyframes using the [Unity Timeline](https://docs.unity3d.com/Packages/com.unity.timeline@1.6/manual/index.html)..
- Added the **Focus Distance** property to the physical Camera properties. To improve compatibility with older HDRP versions,  HDRP only includes this property in Depth of Field (DoF) computations if the **Focus Distance Mode** in the [Depth of Field](Post-Processing-Depth-of-Field.md) volume component is set to **Camera**.

### Depth Of Field

Improved the quality of the physically-based Depth Of Field in the following ways:

- Improved depth sorting to make the Depth Of Field result closer to the path-traced result.

- Improved how HDRP handles near defocus blur and how it blends with in-focus areas.

- Improved support for MSAA to remove artifacts around the edges of visible GameObjects.

![](Images/HDRPFeatures-BetterDoF.png)

### Physically Based Hair Shader
![](Images/PBHairShader.png)

HDRP 12.0 includes a new physically-based Marschner hair shader. You can use this as an alternative to the Kajiya-Kay hair shader. This new shading model allows you to quickly create a hair material that fits any lighting scenario. For more information about this new hair shader model and its parameters, see [Hair Master Stack](master-stack-hair.md).

### New shader for Custom Render Textures

This HDRP version includes a new shader that's formatted for [Custom Render Textures](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html) in **Assets > Create > Shader > Custom Render Texture**. To use this shader, create a new Material and assign it to the Custom Render Texture's **Material** field.

### Tessellation

HDRP 12.x improves motion vector support for tessellation in the following ways:

* Updates the struct `VaryingsPassToDS`, so that it only manages the `previousPositionRWS` variable.
* Adds the `MotionVectorTessellation()` function. For more information, see the `MotionVectorVertexShaderCommon.hlsl` file.
* Evaluates the `tessellationFactor` in the vertex shader and passes it to the hull shader as an interpolator. For more information, see the `VaryingMesh.hlsl` and `VertMesh.hlsl` files.
* Moves the `GetTessellationFactors()` function from `LitDataMeshModification.hlsl` to TessellationShare.hlsl. It calls a new function, `GetTessellationFactor()`, that's in the `LitDataMeshModification.hlsl` file.

### Dynamic Render Pass Culling

HDRP 12.0 can dynamically cull rendering passes that don’t contribute to the output frame based on the viewpoint of the active Camera. For example, if there are no GameObjects that use low resolution transparency in the current viewpoint, HDRP can skip the composition pass for GameObjects that use low resolution transparency.

To enable this feature, go to **Edit > Project Settings > HDRP Global HDRP Settings** and enable **Dynamic Render Pass Culling**.

### Built-in Object ID Custom Pass

HDRP 12.0 includes a built-in custom pass that you can use to generate Object IDs and display them as colors. To use this new custom pass, create a Custom Pass volume and select the new **ObjectIDCustomPass**from the list of available custom passes.

### Specular Occlusion

HDRP 12.0 includes a more precise method to calculate specular occlusion based on Ambient Occlusion (AO) and Bent normals. This replaces the old method for all materials and shader graphs.

![](Images/SpecularOcclusion.png)

### Ambient Occlusion and Specular Occlusion

In HDRP 12.x, the  algorithm that calculates how ambient occlusion and specular occlusion contributes to direct lighting doesn’t use the multi-bounce contribution (GTAOMultiBounce). This gives a more accurate direct lighting result.

### Screen Space Global Illumination

HDRP 12.0 improves the quality of the [Screen Space Global Illumination (SSGI)](Override-Screen-Space-GI.md) in the following ways:

- Added a new fallback mechanic when the ray marching fails to return an intersection point based on reflection probes and the sky probe.
- Improved the denoiser HDRP uses to filter the SSGI signal.
- Improved the performance of SSRI evaluation at half-resolution, and the performance of the denoiser.
- Emissive properties work correctly by default and don't require SSGI flags.

SSGI off:

![](Images/SSGIoff.png)

SSGI on:

![](Images/SSGIon.png)

### Ray Traced Global Illumination

HDRP 12.0 includes the following new volume parameters for [Ray Traced Global Illumination (RTGI)](Ray-Traced-Global-Illumination.md):

- **Ray Miss**
- **Last Bounce**

These new parameters control the fallback method HDRP uses when RTGI interacts with the reflection probe, and the sky.

This HDRP version also improves the quality of the RTGI denoiser.Emissive properties work correctly by default and do not require [Screen Space Global Illumination (SSGI)](Override-Screen-Space-GI.md) flags.

This version includes a new RTGI **Tracing** option called **Mixed.** Select this option to make RTGI use a combination of ray tracing and ray marching to calculate global illumination. **Mixed** tracing only works with RTGI when you set the RTGI  **Mode** to  **Performance**. For more information, see [tracing modes](tracing-modes.md).

### Ray Traced Reflections

HDRP 12.0 includes the following new volume parameters for [Ray Traced Reflections](Ray-Traced-Reflections.md) :

- **Ray Miss**
- **Last Bounce**

These new parameters control the fallback method HDRP uses when Ray Traced Reflections interact with the reflection probe, and the sky.

This version includes a new Ray Traced Reflections **Mode** option called **Mixed Ray Casting**. This new Mode is only a Select this mode to make Ray Traced Reflections evaluate ray intersections using ray marching and the GBuffer when possible.

This version includes a new Ray Traced Reflections **Tracing** option called **Mixed**. Select this option to make Ray Traced Reflections use a combination of ray tracing and ray marching to calculate global illumination. **Mixed** tracing for Ray Traced Reflections is only compatible with [Lit shaders](Lit-Shader.md) that use [deferred rendering](Forward-And-Deferred-Rendering.md). For more information, see [tracing modes](tracing-modes.md).

### Recursive rendering

HDRP 12.0 includes the following new volume parameters for [Recursive Rendering](Ray-Tracing-Recursive-Rendering.md):

- **Ray Miss**
- **Last Bounce**

These new parameters control the fallback method HDRP uses when Recursive Rendering interacts with the reflection probe, and the sky.

### Conservative Depth Offset

HDRP version 12.0 adds a new **Depth Offset** property called **Conservative** to all [Master stacks in HDRP](master-stack-hdrp.md)**.** This option makes all depth offsets positive to  take advantage of the early depth test mechanic.

The **Conservative** option only appears when you enable a Material’s **Surface options** > **Depth Offset** property.

### Custom Pass Improvements

This version includes the following improvements to [custom passes](Custom-Pass.md) in HDRP:

- Added a new API to override the camera properties: `CustomPassUtils.OverrideCameraRendering`.
- You can now use `CustomPassUtils Render` functions to write directly into a `RenderTexture` instead of `RTHandle`.
- Added a search field to filter custom passes to the custom pass volume component.
- You can now use the C# API in the `CustomPassContext` to access the motion vector buffer.
- Added a new **Mode** property to the custom pass volume component called **Camera** that you can use to render a custom pass on one specific Camera.
- Added new nodes to ShaderGraph called **Custom Color Buffer** and **Custom Depth Buffer**. You can use these nodes to sample the custom color and depth buffers of custom passes.
- Added a new injection point called **AfterPostProcessBlurs** that HDRP executes after the **Depth of Field** and **Motion Blur** injection points.

### Custom Post Process

From HDRP 12.0 the custom post processes use the [CommandBuffer.Blit](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.Blit.html) function.

You can also use the new `_CameraMotionVectorsTexture` property to access the motion vectors buffer in a shader

To learn more about Custom Post Processes in HDRP, see the [Custom Post Process](Custom-Post-Process.md).

### Decal Performance Improvements

This version of HDRP introduces performance improvements for Decal Projectors:

- Decal Projector’s processing and draw call creation now work in parallel and use burst.
- Decal Projectors no longer require the `LateUpdate` method, which caused a performance bottleneck.
- On an intel i9-8800KS CPU, the HDRP template took  0.378 ms to process all decals. The new changes improve this number to 0.025ms, which is a 15x improvement in performance. This number goes up the more decals are processed.
- These improvements mean that HDRP is now dependant on the Burst 1.5 package for HDRP.

<a name="Fixed-Dynamic-Resolution-Scaling"></a>


### CPU Light Loop Performance Improvements

This version of HDRP introduces performance improvements for the CPU culling light loop. The new CPU light loop optimizations include:
- Introduction of the new object HDLightEntityCollection - a master singleton object that keeps render side state of light objects.
- Replacement of the flat ProcessVisibleLightsLoop with a parallel job using the HDVisibleLightEntities helper.
- Burstification and parallelization of ProcessVisibleLights.
- Introduction new sorting functions introduced in the core package.

For a more detailed information please check the [Lightloop-Burstification](Lightloop-Burstification.md) documentation entry.

## Removed

### Receive SSGI flags

From HDRP 12.x, Materials don’t include the **Receive SSGI** flags property. This is because all Emissive materials are now compatible with Screen Space Global Illumination.

## Fixed

### Dynamic Resolution Scaling

This version of HDRP introduces multiple fixes to **Dynamic Resolution Scaling**:

- The rendering artifact that caused black edges to appear on screen when in hardware mode no longer occurs.
- The rendering artifacts that appeared when using the Lanczos filter in software mode no longer occur.
- Fixed an issue that caused corrupted scaling on Dx12 hardware mode when a planar reflection probe or a secondary Camera is present.

### Light list

HDRP 12.x splits the ` g_vLightListGlobal` variable  into `g_vLightListTile` and `g_vLightListCluster`. This fix corrects unexpected behavior on console platforms.

For a full list of changes and updates in version 12 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@12.0/changelog/CHANGELOG.html).
