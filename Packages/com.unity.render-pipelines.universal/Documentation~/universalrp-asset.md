# Universal Render Pipeline Asset

Any Unity project that uses the Universal Render Pipeline (URP) must have a URP Asset to configure the settings. When you create a project using the URP template, Unity creates the URP Assets in the **Settings** project folder and assigns them in Project Settings. If you are migrating an existing project to URP, you need to [create a URP Asset and assign the asset in the Graphics settings](InstallURPIntoAProject.md).

The URP Asset controls several graphical features and quality settings for the Universal Render Pipeline.  It is a scriptable object that inherits from ‘RenderPipelineAsset’. When you assign the asset in the Graphics settings, Unity switches from the built-in render pipeline to the URP. You can then adjust the corresponding settings directly in the URP, instead of looking for them elsewhere.

You can have multiple URP assets and switch between them. For example, you can have one with Shadows on and one with Shadows off. If you switch between the assets to understand the effects, you don’t have to manually toggle the corresponding settings for shadows every time. You cannot, however, switch between HDRP/SRP and URP assets, as the render pipelines are incompatible.

## UI overview

In the URP, you can configure settings for:

* [**Rendering**](#rendering)
* [**Quality**](#quality)
* [**Lighting**](#lighting)
* [**Shadows**](#shadows)
* [**Post-processing**](#post-processing)
* [**Volumes**](#volumes)
* [**Adaptive Performance**](#adaptive-performance)

> [!NOTE]
> If you have the experimental 2D Renderer enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), some of the options related to 3D rendering in the URP Asset don't have any impact on your final app or game.

### How to show Additional Properties

Unity does not show certain advanced properties in the URP Asset by default. To reveal all available properties:

* In the URP Asset, in any section, click the vertical ellipsis icon (&vellip;) and select **Show Additional Properties**

    ![Show Additional Properties](Images/settings-general/show-additional-properties.png)

    Unity shows all available properties in the current section.

To show all additional properties in all sections:

1. Click the vertical ellipsis icon (&vellip;) and select **Show All Additional Properties**. Unity opens the **Core Render Pipeline** section in the **Preferences** window.

2. In the property **Additional Properties > Visibility**, select **All Visible**.

    ![Additional Properties > Visibility > All Visible](Images/settings-general/show-all-additional-properties.png)

### Rendering

The **Rendering** settings control the core part of the pipeline rendered frame.

| Property | Description |
| -------- | ----------- |
| **Depth Texture** | Enables URP to create a `_CameraDepthTexture`. URP then uses this [depth texture](https://docs.unity3d.com/Manual/SL-DepthTextures.html) by default for all Cameras in your scene. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| **Opaque Texture** | Enable this to create a `_CameraOpaqueTexture` as default for all cameras in your scene. This works like the [GrabPass](https://docs.unity3d.com/Manual/SL-GrabPass.html) in the built-in render pipeline.<br/><br/>The **Opaque Texture** provides a snapshot of the scene right before URP renders any transparent meshes. You can use this in transparent Shaders to create effects like frosted glass, water refraction, or heat waves. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| **Opaque Downsampling** | Set the sampling mode on the opaque texture to one of the following:<ul><li>**None**: Produces a copy of the opaque pass in the same resolution as the camera.</li><li>**2x Bilinear**: Produces a half-resolution image with bilinear filtering.</li><li>**4x Box**: Produces a quarter-resolution image with box filtering. This produces a softly blurred copy.</li><li>**4x Bilinear**: Produces a quarter-resolution image with bi-linear filtering.</li></ul> |
| **Terrain Holes** | If you disable this option, the URP removes all Terrain hole Shader variants when you build for the Unity Player, which decreases build time. |
| **GPU Resident Drawer** | The GPU Resident Drawer automatically uses the [`BatchRendererGroup`](https://docs.unity3d.com/Manual/batch-renderer-group.html) API to draw GameObjects with GPU instancing. For more information, refer to [Use the GPU Resident Drawer](gpu-resident-drawer.md).<br/><br/>Available options:<ul><li>**Disabled**: Unity doesn't automatically draw GameObjects with GPU instancing.</li><li>**Instanced Drawing**: Unity automatically draws GameObjects with GPU instancing.</li></ul> |
| &#160;&#160;&#160;&#160;**Small-Mesh Screen-Percentage** | Set the screen percentage Unity uses to cull small GameObjects, to speed up rendering. Unity culls GameObjects that fill less of the screen than this value.<br/><br/>This setting might not work if you use your own [Level of Detail (LOD) meshes](https://docs.unity3d.com/Manual/LevelOfDetail.html).<br/><br/>Set the value to 0 to stop Unity culling small GameObjects.<br/><br/>To prevent Unity culling an individual GameObject that covers less screen space than this value, go to the **Inspector** window for the GameObject and add a **Disallow Small Mesh Culling** component. |
| &#160;&#160;&#160;&#160;**GPU Occlusion Culling** | Enable Unity to use the GPU instead of the CPU to exclude GameObjects from rendering when they're hidden behind other GameObjects. Refer to [Use GPU occlusion culling](gpu-culling.md) for more information. |
| **SRP Batcher** | Enable the SRP Batcher. This is useful if you have many different Materials that use the same Shader. The SRP Batcher is an inner loop that speeds up CPU rendering without affecting GPU performance. When you use the SRP Batcher, it replaces the SRP rendering code inner loop.<br/><br/>If both **SRP Batcher** and **Dynamic Batching** are enabled, SRP Batcher will take precedence over dynamic batching as long as the shader is SRP Batcher compatible.<br/><br/>**Note**: If assets or shaders in a project are not optimized for use with the SRP Batcher, low performance devices might be more performant when you disable the SRP Batcher. |
| **Dynamic Batching** | Enable [Dynamic Batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), to make the render pipeline automatically batch small dynamic objects that share the same Material. This is useful for platforms and graphics APIs that do not support GPU instancing.<br/><br/>If your targeted hardware does support GPU instancing, disable **Dynamic Batching**. You can change this at run time. |
| **Debug Level** | Set the level of debug information that the render pipeline generates.<br/><br/>Available options:<ul><li>**Disabled**: Debugging is disabled. This is the default.</li><li>**Profiling**: Makes the render pipeline provide detailed information tags, which you can find in the FrameDebugger.</li></ul> |
| **Shader Variant Log Level** | Set the level of information about Shader Stripping and Shader Variants you want to display when Unity finishes a build.<br/><br/>Available options:<ul><li>**Disabled**: Unity doesn’t log anything.</li><li>**Only Universal**: Unity logs information for all of the [URP Shaders](shaders-in-universalrp.md).</li><li>**All**: Unity logs information for all Shaders in your build.</li></ul><br/>You can check the information in Console panel when your build has finished. |
| **Store Actions** | Defines if Unity discards or stores the render targets of the DrawObjects Passes.<br/><br/>Available options:<ul><li>**Auto**: Unity uses the **Discard** option by default, and falls back to the **Store** option if it detects any injected Passes.</li><li>**Discard**:  Unity discards the render targets of render Passes that are not reused later (lower memory bandwidth).</li><li>**Store**: Unity stores all render targets of each Pass. **Store** significantly increases the memory bandwidth on mobile and tile-based GPUs.</li></ul> |

### Quality

These settings control the quality level of the URP. This is where you can make performance better on lower-end hardware or make graphics look better on  higher-end hardware.

**Tip:** If you want to have different settings for different hardware, you can configure these settings across multiple Universal Render Pipeline assets, and switch them out as needed.

| Property | Description |
| -------- | ----------- |
| **HDR** | Enable this to allow rendering in High Dynamic Range (HDR) by default for every camera in your scene. With HDR, the brightest part of the image can be greater than 1.<br/><br/>This gives you a wider range of light intensities, so your lighting looks more realistic, such as being able to pick out details and experience less saturation even with bright light. This is useful if you want a wide range of lighting or to use [bloom](https://docs.unity3d.com/Manual/PostProcessing-Bloom.html) effects.<br/><br/> If you’re targeting lower-end hardware, you can disable this to skip HDR calculations and get better performance. You can override this for individual cameras in the Camera Inspector. |
| &#160;&#160;&#160;&#160;**HDR Precision** | The precision of the Camera color buffer in HDR rendering. The 64 bit precision lets you avoid banding artifacts, but requires higher bandwidth and might make sampling slower. Default value: 32 bit. |
| **Anti Aliasing (MSAA)** | Use [Multisample Anti-aliasing](anti-aliasing.md#msaa) by default for every Camera in your scene while rendering. This softens edges of your geometry, so they’re not jagged or flickering. In the drop-down menu, select how many samples to use per pixel: **2x**, **4x**, or **8x**. The more samples you choose, the smoother your object edges are.<br/><br/>If you want to skip MSAA calculations, or you don’t need them in a 2D game, select **Disabled**. You can override this for individual cameras in the Camera Inspector.<br/><br/>**Note**: On mobile platforms that do not support the [StoreAndResolve](https://docs.unity3d.com/ScriptReference/Rendering.RenderBufferStoreAction.StoreAndResolve.html) store action, if **Opaque Texture** is selected in the URP asset, Unity ignores the **Anti Aliasing (MSAA)** property at runtime (as if Anti Aliasing (MSAA) is set to Disabled). |
| **Render Scale** | This slider scales the render target resolution (not the resolution of your current device). Use this when you want to render at a smaller resolution for performance reasons or to upscale rendering to improve quality.<br/><br/>**Note**: This only scales the game rendering. UI rendering is left at the native resolution for the device. |
| **Upscaling Filter** | Select which image filter Unity uses when performing the upscaling. Unity performs upscaling when the Render Scale value is less than 1.0. |
| &#160;&#160;&#160;&#160;**Automatic** | Unity selects one of the filtering options based on the Render Scale value and the current screen resolution. If integer scaling is possible, Unity selects the Nearest-Neighbor option, otherwise Unity selects the Bilinear option. |
| &#160;&#160;&#160;&#160;**Bilinear** | Unity uses the bilinear or linear filtering provided by the graphics API. |
| &#160;&#160;&#160;&#160;**Nearest-Neighbor** | Unity uses the nearest-neighbor or point sampling filtering provided by the graphics API. |
| &#160;&#160;&#160;&#160;**FidelityFX Super Resolution&#160;1.0** | Unity uses the AMD FidelityFX Super Resolution 1.0 (FSR) technique to perform upscaling.<br/><br/>Unlike most other Upscaling Filter options, this filter remains active even at a Render Scale value of 1.0. This filter can still improve image quality even when no scaling is occurring. This also makes the transition between scale values 0.99 and 1.0 less noticeable in cases where dynamic resolution scaling is active.<br/><br/>**Note**: This filter is only supported on devices that support Unity shader model 4.5 or higher. On devices that do not support Unity shader model 4.5, Unity uses the **Automatic** option instead. |
| &#160;&#160;&#160;&#160;**Override FSR Sharpness** | Unity shows this check box when you select the FSR filter. Selecting this check box lets you specify the intensity of the FSR sharpening pass. |
| &#160;&#160;&#160;&#160;**FSR Sharpness** | Specify the intensity of the FSR sharpening pass. A value of 0.0 provides no sharpening, a value of 1.0 provides maximum sharpness.<br/><br/>**Note**: This option has no effect when FSR is not the active upscaling filter. |
| &#160;&#160;&#160;&#160;**Spatial Temporal Post-Processing (STP)&#160;1.0** | Unity uses the Spatial Temporal Post-Processing (STP) technique to perform upscaling.<br/><br/>This filter performs temporal anti-aliasing as part of the upscaling process, so using it will override the camera's anti-aliasing method to temporal anti-aliasing. This filter is capable of improving image quality even without scaling, so it remains active at 1.0 Render Scale like FSR.<br/><br/>**Note**: This filter is only supported on non-GLES devices that are capable of running compute shaders. STP is supported on mobile devices, but its performance cost on lower-end hardware can make it impractical. On devices that do not support STP's requirements, Unity uses the **Automatic** option instead. |
| **LOD&#160;Cross&#160;Fade** | Use this property to enable or disable the LOD cross-fade. If you disable this option, URP removes all LOD cross-fade shader variants when you build the Unity Player, which decreases the build time. |
| **LOD Cross Fade Dithering&#160;Type** | When an [LOD group](https://docs.unity3d.com/Manual/class-LODGroup.html) has **Fade Mode** set to **Cross Fade**, Unity renders the Renderer's LOD meshes with cross-fade blending between them using alpha testing. This property defines the type of LOD cross-fade.<br/><br/>Available options:<ul><li>**Bayer Matrix**: better performance than the Blue Noise option, but has a repetitive pattern.</li><li>**Blue Noise**: uses a precomputed blue noise texture and provides a better look than the Bayer Matrix option, but has a slightly higher performance cost.</li></ul> |

### Lighting

These settings affect the lights in your scene.

If you disable some of these settings, the relevant [keywords](https://docs.unity3d.com/Manual/shader-keywords) are [stripped from the Shader variables](shader-stripping.md). If there are settings that you know for certain you won’t use in your game or app, you can disable them to improve performance and reduce build time.

| Property | Description |
| -------- | ----------- |
| **Main Light** | These settings affect the main [Directional Light](https://docs.unity3d.com/Manual/Lighting.html) in your scene. You can select this by assigning it as a [Sun Source](https://docs.unity3d.com/Manual/GlobalIllumination.html) in the Lighting Inspector. If you don’t assign a sun source, the URP treats the brightest directional light in the scene as the main light.<br/><br/>You can choose between [Pixel Lighting](https://docs.unity3d.com/Manual/LightPerformance.html) and **None**. If you choose None, URP doesn’t render a main light,  even if you’ve set a sun source. |
| &#160;&#160;&#160;&#160;**Cast Shadows** | Check this box to make the main light cast shadows in your scene. |
| &#160;&#160;&#160;&#160;**Shadow Resolution** | This controls how large the shadow map texture for the main light is. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |
| **Light Probe System** | Select the light probe system this URP Asset uses.<br/><br/>Available options:<ul><li>**Light Probe Groups (Legacy)**: Use the same [Light Probe Group system](https://docs.unity3d.com/Manual/class-LightProbeGroup.html) as the Built-In Render Pipeline.</li><li>**Adaptive Probe Volumes**: Use [Adaptive Probe Volumes](probevolumes.md).</li></ul> |
| **Memory Budget** | Limits the width and height of the textures that store baked Global Illumination data, which determines the amount of memory Unity sets aside to store baked Adaptive Probe Volume data. These textures have a fixed depth.<br/><br/>Available options:<ul><li>**Memory Budget Low**</li><li>**Memory Budget Medium**</li><li>**Memory Budget High**</li></ul> |
| **SH Bands** | Determines the [spherical harmonics (SH) bands](https://docs.unity3d.com/Manual/LightProbes-TechnicalInformation.html) Unity uses to store probe data. L2 provides more precise results, but uses more system resources.<br/><br/>Available options: <ul><li>**Spherical Harmonics L1**</li><li>**Spherical Harmonics L2**</li></ul> |
| **Enable Streaming** | Enable to stream Adaptive Probe Volume data from CPU memory to GPU memory at runtime. Refer to [Streaming Adaptive Probe Volumes](probevolumes-streaming.md) for more information. |
| **Estimated GPU Memory Cost** | Indicates the amount of texture data used by Adaptive Probe Volumes in your project. |
| **Additional Lights** | Here, you can choose to have additional lights to supplement your main light. Choose between [Per Vertex](https://docs.unity3d.com/Manual/LightPerformance.html), [Per Pixel](https://docs.unity3d.com/Manual/LightPerformance.html), or **Disabled**. |
| &#160;&#160;&#160;&#160;**Per Object Limit** | This slider sets the limit for how many additional lights can affect each GameObject. |
| &#160;&#160;&#160;&#160;**Cast Shadows** | Check this box to make the additional lights cast shadows in your scene. |
| &#160;&#160;&#160;&#160;**Shadow Atlas Resolution** | This controls the size of the textures that cast directional shadows for the additional lights.<br/><br/>This is a sprite atlas that packs up to 16 shadow maps. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |
| &#160;&#160;&#160;&#160;**Shadow Resolution Tiers** |  Set the resolution of the shadows cast by additional lights at various tiers.<br/><br/>Resolutions must have a value of 128 or greater, and are rounded to the next power of two.<br/><br/>**Note**: This property is only visible when the **Cast Shadows** property is enabled for Additional Lights. |
| &#160;&#160;&#160;&#160;**Cookie Atlas Resolution** | The size of the cookie atlas the additional lights use. All additional lights are packed into a single cookie atlas.<br/><br/>This property is only visible when the **Light Cookies** property is enabled. |
| &#160;&#160;&#160;&#160;**Cookie Atlas Format** | The format of the cookie atlas for additional lights. All additional lights are packed into a single cookie atlas.<br/><br/>Available options:<ul><li>**Grayscale Low**</li><li>**Grayscale High**</li><li>**Color Low**</li><li>**Color High**</li><li>**Color HDR**</li></ul>This property is only visible when the **Light Cookies** property is enabled. |
| **Reflection Probes** | Use these properties to control reflection probe settings. |
| &#160;&#160;&#160;&#160;**Probe Blending** | Smooth the transitions between Reflection Probes. For more information, refer to [Reflection Probe Blending](lighting/reflection-probes.md#reflection-probe-blending). |
| &#160;&#160;&#160;&#160;**Box Projection** | Create reflections on objects based on their position within the probe's box, while still using a single probe as the reflection source. For more information, refer to [Advanced Reflection Probe features](xref:AdvancedRefProbe). |
| **Mixed Lighting** | Enable [Mixed Lighting](https://docs.unity3d.com/Manual/LightMode-Mixed.html) to configure the pipeline to include mixed lighting shader variants in the build. |
| **Use Rendering Layers** | With this option selected, you can configure certain Lights to affect only specific GameObjects. For more information on Rendering Layers and how to use them, refer to the documentation on [Rendering Layers](features/rendering-layers.md). |
| **Light Cookies** | Enables [light cookies](https://docs.unity3d.com/Manual/Cookies.html). This property enables **Cookie Atlas Resolution** and **Cookie Atlas Format** for additional lights.  |
| **SH Evaluation Mode** | Defines the spherical harmonic (SH) lighting evaluation type.<br/><br/>Available options:<ul><li>**Auto**: Unity selects a mode automatically.</li><li>**Per Vertex**: Evaluate lighting per vertex.</li><li>**Mixed**: Evaluate lighting partially per vertex, partially per pixel.</li><li>**Per Pixel**: Evaluate lighting per pixel.</li></ul> |

### Shadows

These settings let you configure how shadows look and behave, and find a good balance between the visual quality and performance.

![Shadows](Images/lighting/urp-asset-shadows.png)

The **Shadows** section has the following properties.

| Property | Description |
| -------- | ----------- |
| **Max Distance** | The maximum distance from the Camera at which Unity renders the shadows. Unity does not render shadows farther than this distance.<br/><br/>**Note**: This property is in metric units regardless of the value in the **Working Unit** property. |
| **Working Unit** | The unit in which Unity measures the shadow cascade distances. |
| **Cascade Count** | The number of [shadow cascades](https://docs.unity3d.com/Manual/shadow-cascades.html). With shadow cascades, you can avoid crude shadows close to the Camera and keep the Shadow Resolution reasonably low.<br/><br/>For more information, refer to the documentation on [Shadow Cascades](https://docs.unity3d.com/Manual/shadow-cascades.html). Increasing the number of cascades reduces the performance. Cascade settings only affects the main light. |
| &#160;&#160;&#160;&#160;**Split**&#160;**1** | The distance where cascade 1 ends and cascade 2 starts. |
| &#160;&#160;&#160;&#160;**Split**&#160;**2** | The distance where cascade 2 ends and cascade 3 starts. |
| &#160;&#160;&#160;&#160;**Split**&#160;**3** | The distance where cascade 3 ends and cascade 4 starts. |
| &#160;&#160;&#160;&#160;**Last**&#160;**Border** | The size of the area where Unity fades out the shadows. Unity starts fading out shadows at the distance **Max Distance**&#160;-&#160;**Last Border**, at **Max Distance** the shadows fade to zero. |
| **Depth Bias** | Use this setting to reduce [shadow acne](https://docs.unity3d.com/Manual/ShadowPerformance.html). |
| **Normal Bias** | Use this setting to reduce [shadow acne](https://docs.unity3d.com/Manual/ShadowPerformance.html). |
| <a name="soft-shadows"></a>**Soft Shadows** | Select this check box to enable extra processing of the shadow maps to give them a smoother look.<br/>**Performance impact**: High.<br/>When this option is disabled, Unity samples the shadow map once with the default hardware filtering. |
| &#160;&#160;&#160;&#160;**Quality** | Select the quality level of soft shadow processing.<br/><br/>Available options:<ul><li>**Low**: good balance of quality and performance for mobile platforms. Filtering method: 4 PCF taps.</li><li>**Medium**: good balance of quality and performance for desktop platforms. Filtering method: 5x5 tent filter. This is the default value.</li><li>**High**: best quality, higher performance impact. Filtering method: 7x7 tent filter.</li></ul> |
| **Conservative Enclosing Sphere** | Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades.<br/><br/>Disable this option only for compatibility purposes of existing projects created in previous Unity versions.<br/><br/>If you enable this option in an existing project, you might need to adjust the shadows cascade distances because the shadow culling enclosing spheres change their size and position.<br/><br/>**Performance impact**: Enabling this option is likely to improve performance, because the option minimizes the overlap of shadow cascades, which reduces the number of redundant static shadow casters. |

### Post-processing

This section allows you to fine-tune global post-processing settings.

| Property | Description |
| -------- | ----------- |
| **Grading Mode** | Select the [color grading](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) mode to use for the Project.<ul><li>**High Dynamic Range**: This mode works best for high precision grading similar to movie production workflows. Unity applies color grading before tonemapping.</li><li>**Low Dynamic Range**: This mode follows a more classic workflow. Unity applies a limited range of color grading after tonemapping.</li></ul> |
| **LUT Size** | Set the size of the internal and external [look-up textures (LUTs)](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) that the Universal Render Pipeline uses for color grading. Higher sizes provide more precision, but have a potential cost of performance and memory use. You cannot mix and match LUT sizes, so decide on a size before you start the color grading process.<br/><br/>The default value, **32**, provides a good balance of speed and quality. |
| **Fast sRGB/Linear Conversions** | Select this option to use faster, but less accurate approximation functions when converting between the sRGB and Linear color spaces.|
| **Data Driven Lens Flare** | Allocate the shader variants and memory URP needs for [lens flares](shared/lens-flare/lens-flare-srp-reference.md) effect. |
| **Screen Space Lens Flare** | Allocate the shader variants and memory URP needs for [screen space lens flares](shared/lens-flare/reference-screen-space-lens-flare.md). |

### Volumes

| Property | Description |
| -------- | ----------- |
| **Volume Update Mode** | Select how Unity updates Volumes at run time. <ul><li>**Every Frame**: Unity updates volumes every frame.</li><li>**Via Scripting**: Unity updates volumes when triggered via scripting.</li></ul>In the Editor, Unity updates Volumes every frame when not in Play mode. |
| **Volume Profile** | Set the [Volume Profile](Volume-Profile.md) that a scene uses by default.<br/><br/>Refer to [Understand volumes](Volumes.md) for more information. |

The list of Volume Overrides that the Volume Profile contains appears below **Volume Profile**. You can add, remove, disable, and enable Volume Overrides, and edit their properties. Refer to [Volume Overrides](VolumeOverrides.md) for more information.

### Adaptive Performance

This section is available if the Adaptive Performance package is installed in the project. The **Use Adaptive Performance** property lets you enable the Adaptive Performance functionality.

| Property | Description |
| -------- | ----------- |
| **Use Adaptive Performance**  | Select this check box to enable the Adaptive Performance functionality, which adjusts the rendering quality at runtime. |
