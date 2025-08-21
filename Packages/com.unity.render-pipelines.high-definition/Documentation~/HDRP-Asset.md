# The High Definition Render Pipeline Asset

The High Definition Render Pipeline (HDRP) Asset controls the global rendering settings of your Project and creates an instance of the render pipeline.

Unity only allocates memory and builds shader variants for features you enable in the HDRP Asset. This means that you can disable features your project doesn't use to save memory. Because certain features require shader variants or other resources when Unity builds your project, you can only enable and disable features at edit time. However, it's possible to toggle the rendering of particular features at runtime, just not using the HDRP Asset. Instead, [Frame-Settings](Frame-Settings.md) control the features that cameras in the scene render. Frame Settings can only toggle features that are enabled in the HDRP Asset; they can't enable features that are disabled.

## Rendering

| **Property** | **Subproperty** | **Description** |
|-|-|-|
| **Color Buffer Format**                  | N/A | The format of the color buffer that HDRP uses for rendering. Using R16G16B16A16 instead of R11G11B10 doubles the memory usage, but helps avoid banding. R16G16B16A16 is also required for [Alpha-Output](Alpha-Output.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| **Lit Shader Mode**                      | N/A | Use the drop-down to choose which mode HDRP uses for the [Lit Shader](lit-material.md).<br />&#8226; **Forward Only**: forces HDRP to only use forward rendering for Lit Shaders.<br />&#8226; **Deferred Only**: forces HDRP to use deferred rendering for Lit Shaders (HDRP still renders advanced Materials using forward rendering).<br />&#8226; **Both**: allows the Camera to use deferred and forward rendering.<br /><br />Select **Both** to allow you to switch between forward and deferred rendering for Lit Shaders at runtime per Camera. Selecting a specific mode reduces build time and Shader memory because HDRP requires less Shader variants, but it's not possible to switch from one mode to the other at runtime. |
| N/A  | **Multisample Anti-aliasing Quality** | Use the drop-down to set the number of samples HDRP uses for multisample anti-aliasing (MSAA). The larger the sample count, the better the quality. Select **None** to disable MSAA.<br />This property is only visible when **Lit Shader Mode** is set to **Forward Only** or **Both**.                                                                                                                                                                                                                                                                                                                                                                                                                                                  |
| **Motion Vectors**                       | N/A | Enable the checkbox to enable motion vector support in HDRP. HDRP uses motion vectors for effects like screen space reflection (SSR) and motion blur. When disabled, motion blur has no effect and HDRP calculates SSR with lower quality.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |
| **Runtime Debug Display**                | N/A | Enable the checkbox to enable HDRP to use debug modes from the [Rendering Debugger](use-the-rendering-debugger.md) at runtime. Disable the checkbox to reduce build time and shader memory. This disables all property override options, all lighting debug modes, and all material property debug modes except GBuffer debug.                                                                                                                                                                                                                                                                                                                                                                |
| **Runtime AOV API**                      | N/A | Enable the checkbox to enable HDRP able to use the AOV API (rendering of material properties and lighting modes) at runtime. Disable this checkbox to reduce build time and shader memory. This disables all material properties and lighting modes.                                                                                                                                                                                                                                                                                                                                                                                                                                                               |
| **Terrain Hole**                         | N/A | Enable the checkbox to enable support for [Terrain Holes](https://docs.unity3d.com/2019.3/Documentation/Manual/terrain-PaintHoles.html) in HDRP. If you don't enable this, Terrain Holes aren't visible in your Scene.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
| **Transparent Backface**                 | N/A | Enable the checkbox to enable support for transparent back-face render passes in HDRP. If your Unity Project doesn't need to make a transparent back-face pass, disable this checkbox to reduce build time.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| **Transparent Depth Prepass**            | N/A | Enable the checkbox to enable support for transparent depth render prepasses in HDRP. If your Unity Project doesn't need to make a transparent depth prepass, disable this checkbox to reduce build time.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| **Transparent Depth Postpass**           | N/A | Enable the checkbox to enable support for transparent depth render postpasses in HDRP. If your Unity Project doesn't make use of a transparent depth postpass, disable this checkbox to reduce build time.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| **Custom Pass**                          | N/A | Enable the checkbox to enable support for [custom passes](Custom-Pass.md) in HDRP. If your Unity Project doesn't use custom passes, disable this checkbox to save memory.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| - **Custom Buffer Format**               | N/A | Specify the texture format for the custom buffer. If you experience banding issues due to your custom passes, you can change it to either `R11G11B10` if you don't need alpha, or `R16G16B16A16` if you do need alpha.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| **Realtime Raytracing (Preview)**        | N/A | Enable the checkbox to enable HDRP realtime ray tracing (Preview). It requires ray tracing-compatible hardware. For more information, refer to [Ray Tracing Getting Started](Ray-Tracing-Getting-Started.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
| **Visual Effects Ray Tracing (Preview)** | N/A | Enable the checkbox to enable support for ray tracing with Visual Effects in HDRP. **Realtime Raytracing (Preview)** must be enabled.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              |
| **Supported Ray Tracing Mode (Preview)** | N/A | Select the supported modes for ray tracing effects (**Performance**, **Quality**, or **Both**). For more information, refer to [Ray Tracing Getting Started](Ray-Tracing-Getting-Started.md).                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| - **LOD Bias**                           | N/A | Set the value that Cameras use to calculate their LOD bias. The Camera uses this value differently depending on the **LOD Bias Mode** you select.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| - **Maximum LOD Level**                  | N/A | Set the value that Cameras use to calculate their maximum level of detail. The Camera uses this value differently depending on the **Maximum LOD Level Mode** you select.                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 |
| **GPU Resident Drawer**                  | N/A | The GPU Resident Drawer automatically uses the [`BatchRendererGroup`](https://docs.unity3d.com/Manual/batch-renderer-group.html) API to draw GameObjects with GPU instancing. Refer to [Use the GPU Resident Drawer](gpu-resident-drawer.md) for more information.<br/><br/><ul><li>**Disabled**: Unity doesn't automatically draw GameObjects with GPU instancing.</li><li>**Instanced Drawing**: Unity automatically draws GameObjects with GPU instancing.</li></ul> |
| N/A | **Small-Mesh Screen-Percentage** | Set the screen percentage Unity uses to cull small GameObjects, to speed up rendering. Unity culls GameObjects that fill less of the screen than this value. This setting might not work if you use your own [Level of Detail (LOD) meshes](https://docs.unity3d.com/Manual/LevelOfDetail.html). Set the value to 0 to stop Unity culling small GameObjects.<br/><br/>To prevent Unity culling an individual GameObject that covers less screen space than this value, go to the **Inspector** window for the GameObject and add a **Disallow Small Mesh Culling** component. | 
| N/A | **GPU Occlusion Culling** | Enable Unity using the GPU instead of the CPU to exclude GameObjects from rendering when they're hidden behind other GameObjects. Refer to [Use GPU occlusion culling](gpu-culling.md) for more information. |

<a name="Decals"></a>

### Decals

Enable the checkbox to make HDRP support decals in your Unity Project.

These settings control the various decal parameters like draw distance and activation of decal layers.

<a name="decaltransparent"></a>

#### Transparent surfaces

This table lists the available global settings for decals in HDRP that apply specifically to transparent surfaces and describes their functions.

They allow configuration of the atlas that HDRP uses when it renders decals projected onto transparent surfaces.

| **Property**                            | **Description**                                                                                                                                                                                     |
|--------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Atlas Width**                            | The Decal Atlas width. This atlas stores all decals that project onto transparent surfaces.                                                                                                         |
| **Atlas Height**                           | The Decal Atlas height. This atlas stores all decals that project onto transparent surfaces.                                                                                                        |
| **Transparent Texture Resolution Tiers**   | Set the resolution that transparent textures take up within the decal atlas. The same resolution is used for all textures of a material (base color, normal, mask).                                 |
| **Low**                                    | Set the transparent texture resolution to this quality. Decal Projectors with their **Transparent Texture Resolution** set to **Low** use this resolution for their textures in the decal atlas.    |
| **Medium**                                 | Set the transparent texture resolution to this quality. Decal Projectors with their **Transparent Texture Resolution** set to **Medium** use this resolution for their textures in the decal atlas. |
| **High**                                   | Set the transparent texture resolution to this quality. Decal Projectors with their **Transparent Texture Resolution** set to **High** use this resolution for their textures in the decal atlas.   |
| **Maximum Clustered Decals on Screen**     | The maximum number of clustered decals that can affect transparent GameObjects on screen. Clustered decals refer to a list of decals that HDRP uses when it renders transparent GameObjects.        |

<a name="decalopaque"></a>

#### Opaque surfaces

This table lists the available global settings for decals in HDRP that apply specifically to opaque surfaces and describes their functions.

| **Property**                            | **Description**                                                                                                                                                                                     |
|--------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Draw Distance**                          | The maximum distance from the Camera at which Unity draws Decals.                                                                                                                                   |
| **Metal and Ambient Occlusion properties** | Enable the checkbox to allow decals to affect metallic and ambient occlusion Material properties. Enabling this feature has a performance impact.                                                   |
| **Layers**                                 | Enable the checkbox to allow decals to only affect specific layers.                                                                                                                                 |

<a name="DynamicResolution"></a>
### Dynamic Resolution

For more information about dynamic resolution and upscale filters, refer to [Dynamic Resolution](Dynamic-Resolution.md).

| **Property**                                | **Description**                                              |
|---------------------------------------------|--------------------------------------------------------------|
| **Enable**                                  | Enable the checkbox to make HDRP support dynamic resolution in your Unity Project. |
| **Advanced Upscalers By Priority** | Displays the list of advanced upscale filters that HDRP can use to increase the resolution after rendering at a lower resolution. To add an upscale filter, select the **Add** (**+**) button. To remove a filter, select it then select the **Remove** (**-**) button.<br/><br/>The options are:<ul><li>**DLSS**: NVIDIA Deep Learning Super Sampling. This option is available only if you enable the NVIDIA built-in package in the [Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html). For more information, refer to [DLSS settings](#dlss-settings).</li><li>**FSR2**: AMD FidelityFX Super Resolution 2.0. This option is available only if you enable the AMD built-in package in the [Package Manager window](https://docs.unity3d.com/Manual/upm-ui.html). For more information, refer to [FSR2 settings](#fsr2-settings).</li><li>**STP**: Spatial-Temporal Post-processing. For more information, refer to [STP settings](#stp-settings).</li></ul> |
| **Dynamic Resolution Type** | Sets how HDRP renders at a lower resolution. The options are:<ul><li>**Software**: Allocates render targets to accommodate the maximum resolution possible, then rescales the viewport accordingly. This allows the viewport to render at varying resolutions.</li><li>**Hardware**: Treats the render targets up to the back buffer as if they're all the scaled size. This means HDRP clears the render targets faster.</li></ul> |
| **Default Upscale Filter** | Sets the upscale filter HDRP uses to increase the resolution after rendering at a lower resolution.<br/><br/>The options are: <ul><li>**Catmull-Rom**: A bicubic upsample with 4 taps.</li><li>**Contrast Adaptive Sharpen**: An ultra-sharp upsample. This option is not meant for screen percentages less than 50% and still sharpens when you set the screen percentage to 100%. This filter uses FidelityFX (CAS) AMD™.</li><li>**FidelityFX Super Resolution 1.0 AMD™ (FSR1)**: A spatial super-resolution technology that leverages cutting-edge algorithms to produce high upscaling quality at very fast performance.</li><li>**TAA Upscale**: A temporal anti-aliasing upscaler that uses information from previous frames to produce high-quality visuals.</li></ul>If you add one or more upscale filters to **Advanced Upscalers By Priority**, this property becomes **Default Fallback Upscale Filter**. HDRP uses this filter if your build platform doesn't support the advanced upscale filters you add.|
| **Injection Point** | Sets when HDRP applies the Catmull-Rom upscale filter. This property is available here only if you set **Default Upscale Filter** to **Catmull-Rom**. For more information, refer to [Injection points dropdown](#injection-points). |
| **Override FSR Sharpness** | Enables an **FSR Sharpness** slider that lets you set the sharpness of the FSR1 upscale filter. A value of 1.0 means maximum sharpness. A value of 0 means no sharpening. You can also set the sharpness per camera in the [Camera Inspector window](hdrp-camera-component-reference.md). This property is available only if you set **Default Upscale Filter** to **FidelityFX Super Resolution 1.0**. |
| **TAA Upscale Injection Point** | Sets when HDRP applies the TAA Upscaling filter. This property is available here only if you set **Default Upscale Filter** to **TAA Upscaling**. For more information, refer to [Injection points dropdown](#injection-points). |
| **Use Mip Bias**                            | Apply a negative bias on the texture samplers of deferred, opaque, and transparent passes. This improves detail on textures but increases the texture fetching cost. Cost varies per platform. |
| **Force Screen Percentage**              | The specific screen percentage that HDRP uses for dynamic resolution. This property is only visible when you enable the **Force Screen Percentage**. |
| **Minimum Screen Percentage**               | The minimum screen percentage that dynamic resolution can reach. |
| **Maximum Screen Percentage**               | The maximum screen percentage that dynamic resolution can reach. This value must be higher than the **Min Screen Percentage**. |
| **Low Res Transparency Min Threshold**      | Sets the minimum resolution that HDRP can render transparency, as a percentage of full screen resolution. |
| **Low Res Screen Space GI Min Threshold** | Sets the minimum resolution that HDRP can render screen-space global illumination, as a percentage of full screen resolution.  |
| **Low Res Volumetric Clouds Min Threshold** | Sets the minimum resolution that HDRP can render volumetric clouds, as a percentage of full screen resolution.  |
| **Ray Tracing Half Resolution Threshold**   | Sets the minimum resolution that HDRP can render ray tracing at half resolution. If the resolution falls below this threshold, HDRP renders ray tracing at full resolution.  |

<a name="dlss-settings"></a>
#### DLSS settings

The following properties are available only if you add **DLSS** to **Advanced Upscalers By Priority**.

| **Property** | **Description** |
|-|-|
| **DLSS Mode** | Sets whether DLSS prioritizes quality or performance. The options are:<ul><li>**Maximum Quality**</li><li>**Balanced**</li><li>**Maximum Performance**</li><li>**Ultra Performance**</li></ul> |
| **DLSS Injection Point** | Sets when DLSS runs in the rendering pipeline. For more information, refer to [Injection points dropdown](#injection-points). |
| **DLSS Use Optimal Settings** | Enables DLSS to control screen percentage automatically. |

<a name="fsr2-settings"></a>
#### FSR2 settings

The following properties are available only if you add **FSR2** to **Advanced Upscalers By Priority**.

| **Property**                                 | **Description**                                              |
|-|-|
| **FSR2 Injection Point** | Sets when HDRP applies the FSR2 upscale filter. For more information, refer to [Injection points dropdown](#injection-points). |
| **FSR2 Enable Sharpness** | Enables an **FSR2 Sharpness** slider that lets you set the sharpness of the FSR2 upscale filter. A value of 1.0 means maximum sharpness. A value of 0 means no sharpening. You can also set the sharpness per camera in the [Camera Inspector window](hdrp-camera-component-reference.md). |
| **FSR2 Use Optimal Settings** | Enables the **FSR2 Mode** property. |
| **FSR2 Mode** | Sets whether FSR2 prioritizes quality or performance. This parameter is available only if you enable **FSR2 Use Optimal Settings**. The options are:<ul><li>**Quality**</li><li>**Balanced**</li><li>**Performance**</li><li>**Ultra Performance**</li></ul> |

<a name="stp-settings"></a>
#### STP settings

The following property is available only if you add **STP** to **Advanced Upscalers By Priority**.

| **Property** | **Description** |
|-|-|
| **STP Injection Point** | Sets when HDRP applies the STP upscale filter. For more information, refer to [Injection points dropdown](#injection-points). |

<a name="injection-points"></a>
#### Injection points dropdown

| **Option** | **Description** |
|-|-|
|**Before Post Process**|HDRP upscales when post-processing effects are at full resolution.|
|**After Depth Of Field**|HDRP upscales when depth of field is at low resolution, but post-processing effects are at full resolution.|
|**After Post Process**|HDRP upscales at the end of the pipeline, when post-processing effects are at low resolution.|

<a name="Compute Thickness"></a>

## Compute Thickness

| **Property**   | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Enable**     | Enable the checkbox to sample the thickness of GameObjects to use in a shader graph material. For more information, refer to [Sample and use material thickness](Compute-Thickness.md). |
| **Resolution** | Set the resolution of a material’s thickness: <br/>&#8226;**Quarter**: Renders the thickness at a quarter of the current screen resolution.<br/>&#8226;**Half**: Renders the thickness at half the current screen resolution. This resolution is the best balance of detail and performance.<br/>&#8226;**Full**: Renders the thickness at full screen resolution. |
| **Layer Mask** | Select one or more layers to compute the thickness of.       |

<a name="Lighting"></a>

## Lighting

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Screen Space Ambient Occlusion**   | Enable the checkbox to make HDRP support screen space ambient occlusion (SSAO). SSAO is a technique for approximating ambient occlusion efficiently in real time. |
| **Screen Space Global Illumination** | Enable the checkbox to make HDRP support screen space global illumination (SSGI). SSGI is a technique for approximating global illumination efficiently in real time. |
| **Volumetrics**                      | Enable the checkbox to make HDRP support volumetrics. This allows you to use **Volumetric Fog** for the **Fog Type** in the [Visual Environment](visual-environment-volume-override-reference.md). |
| **Light Layers**                     | Enable the checkbox to make HDRP support Light Layers. You can assign a Layer to a Light which then only lights up Mesh Renderers or Terrains with a matching rendering Layer. |
| **Rendering Layer Mask Buffer**      | Enable the checkbox to make HDRP write the Rendering Layer Mask of GameObjects in a fullscreen buffer target. This comes with a performance and memory cost.<br/>The [HD Sample Buffer node](https://docs.unity3d.com/Packages/com.unity.shadergraphlatest?subfolder=/manual/HD-Sample-Buffer-Node.html) in ShaderGraph can sample this target. |

### Light Probe Lighting
Use these settings in the **Quality** > **HDRP** menu to configure [Adaptive Probe Volumes](probevolumes.md).

| **Property**             | **Subproperty**                   | **Description**                                                                                                                                                                                                                                                                                                                                                                   |
|---------------------------|-------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Light Probe System**    | N/A                                 | <ul><li>**Light Probe Groups (Legacy)**: Use the same [Light Probe Group system](https://docs.unity3d.com/Manual/class-LightProbeGroup.html) as the Built-In Render Pipeline.</li><li>**Adaptive Probe Volumes**: Use [Adaptive Probe Volumes](probevolumes.md).</li></ul>                                                                                                      |
| N/A                       | **Memory Budget**                  | Limits the width and height of the textures that store baked Global Illumination data, which determines the amount of memory Unity sets aside to store baked Adaptive Probe Volume data. These textures have a fixed depth.<br/>Options: <ul><li>**Memory Budget Low**</li><li>**Memory Budget Medium**</li><li>**Memory Budget High**</li></ul>                                     |
| N/A                       | **SH Bands**                       | Determines the [spherical harmonics (SH) bands](https://docs.unity3d.com/Manual/LightProbes-TechnicalInformation.html) Unity uses to store probe data. L2 provides more precise results, but uses more system resources.<br/>Options: <ul><li>**Spherical Harmonics L1**</li><li>**Spherical Harmonics L2**</li></ul>                                                              |
| **Lighting Scenarios**    | N/A                                 | Enable to use Lighting Scenarios. Refer to [Bake different lighting setups using Lighting Scenarios](probevolumes-bakedifferentlightingsetups.md) for more information.                                                                                                                                                                                                          |
| N/A                       | **Scenario Blending**              | Enable blending between different Lighting Scenarios. This uses more memory and makes rendering slower.                                                                                                                                                                                                                                                                        |
| N/A                       | **Scenario Blending Memory Budget** | Limits the width and height of the textures that Unity uses to blend between Lighting Scenarios. This determines the amount of memory Unity sets aside to store Lighting Scenario blending data, and store data while doing the blending operation. These textures have a fixed depth. <br/>Options: <br/> &#8226; **Memory Budget Low**<br/> &#8226; **Memory Budget Medium**<br/> &#8226; **Memory Budget High** |
| **Enable GPU Streaming**  | N/A                                 | Enable to stream Adaptive Probe Volume data from CPU memory to GPU memory at runtime. Refer to [Streaming Adaptive Probe Volumes](probevolumes-streaming.md) for more information.                                                                                                                                                                                              |
| **Enable Disk Streaming** | N/A                                 | Enable to stream Adaptive Probe Volume data from disk to CPU memory at runtime. [Streaming Adaptive Probe Volumes](probevolumes-streaming.md) for more information.                                                                                                                                                                                                             |
| **Estimated GPU Memory Cost** | N/A                            | Indicates the amount of texture data used by Adaptive Probe Volumes in your project. This includes textures used both for Global Illumination and Lighting Scenario blending.                                                                                                                                                                                                    |
### Cookies

Use the Cookie settings to configure the maximum resolution of the atlas and it's format. A bigger resolution means that you can have more cookies on screen at one time or use bigger cookies texture in general. Increasing for format will allow you to handle HDR cookies and have better precision at the cost of memory.

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **2D Atlas Size**      | Use the drop-down to select the maximum size for 2D cookie atlas. HDRP uses 2D cookies for Directional, Spot Lights and Area Lights. |
| **2D Atlas Last Valid Mip** | Adds padding to prevent area light cookie border from being cut but can blur the texture a lot if too high values are used. Generally the default value (0) works well in most cases. |
| **Cookie Format** | The format of the cookies that HDRP will use, using R16G16B16A16 instead of R11G11B10 will double the memory usage but help you to avoid banding and adds the support for EXR cookies. |

### Reflections

Use the Reflection settings to configure the max number and resolution of the probes and whether Unity should compress the Reflection Probe cache or not. The Reflection Probe cache is runtime memory that HDRP reserves for Reflection Probes. The cache is a first in, first out list that stores the currently visible Reflection Probes.

| **Property**                                    | **Subproperty**  | **Description**                                                                                                                                                                                                                                                       |
|-------------------------------------------------|---|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Screen Space Reflection**                     | N/A | Enable the checkbox to make HDRP support [screen space reflection](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html). SSR is a technique for calculating reflections by reusing screen space data.                                           |
| N/A                               | **Transparent**  | Enable the checkbox to make HDRP support [screen space reflection](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html) on transparent materials. This feature requires the transparent depth render prepasses to be enabled on the HDRP asset. |
| **Reflection and Planar Probes Format**         | N/A | Color format used for reflection and planar probes.                                                                                                                                                                                                                   |
| **Compress Baked Reflection Probes**            | N/A | Compress baked [Reflection Probe](Reflection-Probe.md) data, which conserves disk space.                                                                                                                                                                              |
| **Reflection 2D Atlas Size**                    | N/A | Select a resolution for the cube and planar probe atlases to define the quantity of reflection probes you can render simultaneously, and their resolution.                                                                                                            |
| **Reflection 2D Atlas Last Valid Cube Mip**     | N/A | Add padding to hide sharp seams in Reflection Probe cube mip data. Values above 3 can blur the probe texture too much.                                                                                                                                                |
| **Reflection 2D Atlas Last Valid Planar Mip**   | N/A | Add padding to hide sharp seams in Reflection Probe planar mip data. Values above 0 can blur the probe texture too much.                                                                                                                                              |
| **Cube Resolution Tiers**                     | N/A | N/A                                                                                                                                                                                                                                                                   |
| N/A                          | **L**                                         | Define the lowest possible resolution for cube Reflection Probes in this project.                                                                                                                                                                                     |
| N/A                          | **M**                                         | Define the medium resolution for cube Reflection Probes in this project.                                                                                                                                                                                              |
| N/A                          | **H**                                         | Define the highest possible resolution for cube Reflection Probes in this project.                                                                                                                                                                                    |
| **Planar Resolution Tiers**                   | N/A |                                                                                                                                                                                                                                                                       |
| N/A                          | **L**                                         | Define the lowest possible resolution for planar Reflection Probes in this project.                                                                                                                                                                                   |
| N/A                          | **M**                                         | Define the medium resolution for planar Reflection Probes in this project.                                                                                                                                                                                            |
| N/A                          | **H**                                         | Define the highest possible resolution for planar Reflection Probes in this project.                                                                                                                                                                                  |
| **Max Cube Reflection On Screen**               | N/A | The maximum number of cube reflections on screen at once.                                                                                                                                                                                                             |
| **Max Planar Reflection On Screen**             | N/A | The maximum number of planar reflections on screen at once.                                                                                                                                                                                                           |
| **Decrease Reflection Probe Resolution To Fit** | N/A | Decrease the Planar and Reflection Probe resolution in the reflection 2D atlas if this texture doesn't fit in the atlas.                                                                                                                                              |

<a name="SkyLighting"></a>

### Sky

These settings control skybox reflections and skybox lighting.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Reflection Size**        | Use the drop-down to select the maximum resolution of the cube map HDRP uses to manage fallback reflection when no local reflection probes are present. This property has no effect on the quality of the sky itself. |
| **Lighting Override Mask** | Use the drop-down to select the [Volume](understand-volumes.md) layer mask HDRP uses to override sky lighting. Use this to decouple the display sky and lighting. See the [Environment Lighting](Environment-Lighting.md#DecoupleVisualEnvironment) for information on how to decouple environment lighting from the sky background. |

### Shadow

These settings adjust the size of the shadowmask. Smaller values causes Unity to discard more distant shadows, while higher values lead to Unity displaying more shadows at longer distances from the Camera.

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Shadowmask**                  | Enable the checkbox to make HDRP support the [Shadowmask lighting mode](Lighting-Mode-Shadowmask.md) in your Unity Project. |
| **Maximum** **Shadow on Screen** | The maximum number of shadows you can have in view. A Spot Light casts a single shadow, a Point Light casts six shadows, and a Directional Light casts shadows equal to the number of cascades defined in the [HD Shadow Settings](Override-Shadows.md) override. |
| **Punctual Shadow Filtering Quality** | Use the drop-down to select the filtering quality for punctual shadows. A higher shadow quality makes the shadow softer. For information on each filtering quality preset, see the [Filtering Quality](#filtering-quality) table. |
| **Directional Shadow Filtering Quality** | Use the drop-down to select the filtering quality for punctual shadows. A higher shadow quality makes the shadow softer. For information on each filtering quality preset, see the [Filtering Quality](#filtering-quality) table. |
| **Area Shadow Filtering Quality**| Use the drop-down to select the filtering quality for area shadows. Higher values increase the area shadow quality in HDRP as better filtering improves the shape of the penumbra of very soft shadows and reduces light leaking. For information on each area filtering quality preset, see the [Filtering Quality](#filtering-quality) table. |
| **Screen Space Shadows**         | Enable the checkbox to allow HDRP to compute shadows in a separate pass and store them in a screen-aligned Texture. |
| - **Maximum**                    | Set the maximum number of screen space shadows that HDRP can handle. |
| - **Buffer Format**              | Defines the format (R11G11B10 or R16G16B16A16) of the buffer used for screen space shadows.|

<a name="ShadowMapSettings"></a>

The following sections allow you to customize the shadow atlases and individual shadow resolution tiers for each type of Light in HDRP. Shadow resolution tiers are useful because, instead of defining the shadow resolution for each individual Light as a number, you can assign a numbered resolution to a named shadow resolution tier then use the named tier instead of rewriting the number. For example, instead of setting the resolution of each Light to 512, you could say that **Medium** resolution shadows have a resolution of 512 and then set the shadow quality of each Light to be **Medium**. This way, you can more easily have consistent shadow quality across your HDRP Project.

The three sections here are:

- **Directional Light Shadows**
- **Punctual Light Shadows**
- **Area Light Shadows**

They all share the same properties, except **Directional Light Shadows** which doesn't include **Resolution** or **Dynamic Rescale** and **Cached Shadow Atlas Resolution**.

| **Light Atlas property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Resolution**      | Use the drop-down to select the resolution of the shadow atlas. |
| **Precision**       | Use the drop-down to select the precision of the shadow map. This sets the bit depth of each pixel of the shadow map. **16 bit** is faster and uses less memory at the expense of precision. |
| **Dynamic Rescale** | Enable the checkbox to allow HDRP to rescale the shadow atlas if all the shadows on the screen don't currently fit onto it. |

| **Shadow Resolution Tiers**      | **Description**                                                             |
| ---------------------------------- | ------------------------------------------------------------ |
| **L**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Low** use this resolution for their shadows. |
| **M**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Medium** use this resolution for their shadows. |
| **H**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **High** use this resolution for their shadows. |
| **U**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Ultra** use this resolution for their shadows. |
| **Maximum Shadow Resolution**      | Set the maximum resolution of any shadow map of this Light type. If you set any shadow resolution to a value higher than this, HDRP clamps it to this value. |
| **Cached Shadow Atlas Resolution** | Use the drop-down to select the resolution of the shadow atlas used for cached shadows (Update mode set to OnEnable or OnDemand). |

<a name="filtering-quality"></a>
#### Filtering Quality

| **Punctual Shadow Filtering Quality** | **Algorithm**                                       |
| ---------------------------- | ------------------------------------------------------------ |
| **Low**                      | Percentage Closer Filtering (PCF) 3x3 (4 taps). |
| **Medium**                   | PCF 5x5 (9 taps). |
| **High**                     | Percentage Closer Soft Shadows (PCSS). |

| **Directional Shadow Filtering Quality** | **Algorithm**                                    |
| ---------------------------- | ------------------------------------------------------------ |
| **Low**                      | PCF Tent 5x5 (9 taps). |
| **Medium**                   | PCF Tent 5x5 (9 taps). |
| **High**                     | PCSS. |

| **Area Shadow Filtering Quality** | **Algorithm**                                                |
| --------------------------------- | ------------------------------------------------------------ |
| **Medium**                        | Exponential Variance Shadow Map (EVSM). |
| **High**                          | PCSS. |

The PCF algorithm applies a fixed size blur. PCSS applies a different blur size depending on the distance between the shadowed pixel and the shadow caster. This results in a more realistic shadow that is also more resource intensive to compute.

PCSS: You can change the sample count to decrease the resource intensity of this algorithm, which decreases the quality of these shadows. To change the sample count, set the **Filter Sample Count** and **Blocker Sample Count** in the Inspector of each Light component.

The following factors determine the softness of PCSS shadows:
- Point and Spot Lights: The **Shape** property **Radius**.
- Directional Lights: **Angular Diameter**.
- Area Lights: The position and size of the shadow's near plane, determined by the dimensions of the Light and its **Near Plane** distance setting.

Use **Radius Scale for Softness** or **Angular Diameter Scale for Softness** for additional shadow softness adjustments.

### Lights

Use these settings to enable or disable settings relating to lighting in HDRP.

| **Property**                              | **Description**                                              |
| ----------------------------------------- | ------------------------------------------------------------ |
| **Maximum Directional On Screen**         | The maximum number of Directional Lights HDRP can manage on screen at once. |
| **Maximum Punctual On Screen**            | The maximum number of [Point and Spot Lights](Glossary.md#PunctualLight) HDRP can manage on screen at once. |
| **Maximum Area On Screen**                | The maximum number of area Lights HDRP can manage on screen at once. |
| **Maximum Lights Per Cell (Ray Tracing)** | The maximum number of Lights that an individual grid cell in a [Light Cluster](Ray-Tracing-Light-Cluster.md) can store. |

## Material

| **Property**                | **Subproperty** | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
|-----------------------------|---|---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Distortion**              | N/A | Enable the checkbox to make HDRP support distortion. If your Unity Project doesn't use distortion, disable this checkbox to reduce build time.                                                                                                                                                                                                                                                                                                                                                                                                                                                         |
| **Subsurface Scattering**   | N/A | Enable the checkbox to make HDRP support subsurface scattering (SSS). SSS describes light penetration of the surface of a translucent object                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| N/A | **High Quality**           | Enable the checkbox to increase the SSS Sample Count and enable high quality subsurface scattering. Increasing the sample count greatly increases the performance cost of the Subsurface Scattering effect.                                                                                                                                                                                                                                                                                                                                                                                             |
| **Fabric BSDF Convolution** | N/A | By default, Fabric Materials reuse the Reflection Probes that HDRP calculates for the Lit Shader (GGX BRDF). Enable the checkbox to make HDRP calculate another version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. This increases the resource intensity because HDRP must condition two Reflection Probes instead of one. It also reduces the number of visible Reflection Probes in the current view by half because the size of the cache that stores Reflection Probe data doesn't change and must now store both versions of each Reflection Probe. |

## Post-processing

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **Grading LUT Size**   | The size of the internal and external color grading lookup textures (LUTs). This size is fixed for the Project. You can not mix and match LUT sizes, so decide on a size before you start the color grading process. The default value, **32**, provides a good balance of speed and quality. |
| **Grading LUT Format** | Use the drop-down to select the format to encode the color grading LUTs with. Lower precision formats are faster and use less memory at the expense of color precision. These formats directly map to their equivalent in the built-in [GraphicsFormat](https://docs.unity3d.com/ScriptReference/Experimental.Rendering.GraphicsFormat.html) enum value. |
| **Buffer Format** |  Use the drop-down to select the format of the color buffers that are used in the post-processing passes. Lower precision formats are faster and use less memory at the expense of color precision. These formats directly map to their equivalent in the built-in [GraphicsFormat](https://docs.unity3d.com/ScriptReference/Experimental.Rendering.GraphicsFormat.html) enum value.

## Post-processing Quality Settings
These settings define the quality levels (low, medium, high) related to post processing effects in HDRP. For a detailed description of each setting, see the [Post-processing in HDRP](Post-Processing-Main.md) section of the documentation.

## Virtual Texturing

| **Property**            | **Description**                                                 |
| ----------------------- | --------------------------------------------------------------- |
| **CPU Cache Size**      | Amount of CPU memory (in MB) that can be allocated by the Streaming Virtual Texturing system to cache texture data. |
| **GPU Cache Size per Format** | Amount of GPU memory (in MB) that can be allocated per format by the Streaming Virtual Texturing system to cache texture data. The value assigned to None is used for all unspecified formats. |
| **Preload Textures Per Frame** | The number of textures Unity tries to preload their least detailed mipmap levels (least being 128×128) into GPU memory per frame. Use this to avoid texture pop-in. The range is 0 through 1024. The default is 0, which disables preloading. |
| **Preload Mip Count** | The number of mipmap levels to preload. The range is 1 through 9. The default is 1, which preloads only the highest mipmap level with the smallest size (128×128 pixels, the size of a Streaming Virtual Texturing tile). |
