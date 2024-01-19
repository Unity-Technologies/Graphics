# Universal Render Pipeline Asset

Any Unity project that uses the Universal Render Pipeline (URP) must have a URP Asset to configure the settings. When you create a project using the URP template, Unity creates the URP Assets in the **Settings** project folder and assigns them in Project Settings. If you are migrating an existing project to URP, you need to [create a URP Asset and assign the asset in the Graphics settings](InstallURPIntoAProject.md).

The URP Asset controls several graphical features and quality settings for the Universal Render Pipeline.  It is a scriptable object that inherits from ‘RenderPipelineAsset’. When you assign the asset in the Graphics settings, Unity switches from the built-in render pipeline to the URP. You can then adjust the corresponding settings directly in the URP, instead of looking for them elsewhere.

You can have multiple URP assets and switch between them. For example, you can have one with Shadows on and one with Shadows off. If you switch between the assets to see the effects, you don’t have to manually toggle the corresponding settings for shadows every time. You cannot, however, switch between HDRP/SRP and URP assets, as the
 render pipelines are incompatible.


## UI overview

In the URP, you can configure settings for:

- [**Rendering**](#rendering)
- [**Quality**](#quality)
- [**Lighting**](#lighting)
- [**Shadows**](#shadows)
- [**Post-processing**](#post-processing)
- [**Adaptive Performance**](#adaptive-performance)


**Note:** If you have the experimental 2D Renderer enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), some of the options related to 3D rendering in the URP Asset don't have any impact on your final app or game.

### How to show Additional Properties

Unity does not show certain advanced properties in the URP Asset by default. To see all available properties:

* In the URP Asset, in any section, click the vertical ellipsis icon (&vellip;) and select **Show Additional Properties**

    ![Show Additional Properties](Images/settings-general/show-additional-properties.png)

    Unity shows all available properties in the current section.

To show all additional properties in all sections:

1. Click the vertical ellipsis icon (&vellip;) and select **Show All Additional Properties**. Unity opens the **Core Render Pipeline** section in the **Preferences** window.

2. In the property **Additional Properties > Visibility**, select **All Visible**.

    ![Additional Properties > Visibility > All Visible](Images/settings-general/show-all-additional-properties.png)


### Rendering

The **Rendering** settings control the core part of the pipeline rendered frame.

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Depth Texture**       | Enables URP to create a `_CameraDepthTexture`. URP then uses this [depth texture](https://docs.unity3d.com/Manual/SL-DepthTextures.html) by default for all Cameras in your scene. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| **Opaque Texture**      | Enable this to create a `_CameraOpaqueTexture` as default for all cameras in your scene. This works like the [GrabPass](https://docs.unity3d.com/Manual/SL-GrabPass.html) in the built-in render pipeline. The **Opaque Texture** provides a snapshot of the scene right before URP renders any transparent meshes. You can use this in transparent Shaders to create effects like frosted glass, water refraction, or heat waves. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| **Opaque Downsampling** | Set the sampling mode on the opaque texture to one of the following:<br/>**None**:  Produces a copy of the opaque pass in the same resolution as the camera.<br/>**2x Bilinear**: Produces a half-resolution image with bilinear filtering.<br/>**4x Box**: Produces a quarter-resolution image with box filtering. This produces a softly blurred copy.<br/>**4x Bilinear**: Produces a quarter-resolution image with bi-linear filtering. |
| **Terrain Holes**       | If you disable this option, the URP removes all Terrain hole Shader variants when you build for the Unity Player, which decreases build time. |
| **SRP Batcher**            | Check this box to enable the SRP Batcher. This is useful if you have many different Materials that use the same Shader. The SRP Batcher is an inner loop that speeds up CPU rendering without affecting GPU performance. When you use the SRP Batcher, it replaces the SRP rendering code inner loop. If both **SRP Batcher** and **Dynamic Batching** are enabled, SRP Batcher will take precedence over dynamic batching as long as the shader is SRP Batcher compatible.<br/><br/>**Note**: If assets or shaders in a project are not optimized for use with the SRP Batcher, low performance devices might be more performant when you disable the SRP Batcher. |
| **Dynamic Batching**       | Enable [Dynamic Batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), to make the render pipeline automatically batch small dynamic objects that share the same Material. This is useful for platforms and graphics APIs that do not support GPU instancing. If your targeted hardware does support GPU instancing, disable **Dynamic Batching**. You can change this at run time. |
| **Debug Level**            | Set the level of debug information that the render pipeline generates. The values are:<br />**Disabled**:  Debugging is disabled. This is the default.<br  />**Profiling**: Makes the render pipeline provide detailed information tags, which you can find in the FrameDebugger. |
| **Shader Variant Log Level** | Set the level of information about Shader Stripping and Shader Variants you want to display when Unity finishes a build. Values are:<br /> **Disabled**: Unity doesn’t log anything.<br />**Only Universal**: Unity logs information for all of the [URP Shaders](shaders-in-universalrp.md).<br />**All**: Unity logs information for all Shaders in your build.<br /> You can check the information in Console panel when your build has finished. |
| **Store Actions**          | Defines if Unity discards or stores the render targets of the DrawObjects Passes. Selecting the **Store** option significantly increases the memory bandwidth on mobile and tile-based GPUs.<br/>**Auto**: Unity uses the **Discard** option by default, and falls back to the **Store** option if it detects any injected Passes.<br/>**Discard**:  Unity discards the render targets of render Passes that are not reused later (lower memory bandwidth).<br/>**Store**: Unity stores all render targets of each Pass (higher memory bandwidth). |

### Quality

These settings control the quality level of the URP. This is where you can make performance better on lower-end hardware or make graphics look better on  higher-end hardware.

**Tip:** If you want to have different settings for different hardware, you can configure these settings across multiple Universal Render Pipeline assets, and switch them out as needed.

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| **HDR**          | Enable this to allow rendering in High Dynamic Range (HDR) by default for every camera in your Scene. With HDR, the brightest part of the image can be greater than 1. This gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light. This is useful if you want a wide range of lighting or to use [bloom](https://docs.unity3d.com/Manual/PostProcessing-Bloom.html) effects. If you’re targeting lower-end hardware, you can disable this to skip HDR calculations and get better performance. You can override this for individual cameras in the Camera Inspector. |
| &#160;&#160;&#160;&#160;HDR Precision | The precision of the Camera color buffer in HDR rendering. The 64 bit precision lets you avoid banding artifacts, but requires higher bandwidth and might make sampling slower. Default value: 32 bit. |
| **Anti Aliasing (MSAA)** | Use [Multisample Anti-aliasing](anti-aliasing.md#msaa) by default for every Camera in your Scene while rendering. This softens edges of your geometry, so they’re not jagged or flickering. In the drop-down menu, select how many samples to use per pixel: **2x**, **4x**, or **8x**. The more samples you choose, the smoother your object edges are. If you want to skip MSAA calculations, or you don’t need them in a 2D game, select **Disabled**. You can override this for individual cameras in the Camera Inspector.<br/>**Note:** On mobile platforms that do not support the [StoreAndResolve](https://docs.unity3d.com/ScriptReference/Rendering.RenderBufferStoreAction.StoreAndResolve.html) store action, if **Opaque Texture** is selected in the URP asset, Unity ignores the **Anti Aliasing (MSAA)** property at runtime (as if Anti Aliasing (MSAA) is set to Disabled). |
| **Render Scale** | This slider scales the render target resolution (not the resolution of your current device). Use this when you want to render at a smaller resolution for performance reasons or to upscale rendering to improve quality.  This only scales the game rendering. UI rendering is left at the native resolution for the device. |
| **Upscaling Filter** | Select which image filter Unity uses when performing the upscaling. Unity performs upscaling when the Render Scale value is less than 1.0. |
| &#160;&#160;&#160;&#160;**Automatic** | Unity selects one of the filtering options based on the Render Scale value and the current screen resolution. If integer scaling is possible, Unity selects the Nearest-Neighbor option, otherwise Unity selects the Bilinear option. |
| &#160;&#160;&#160;&#160;**Bilinear** | Unity uses the bilinear or linear filtering provided by the graphics API. |
| &#160;&#160;&#160;&#160;**Nearest-Neighbor** | Unity uses the nearest-neighbor or point sampling filtering provided by the graphics API. |
| &#160;&#160;&#160;&#160;**FidelityFX Super Resolution&#160;1.0** | Unity uses the AMD FidelityFX Super Resolution 1.0 (FSR) technique to perform upscaling.<br/>Unlike the other Upscaling Filter options, this filter remains active even at a Render Scale value of 1.0. This filter can still improve image quality even when no scaling is occurring. This also makes the transition between scale values 0.99 and 1.0 less noticeable in cases where dynamic resolution scaling is active.<br/>**Note**: This filter is only supported on devices that support Unity shader model 4.5 or higher. On devices that do not support Unity shader model 4.5, Unity uses the **Automatic** option instead. |
| &#160;&#160;&#160;&#160;**Override FSR Sharpness** | Unity shows this check box when you select the FSR filter. Selecting this check box lets you specify the intensity of the FSR sharpening pass. |
| &#160;&#160;&#160;&#160;**FSR Sharpness** | Specify the intensity of the FSR sharpening pass. A value of 0.0 provides no sharpening, a value of 1.0 provides maximum sharpness. This option has no effect when FSR is not the active upscaling filter. |
| **<a name="lod-cross-fade"></a>LOD&#160;Cross&#160;Fade**       | Use this property to enable or disable the LOD cross-fade. If you disable this option, URP removes all LOD cross-fade shader variants when you build the Unity Player, which decreases the build time . If you target a low-end mobile platform, disable this setting so URP doesn't use alpha testing to fade level of detail (LOD) meshes in and out, which reduces processing time on the GPU. |
| **LOD Cross Fade Dithering Type** | When an [LOD group](https://docs.unity3d.com/Manual/class-LODGroup.html) has **Fade Mode** set to **Cross Fade**, Unity renders the Renderer's LOD meshes with cross-fade blending between them using alpha testing. This property defines the type of LOD cross-fade.<br/>Options:<br/>**Bayer Matrix**: better performance than the Blue Noise option, but has a repetitive pattern.<br/>**Blue Noise**: uses a precomputed blue noise texture and provides a better look than the Bayer Matrix option, but has a slightly higher performance cost. |

### Lighting

These settings affect the lights in your Scene.

If you disable some of these settings, the relevant [keywords](https://docs.unity3d.com/Manual/shader-keywords) are [stripped from the Shader variables](shader-stripping.md). If there are settings that you know for certain you won’t use in your game or app, you can disable them to improve performance and reduce build time.

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| **Main Light**        | These settings affect the main [Directional Light](https://docs.unity3d.com/Manual/Lighting.html) in your Scene. You can select this by assigning it as a [Sun Source](https://docs.unity3d.com/Manual/GlobalIllumination.html) in the Lighting Inspector. If you don’t assign a sun source, the URP treats the brightest directional light in the Scene as the main light. You can choose between [Pixel Lighting](https://docs.unity3d.com/Manual/LightPerformance.html) and _None_. If you choose None, URP doesn’t render a main light,  even if you’ve set a sun source. |
| **Cast Shadows**      | Check this box to make the main light cast shadows in your Scene. On lower-end platforms, you can disable this setting to reduce how much memory URP uses, and reduce processing time on the CPU and the GPU. |
| **Shadow Resolution** | This controls how large the shadow map texture for the main light is. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |
| **Mixed Lighting**    | When [Mixed Lighting](https://docs.unity3d.com/Manual/LightMode-Mixed.html) is enabled, Unity includes mixed lighting shader variants in the build.|
| **Use Rendering Layers** | With this option selected, you can configure certain Lights to affect only specific GameObjects. For more information on Rendering Layers and how to use them, see the page [Rendering Layers](features/rendering-layers.md). If you enable this setting and you use the Deferred rendering path, URP uses more memory. |
| **Light Cookies** | When enabled, URP includes light cookie shader variants in the build. If you enable this setting, URP uses more memory. Refer to [Light component reference](light-component.md) for more information. |
| **Additional Lights** | Choose whether to add additional lights that supplement your main light. Choose between [Per Vertex](https://docs.unity3d.com/Manual/LightPerformance.html), [Per Pixel](https://docs.unity3d.com/Manual/LightPerformance.html), or **Disabled**. To reduce processing time on the GPU, set to **Disabled**, or **Per Vertex** if you use the Forward or Forward+ rendering path. |
| **Per Object Limit**  | This slider sets the limit for how many additional lights can affect each GameObject. |
| **Cast Shadows**      | Check this box to make the additional lights cast shadows in your Scene. |
| **Shadow Atlas Resolution** | Control the size of the textures that cast directional shadows for the additional lights. This is a sprite atlas that packs up to 16 shadow maps. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |
| **Mixed Lighting**         | Enable [Mixed Lighting](https://docs.unity3d.com/Manual/LightMode-Mixed.html) to configure the pipeline to include mixed lighting shader variants in the build. |

### Shadows

These settings let you configure how shadows look and behave, and find a good balance between the visual quality and performance.

![Shadows](Images/lighting/urp-asset-shadows.png)

The **Shadows** section has the following properties.

| Property         | Description |
| ---------------- | ----------- |
| **Max Distance** | The maximum distance from the Camera at which Unity renders the shadows. Unity does not render shadows farther than this distance. Increasing the distance reduces the performance. <br/>**Note:** This property is in metric units regardless of the value in the **Working Unit** property. |
| **Working Unit** | The unit in which Unity measures the shadow cascade distances. |
| **Cascade Count** | The number of [shadow cascades](https://docs.unity3d.com/Manual/shadow-cascades.html). With shadow cascades, you can avoid crude shadows close to the Camera and keep the Shadow Resolution reasonably low. For more information, see the page [Shadow Cascades](https://docs.unity3d.com/Manual/shadow-cascades.html). Increasing the number of cascades reduces the performance. Cascade settings only affects the main light. |
| &#160;&#160;&#160;&#160;Split&#160;1 | The distance where cascade 1 ends and cascade 2 starts. |
| &#160;&#160;&#160;&#160;Split&#160;2 | The distance where cascade 2 ends and cascade 3 starts. |
| &#160;&#160;&#160;&#160;Split&#160;3 | The distance where cascade 3 ends and cascade 4 starts. |
| &#160;&#160;&#160;&#160;Last&#160;Border | The size of the area where Unity fades out the shadows. Unity starts fading out shadows at the distance **Max Distance**&#160;-&#160;**Last Border**, at **Max Distance** the shadows fade to zero. |
| **Depth Bias** | Use this setting to reduce [shadow acne](https://docs.unity3d.com/Manual/ShadowPerformance.html). |
| **Normal Bias** | Use this setting to reduce [shadow acne](https://docs.unity3d.com/Manual/ShadowPerformance.html). |
| <a name="soft-shadows"></a>**Soft Shadows** | Select this check box to enable extra processing of the shadow maps to give them a smoother look.<br/>**Performance impact**: high.<br/>When this option is disabled, Unity samples the shadow map once with the default hardware filtering, which reduces processing time on the GPU. |
| &#160;&#160;&#160;&#160;Quality | Select the quality level of soft shadow processing.<br/>Available options:<br/>**Low**: good balance of quality and performance for mobile platforms. Filtering method: 4 PCF taps.<br/>**Medium**: good balance of quality and performance for desktop platforms. Filtering method: 5x5 tent filter. This is the default value.<br/>**High**: best quality, higher performance impact. Filtering method: 7x7 tent filter. |
| **Conservative Enclosing Sphere** | Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades.<br/>Disable this option only for compatibility purposes of existing projects created in previous Unity versions.<br/>If you enable this option in an existing project, you might need to adjust the shadows cascade distances because the shadow culling enclosing spheres change their size and position.<br/>**Performance impact**: enabling this option is likely to improve performance, because the option minimizes the overlap of shadow cascades, which reduces the number of redundant static shadow casters. |

### Post-processing

This section allows you to fine-tune global post-processing settings.

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| **Post Processing** | This check box turns post-processing on (check box selected) or off (check box cleared) for the current URP asset.<br/>If you clear this check box, Unity excludes post-processing shaders and textures from the build, unless one of the following conditions is true:<ul><li>Other assets in the build refer to the assets related to post-processing.</li><li>A different URP asset has the Post Processing property enabled.</li></ul> |
| **Post Process Data** | The asset containing references to shaders and Textures that the Renderer uses for post-processing.<br/>**Note:** Changes to this property are necessary only for advanced customization use cases. |
| **Grading Mode** | Select the [color grading](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) mode to use for the Project.<ul><li>**High Dynamic Range**: This mode works best for high precision grading similar to movie production workflows. Unity applies color grading before tonemapping.</li><li>**Low Dynamic Range**: This mode follows a more classic workflow. Unity applies a limited range of color grading after tonemapping.</li></ul> |
| **LUT Size**     | Set the size of the internal and external [look-up textures (LUTs)](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) that the Universal Render Pipeline uses for color grading. Higher sizes provide more precision, but have a potential cost of performance and memory use. You cannot mix and match LUT sizes, so decide on a size before you start the color grading process.<br />The default value, **32**, provides a good balance of speed and quality. |
| **Fast sRGB/Linear Conversions** | Select this option to use faster, but less accurate approximation functions when converting between the sRGB and Linear color spaces.|
| **Volume Update Mode** | Select how Unity updates Volumes: every frame or when triggered via scripting. If you select **Every Frame**, URP requires more processing time on the CPU. In the Editor, Unity updates Volumes every frame when not in the Play mode. |

### Adaptive Performance

This section is available if the Adaptive Performance package is installed in the project. The **Use Adaptive Performance** property lets you enable the Adaptive Performance functionality.

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Use Adaptive Performance**  | Select this check box to enable the Adaptive Performance functionality, which adjusts the rendering quality at runtime. |
