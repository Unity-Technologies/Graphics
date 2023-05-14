# The High Definition Render Pipeline Asset

The High Definition Render Pipeline (HDRP) Asset controls the global rendering settings of your Project and creates an instance of the render pipeline.

Unity only allocates memory and builds shader variants for features you enable in the HDRP Asset. This means that you can disable features your project does not use to save memory. Since certain features require shader variants or other resources when Unity builds your project, you can only enable and disable features at edit time. However, it is possible to toggle the rendering of particular features at runtime, just not using the HDRP Asset. Instead, [Frame-Settings](Frame-Settings.md) control the features that cameras in the scene render. Frame Settings can only toggle features that are enabled in the HDRP Asset; they cannot enable features that are disabled.

<a name="CreatingAnHDRPAsset"></a>

## Creating an HDRP Asset

A new Project using the HDRP template includes an HDRP Asset file named HDRenderPipelineAsset in the Assets/Settings folder.

If you [upgrade a Project to HDRP](Upgrading-To-HDRP.md) and therefore do not use the HDRP template, you need to add an HDRP Asset to your Project. To create and customize an HDRP Asset:

1. In the Unity Editor, go to the Project window and navigate to the folder you want to create your HDRP Asset in. This folder must be inside the **Assets** folder; you can not create Assets in the **Packages** folder.
2. In the main menu, go to **Assets > Create > Rendering** and click **HDRP Asset**.
3. Enter a name for the **HDRP Asset** and press the Return key to confirm it.

When you have created an HDRP Asset, you must assign it it to the pipeline:

1. Navigate to **Edit > Project Settings > Graphics** and locate the **Scriptable Render Pipeline Settings** property at the top.
2. Either drag and drop the HDRP Asset into the property field, or use the object picker (located on the right of the field) to select it from a list of all HDRP Assets in your Project.

Unity now uses the High Definition Render Pipeline (HDRP) in your Unity Project. HDRP does not support gamma space, so your Project must use linear color space, To do this:

1. Navigate to **Edit > Project Settings > Player > Other Settings** and locate the **Color Space** property.
2. Select **Linear** from the **Color Space** drop-down.

You can create multiple HDRP Assets containing different settings. This is useful for Project that support multiple platforms, such as PC, Xbox One and PlayStation 4. In each HDRP Asset, you can change settings to suite the hardware of each platform and then assign the relevant one when building your Project for each platform. For more information on using creating HDRP Assets to target different platforms, see [Scalability in HDRP](Scalability-Manual.md).

To change which HDRP Asset your render pipeline uses, either manually select an HDRP Asset in the active Quality Level of the Quality Settings window (as shown above), or use the QualitySettings.renderPipeline property via script.

To change which HDRP Asset your render pipeline uses by default, either manually select an HDRP Asset in the Graphics Settings window (as shown above), or use the GraphicsSettings.renderPipelineAsset property via script.

When you create an HDRP Asset, open it in the Inspector to edit its properties.

## Rendering

| **Property**                            | **Description**                                              |
| --------------------------------------- | ------------------------------------------------------------ |
| **Color Buffer Format**                 | The format of the color buffer that HDRP will use for rendering, using R16G16B16A16 instead of R11G11B10 will double the memory usage but help you to avoid banding. R16G16B16A16 is also required for [Alpha-Output](Alpha-Output.md).|
| **Lit Shader Mode**                     | Use the drop-down to choose which mode HDRP uses for the [Lit Shader](Lit-Shader.md).<br />&#8226; **Forward Only**: forces HDRP to only use forward rendering for Lit Shaders.<br />&#8226; **Deferred Only**: forces HDRP to use deferred rendering for Lit Shaders (HDRP still renders advanced Materials using forward rendering).<br />&#8226; **Both**: allows the Camera to use deferred and forward rendering.<br /><br />Select **Both** to allow you to switch between forward and deferred rendering for Lit Shaders at runtime per Camera. Selecting a specific mode reduces build time and Shader memory because HDRP requires less Shader variants, but it is not possible to switch from one mode to the other at runtime. |
| **- Multisample Anti-aliasing Quality** | Use the drop-down to set the number of samples HDRP uses for multisample anti-aliasing (MSAA). The larger the sample count, the better the quality. Select **None** to disable MSAA.<br />This property is only visible when **Lit Shader Mode** is set to **Forward Only** or **Both**. |
| **Motion Vectors**                      | Enable the checkbox to make HDRP support motion vectors. HDRP uses motion vectors for effects like screen space reflection (SSR) and motion blur. When disabled, motion blur has no effect and HDRP calculates SSR with lower quality. |
| **Runtime Debug Display**               | Enable the checkbox to make HDRP able to use debug modes from the [Rendering Debugger](Render-Pipeline-Debug-Window.md) at runtime. Disable the checkbox to reduce build time and shader memory. This disables the following debug modes: All material property debug modes except GBuffer debug, the various property override options, and all the lighting debug modes.  |
| **Runtime AOV API**                     | Enable the checkbox to make HDRP able to use the AOV API (rendering of material properties and lighting modes) at runtime. Disable this checkbox to reduce build time and shader memory. This disables the following AOV modes: All material properties and lighting modes. |
| **Dithering Cross-fade**                | Enable the checkbox to make HDRP support dithering cross fade. This allows HDRP to implement smooth transitions between a GameObject’s LOD levels. When disabled, this reduces build time if you are not using LOD fade. |
| **Terrain Hole**                        | Enable the checkbox to make HDRP support [Terrain Holes](https://docs.unity3d.com/2019.3/Documentation/Manual/terrain-PaintHoles.html). If you do not enable this, Terrain Holes are not visible in your Scene. |
| **Transparent Backface**                | Enable the checkbox to make HDRP support transparent back-face render passes. If your Unity Project does not need to make a transparent back-face pass, disable this checkbox to reduce build time. |
| **Transparent Depth Prepass**           | Enable the checkbox to make HDRP support transparent depth render prepasses. If your Unity Project does not need to make a transparent depth prepass, disable this checkbox to reduce build time . |
| **Transparent Depth Postpass**          | Enable the checkbox to make HDRP support transparent depth render postpasses. If your Unity Project does not make use of a transparent depth postpass. Uncheck this checkbox to reduce build time . |
| **Custom Pass**                         | Enable the checkbox to make HDRP support custom passes. If your Unity Project does not make use [Custom Passes](Custom-Pass.md), Uncheck this checkbox to save memory . |
| - **Custom Buffer Format**              | Specify the texture format for the custom buffer. If you experience banding issues due to your custom passes, you can change it to either `R11G11B10` if you don't need alpha or `R16G16B16A16`. |
| **Realtime Raytracing (Preview)**       | Enable the checkbox to enable HDRP realtime ray tracing (Preview). It requires to have ray tracing compatible hardware. For more information, please refer to the [Ray Tracing Getting Started](Ray-Tracing-Getting-Started.md#HardwareRequirements) page. |
| **Supported Ray Tracing Mode (Preview)**| Select the supported modes for ray tracing effects (Performance, Quality or Both). For more information, see the [Ray Tracing Getting Started](Ray-Tracing-Getting-Started.md) page. |
| - **LOD Bias**                          | Set the value that Cameras use to calculate their LOD bias. The Camera uses this value differently depending on the **LOD Bias Mode** you select. |
| - **Maximum LOD Level**                 | Set the value that Cameras use to calculate their maximum level of detail. The Camera uses this value differently depending on the **Maximum LOD Level Mode** you select. |




<a name="Decals"></a>

### Decals

These settings control the draw distance and resolution of the decals atlas that HDRP uses when it renders decals projected onto transparent surfaces.

| **Property**                                 | **Description**                                              |
| -------------------------------------------- | ------------------------------------------------------------ |
| **Enable**                                   | Enable the checkbox to make HDRP support decals in your Unity Project. |
| **- Draw Distance**                          | The maximum distance from the Camera at which Unity draws Decals. |
| **- Atlas Width**                            | The Decal Atlas width. This atlas stores all decals that project onto transparent surfaces. |
| **- Atlas Height**                           | The Decal Atlas height. This atlas stores all decals that project onto transparent surfaces. |
| **- Metal and Ambient Occlusion properties** | Enable the checkbox to allow decals to affect metallic and ambient occlusion Material properties. Enabling this feature has a performance impact. |
| **- Maximum Clustered Decals on Screen**     | The maximum number of clustered decals that can affect transparent GameObjects on screen. Clustered decals refer to a list of decals that HDRP uses when it renders transparent GameObjects. |
| **- Layers**                                 | Enable the checkbox to allow decals to only affect specific layers. <a name="decallayers"></a>|

<a name="DynamicResolution"></a>

### Dynamic Resolution

| **Property**                                | **Description**                                              |
| ------------------------------------------- | ------------------------------------------------------------ |
| **Enable**                                  | Enable the checkbox to make HDRP support dynamic resolution in your Unity Project. |
| **- Enable DLSS**                           | Enable the checkbox to make HDRP support NVIDIA Deep Learning Super Sampling (DLSS).<br/>This property only appears if you enable the NVIDIA package (com.unity.modules.nvidia) in your Unity project. |
| **-- Mode**                                 | Use the drop-down to select which performance mode DLSS operates on. The options are:<br/>&#8226; **Balanced**: - Balances performance with quality.<br/>&#8226; **MaxPerf**: - Fast performance, lower quality.<br/>&#8226; **MaxQuality**: - High quality, lower performance.<br/>&#8226; **UltraPerformance**: - Fastest performance, lowest quality. |
| **-- Injection Point**                      | Use the drop-down to select at which point DLSS runs in the rendering pipeline: <br/>&#8226; **Before Post**: - DLSS runs when all post processing effects are at full resolution.<br/>&#8226; **After Depth Of Field**: - Depth of field runs at a low resolution and DLSS upscales everything in the next rendering step. All other post processing effects run at full resolution. <br/>&#8226; **After Post Process**: - DLSS runs at the end of the pipeline when all post process are at low resolution.<br/>&#8226; |
| **-- Use Optimal Settings**                 | Enable the checkbox to make DLSS control the Sharpness and Screen Percentage automatically. |
| **-- Sharpness**                            | Controls how the DLSS upsampler renders edges on the image. More sharpness usually means more contrast and a clearer image but can increase flickering and fireflies. Unity ignores this property if you enable **Use Optimal Settings**. |
| **- Dynamic Resolution Type**               | Use the drop-down to select the type of dynamic resolution HDRP uses:<br />&#8226; **Software**: This option allocates render targets to accommodate the maximum resolution possible, then rescales the viewport accordingly. This allows the viewport to render at varying resolutions. <br />&#8226; **Hardware**: This option treats the render targets, up until the back buffer, as if they are all the scaled size. This means HDRP clears the render targets faster. |
| **- Upscale Filter**                        | Use the drop-down to select the filter that HDRP uses for upscaling unless overridden by user via script.<br />&#8226; **Catmull-Rom**: A bicubic upsample with 4 taps.<br />&#8226; **Contrast Adaptive Sharpen**: An ultra sharp upsample. This option is not meant for screen percentages less than 50% and still sharpens when the screen percentage is set to 100%. This uses **FidelityFX (CAS) AMD™**: For information about FidelityFX and Contrast Adaptive Sharpening, see [AMD FidelityFX](https://www.amd.com/en/technologies/radeon-software-fidelityfx). <br />&#8226; **FidelityFX Super Resolution 1.0 AMD™**: a spatial super-resolution technology that leverages cutting-edge algorithms to produce impressive upscaling quality at very fast performance. |
| **- Use Mip Bias**                          | Apply a negative bias on the texture samplers of deferred, opaque and transparent passes. This improves detail on textures but increases the texture fetching cost. Cost varies per platform. |
| **- Minimum Screen Percentage**             | The minimum screen percentage that dynamic resolution can reach. |
| **- Maximum Screen Percentage**             | The maximum screen percentage that dynamic resolution can reach. This value must be higher than the **Min Screen Percentage**. |
| **- Force Screen Percentage**               | Enable the checkbox to force HDRP to use a specific screen percentage for dynamic resolution. This feature is useful for debugging dynamic resolution. |
| **- Forced Screen Percentage**              | The specific screen percentage that HDRP uses for dynamic resolution. This property is only visible when you enable the **Force Screen Percentage**.. |
| **- Low Res Transparency Min Threshold**    | The minimum percentage threshold allowed to clamp low resolution transparency. When the resolution percentage falls below this threshold, HDRP will clamp the low resolution to this percentage. |
| **- Ray Tracing Half Resolution Threshold** | The minimum percentage threshold allowed to render ray tracing effects at half resolution. When the resolution percentage falls below this threshold, HDRP will render ray tracing effects at full resolution. |
| **Water**   <a name="water"></a> | Controls for the Water System. |
| **- Enable**    | Enable the Water System.|
| **- Simulation Resolution**    | Set the resolution of the water simulation. Higher values use more system resources but provide higher visual quality. |
| **- Script Interactions**    | Enable to have HDRP calculate the height of the water simulation on the CPU. Also makes it possible for you to query height data for specific points on the water's surface. You can use this data for customizations like your own buoyancy implementation, for example. |
<a name="water-scriptinteractions"></a>

<a name="Lighting"></a>

## Lighting

| **Property**                         | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Screen Space Ambient Occlusion**   | Enable the checkbox to make HDRP support screen space ambient occlusion (SSAO). SSAO is a technique for approximating ambient occlusion efficiently in real time. |
| **Screen Space Global Illumination** | Enable the checkbox to make HDRP support screen space global illumination (SSGI). SSGI is a technique for approximating global illumination efficiently in real time. |
| **Volumetrics**                      | Enable the checkbox to make HDRP support volumetrics. This allows you to use **Volumetric Fog** for the **Fog Type** in the [Visual Environment](Override-Visual-Environment.md). |
| **Light Layers**                     | Enable the checkbox to make HDRP support Light Layers. You can assign a Layer to a Light which then only lights up Mesh Renderers or Terrain with a matching rendering Layer. <a name="lightlayers"></a>|
| **Rendering Layer Mask Buffer**      | Enable the checkbox to make HDRP write the Rendering Layer Mask of GameObjects in a fullscreen buffer target. This comes with a performance and memory cost.<br/>The [HD Sample Buffer node](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest?subfolder=/manual/HD-Sample-Buffer-Node.html) in ShaderGraph can sample this target. |

### Light Probe Lighting
Use these settings in the **Quality** > **HDRP** menu to configure [Probe Volumes](probevolumes.md).

| **Property**                        | **Description**                                                     |
|---------------------------------|-----------------------------------------------------------------|
| **Light Probe System**              | &#8226; **Light Probe Groups (Legacy)**: Use the same [Light Probe Group system](https://docs.unity3d.com/Manual/class-LightProbeGroup.html) as the Built-In Render Pipeline. <br/>  &#8226; **Probe Volumes**: Use Probe Volumes.                            |
| **Memory Budget**                   | Limits the width and height of the textures that store baked Global Illumination data, which determines the amount of memory Unity sets aside to store baked Probe Volume data. These textures have a fixed depth.<br/>Options: <br/>&#8226; **Memory Budget Low**<br/>&#8226;**Memory Budget Medium**<br/>&#8226;**Memory Budget High** |
| **Scenario Blending Memory Budget** | Limits the width and height of the textures that Unity uses to blend between lighting scenarios. This determines the amount of memory Unity sets aside to store Lighting Scenario blending data, and store data while doing the blending operation. These textures have a fixed depth. <br/>Options: <br/> &#8226; **Memory Budget Low**<br/> &#8226; **Memory Budget Medium**<br/> &#8226; **Memory Budget High** |                                                 |
| **SH Bands**                        | Determines the [spherical harmonics (SH) bands](https://docs.unity3d.com/Manual/LightProbes-TechnicalInformation.html) Unity uses to store probe data. L2 provides more precise results, but uses more system resources.<br/>Options: <br/>&#8226; **Spherical Harmonics L1** (default)<br/> &#8226; **Spherical Harmonics L2**               |
| **Enable Streaming**                | Enable to use [streaming](probevolumes-streaming.md).  |
| **Estimated GPU Memory cost**      | Indicates the amount of Global Illumination and scenario blending texture data associated with Probe Volumes in your project.|


### Cookies

Use the Cookie settings to configure the maximum resolution of the atlas and it's format. A bigger resolution means that you can have more cookies on screen at one time or use bigger cookies texture in general. Increasing for format will allow you to handle HDR cookies and have better precision at the cost of memory.

| **Property**           | **Description**                                              |
| ---------------------- | ------------------------------------------------------------ |
| **2D Atlas Size**      | Use the drop-down to select the maximum size for 2D cookie atlas. HDRP uses 2D cookies for Directional, Spot Lights and Area Lights. |
| **2D Atlas Last Valid Mip** | Adds padding to prevent area light cookie border to be cut but can blur the texture a lot if too high values are used. Generally the default value (0) works well in most cases. |
| **Cookie Format** | The format of the cookies that HDRP will use, using R16G16B16A16 instead of R11G11B10 will double the memory usage but help you to avoid banding and adds the support for EXR cookies. |

### Reflections

Use the Reflection settings to configure the max number and resolution of the probes and whether Unity should compress the Reflection Probe cache or not. The Reflection Probe cache is runtime memory that HDRP reserves for Reflection Probes. The cache is a first in, first out list that stores the currently visible Reflection Probes.

| **Property**                             | **Description**                                              |
| ---------------------------------------- | ------------------------------------------------------------ |
| **Screen Space Reflection**              | Enable the checkbox to make HDRP support [screen space reflection](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html). SSR is a technique for calculating reflections by reusing screen space data. |
| **- Transparent**                        | Enable the checkbox to make HDRP support [screen space reflection](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html) on transparent materials. This feature requires the transparent depth render prepasses to be enabled on the HDRP asset.|
| **Reflection and Planar Probes Format**  | Color format used for reflection and planar probes.          |
| **Compress Baked Reflection Probes**      | Compress baked [Reflection Probe](Reflection-Probe.md) data, which conserves disk space. |
| **Reflection 2D Atlas Size**         | Select a resolution for the cube and planar probe atlases to define the quantity of reflection probes you can render simultaneously, and their resolution. |
| **Reflection 2D Atlas Last Valid Cube Mip** | Add padding to hide sharp seams in Reflection Probe cube mip data. Values above 3 can blur the probe texture too much. |
| **Reflection 2D Atlas Last Valid Planar Mip** | Add padding to hide sharp seams in Reflection Probe planar mip data. Values above 0 can blur the probe texture too much. |
| ***Cube Resolution Tiers***            |                                                              |
| **- L**                                  | Define the lowest possible resolution for cube Reflection Probes in this project. |
| **- M**                                  | Define the medium resolution for cube Reflection Probes in this project. |
| **- H**                                  | Define the highest possible resolution for cube Reflection Probes in this project. |
| ***Planar Resolution Tiers***            |                                                              |
| **- L**                                  | Define the lowest possible resolution for planar Reflection Probes in this project. |
| **- M**                                  | Define the medium resolution for planar Reflection Probes in this project. |
| **- H**                                  | Define the highest possible resolution for planar Reflection Probes in this project. |
| **Max Cube Reflection On Screen**      | The maximum number of cube reflections on screen at once.  |
| **Max Planar Reflection On Screen**      | The maximum number of planar reflections on screen at once.  |
| **Decrease Reflection Probe Resolution To Fit**      | Decrease the Planar and Reflection Probe resolution in the reflection 2D atlas if this texture doesn't fit in the atlas.  |

<a name="SkyLighting"></a>

### Sky

These settings control skybox reflections and skybox lighting.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Reflection Size**        | Use the drop-down to select the maximum resolution of the cube map HDRP uses to manage fallback reflection when no local reflection probes are present. This property has no effect on the quality of the sky itself. |
| **Lighting Override Mask** | Use the drop-down to select the [Volume](Volumes.md) layer mask HDRP uses to override sky lighting. Use this to decouple the display sky and lighting. See the [Environment Lighting](Environment-Lighting.md#DecoupleVisualEnvironment) for information on how to decouple environment lighting from the sky background. |

### Shadow

These settings adjust the size of the shadowmask. Smaller values causes Unity to discard more distant shadows, while higher values lead to Unity displaying more shadows at longer distances from the Camera.

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Shadowmask**                  | Enable the checkbox to make HDRP support the [Shadowmask lighting mode](Lighting-Mode-Shadowmask.md) in your Unity Project. |
| **Maximum** **Shadow on Screen** | The maximum number of shadows you can have in view. A Spot Light casts a single shadow, a Point Light casts six shadows, and a Directional Light casts shadows equal to the number of cascades defined in the [HD Shadow Settings](Override-Shadows.md) override. |
| **Shadow Filtering Quality**     | Use the drop-down to select the filtering quality for shadows. Higher values increase the shadow quality in HDRP as better filtering near the edges of shadows reduce aliasing effects. For information on each filtering quality preset, see [Filtering Quality](#filtering-quality). |
| **Area Shadow Filtering Quality**| Use the drop-down to select the filtering quality for area shadows. Higher values increase the area shadow quality in HDRP as better filtering improves the shape of the penumbra of very soft shadows and reduces light leaking. For information on each area filtering quality preset, see [Filtering Quality](#filtering-quality). |
| **Screen Space Shadows**         | Enable the checkbox to allow HDRP to compute shadows in a separate pass and store them in a screen-aligned Texture. |
| - **Maximum**                    | Set the maximum number of screen space shadows that HDRP can handle. |
| - **Buffer Format**              | Defines the format (R11G11B10 or R16G16B16A16) of the buffer used for screen space shadows.|

<a name="ShadowMapSettings"></a>

The following sections allow you to customize the shadow atlases and individual shadow resolution tiers for each type of Light in HDRP. Shadow resolution tiers are useful because, instead of defining the shadow resolution for each individual Light as a number, you can assign a numbered resolution to a named shadow resolution tier then use the named tier instead of rewriting the number. For example, instead of setting the resolution of each Light to 512, you could say that **Medium** resolution shadows have a resolution of 512 and then set the shadow quality of each Light to be **Medium**. This way, you can more easily have consistent shadow quality across your HDRP Project.

The three sections here are:

- **Directional Light Shadows**
- **Punctual Light Shadows**
- **Area Light Shadows**

They all share the same properties, except **Directional Light Shadows** which does not include **Resolution** or **Dynamic Rescale** and **Cached Shadow Atlas Resolution**.

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| ***Light Atlas***   |                                                              |
| **Resolution**      | Use the drop-down to select the resolution of the shadow atlas. |
| **Precision**       | Use the drop-down to select the precision of the shadow map. This sets the bit depth of each pixel of the shadow map. **16 bit** is faster and uses less memory at the expense of precision. |
| **Dynamic Rescale** | Enable the checkbox to allow HDRP to rescale the shadow atlas if all the shadows on the screen don't currently fit onto it. |

| ***Shadow Resolution Tiers***      |                                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **L**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Low** use this resolution for their shadows. |
| **M**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Medium** use this resolution for their shadows. |
| **H**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **High** use this resolution for their shadows. |
| **U**                              | Set the resolution of shadows set to this quality. Light's with their **Resolution** set to **Ultra** use this resolution for their shadows. |
| **Maximum Shadow Resolution**      | Set the maximum resolution of any shadow map of this Light type. If you set any shadow resolution to a value higher than this, HDRP clamps it to this value. |
| **Cached Shadow Atlas Resolution** | Use the drop-down to select the resolution of the shadow atlas used for cached shadows (Update mode set to OnEnable or OnDemand). |

#### Filtering Quality

| **Shadow Filtering Quality** | **Algorithm**                                                |
| ---------------------------- | ------------------------------------------------------------ |
| **Low**                      | &#8226; **Point/Spot Lights**: Percentage Closer Filtering (PCF) 3x3 (4 taps).<br />&#8226; **Directional Lights**: PCF Tent 5x5 (9 taps). |
| **Medium**                   | &#8226; **Point/Spot Lights**: PCF 5x5 (9 taps).<br />&#8226; **Directional Lights**: PCF Tent 5x5 (9 taps). |
| **High**                     | &#8226;**Point/Spot/Directional Lights**: Percentage Closer Soft Shadows (PCSS). |

| **Area Shadow Filtering Quality** | **Algorithm**                                                |
| --------------------------------- | ------------------------------------------------------------ |
| **Medium**                        | &#8226; **Area Lights**: EVSM. |
| **High**                          | &#8226; **Area Lights**: Percentage Closer Soft Shadows (PCSS). |

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

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Distortion**                  | Enable the checkbox to make HDRP support distortion. If your Unity Project does not use distortion, disable this checkbox to reduce build time. |
| **Subsurface Scattering**       | Enable the checkbox to make HDRP support subsurface scattering (SSS). SSS describes light penetration of the surface of a translucent object |
| **- High Quality**             | Enable the checkbox to increase the SSS Sample Count and enable high quality subsurface scattering. Increasing the sample count greatly increases the performance cost of the Subsurface Scattering effect. |
| **Fabric BSDF Convolution** | By default, Fabric Materials reuse the Reflection Probes that HDRP calculates for the Lit Shader (GGX BRDF). Enable the checkbox to make HDRP calculate another version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. This increases the resource intensity because HDRP must condition two Reflection Probes instead of one. It also reduces the number of visible Reflection Probes in the current view by half because the size of the cache that stores Reflection Probe data does not change and must now store both versions of each Reflection Probe. |

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
| **Preload Textures Per Frame** | The number of textures Unity tries to preload their least detailed mipmap levels (128x128 pixels) into GPU memory per frame. Use this to avoid texture pop-in. The range is 0 through 1024. The default is 0, which disables preloading. |
| **Preload Mip Count** | The number of mipmap levels to preload. The range is 1 through 9. The default is 1, which preloads only the highest mipmap level with the smallest size (128x128 pixels, the size of a Streaming Virtual Texturing tile). |
