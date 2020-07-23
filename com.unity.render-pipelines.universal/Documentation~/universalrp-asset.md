# Universal Render Pipeline Asset
To use the Universal Render Pipeline (URP), you have to [create a URP Asset and assign the asset in the Graphics settings](configuring-universalrp-for-use.md). 

The URP Asset controls several graphical features and quality settings for the Universal Render Pipeline.  It is a scriptable object that inherits from ‘RenderPipelineAsset’. When you assign the asset in the Graphics settings, Unity switches from the built-in render pipeline to the URP. You can then adjust the corresponding settings directly in the URP, instead of looking for them elsewhere.

You can have multiple URP assets and switch between them. For example, you can have one with Shadows on and one with Shadows off. If you switch between the assets to see the effects, you don’t have to manually toggle the corresponding settings for shadows every time. You cannot, however, switch between HDRP/SRP and URP assets, as the 
 render pipelines are incompatible.


## UI overview
In the URP, you can configure settings for:

- [__General__](#general)
- [__Quality__](#quality)
- [__Lighting__](#lighting)
- [__Shadows__](#shadows)
- [__Post-processing__](#post-processing)
- [__Advanced__](#advanced)
- [__Adaptive Performance__](#adaptive-performance)



**Note:** If you have the experimental 2D Renderer enabled (menu: **Graphics Settings** > add the 2D Renderer Asset under **Scriptable Render Pipeline Settings**), some of the options related to 3D rendering in the URP Asset don't have any impact on your final app or game.



### General
The __General__ settings control the core part of the pipeline rendered frame.

| __Property__            | __Description__                                              |
| ----------------------- | ------------------------------------------------------------ |
| __Depth Texture__       | Enables URP to create a `_CameraDepthTexture`. URP then uses this [depth texture](https://docs.unity3d.com/Manual/SL-DepthTextures.html) by default for all Cameras in your Scene. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| __Opaque Texture__      | Enable this to create a `_CameraOpaqueTexture` as default for all cameras in your Scene. This works like the [GrabPass](https://docs.unity3d.com/Manual/SL-GrabPass.html) in the built-in render pipeline. The __Opaque Texture__ provides a snapshot of the scene right before URP renders any transparent meshes. You can use this in transparent Shaders to create effects like frosted glass, water refraction, or heat waves. You can override this for individual cameras in the [Camera Inspector](camera-component-reference.md). |
| __Opaque Downsampling__ | Set the sampling mode on the opaque texture to one of the following:<br/>__None__:  Produces a copy of the opaque pass in the same resolution as the camera.<br/>__2x Bilinear__: Produces a half-resolution image with bilinear filtering.<br/>__4x Box__: Produces a quarter-resolution image with box filtering. This produces a softly blurred copy.<br/>__4x Bilinear__: Produces a quarter-resolution image with bi-linear filtering. |
| __Terrain Holes__       | If you disable this option, the URP removes all Terrain hole Shader variants when you build for the Unity Player, which decreases build time. |


### Quality                                                                                                                                                                                                                                         
These settings control the quality level of the URP. This is where you can make performance better on lower-end hardware or make graphics look better on  higher-end hardware. 

**Tip:** If you want to have different settings for different hardware, you can configure these settings across multiple Universal Render Pipeline assets, and switch them out as needed.

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| __HDR__          | Enable this to allow rendering in High Dynamic Range (HDR) by default for every camera in your Scene. With HDR, the brightest part of the image can be greater than 1. This gives you a wider range of light intensities, so your lighting looks more realistic. With it, you can still see details and experience less saturation even with bright light. This is useful if you want a wide range of lighting or to use [bloom](https://docs.unity3d.com/Manual/PostProcessing-Bloom.html) effects. If you’re targeting lower-end hardware, you can disable this to skip HDR calculations and get better performance. You can override this for individual cameras in the Camera Inspector. |
| __MSAA__         | Use [Multi Sample Anti-aliasing](https://en.wikipedia.org/wiki/Multisample_anti-aliasing) by default for every Camera in your Scene while rendering. This softens edges of your geometry, so they’re not jagged or flickering. In the drop-down menu, select how many samples to use per pixel: __2x__, __4x__, or __8x__. The more samples you choose, the smoother your object edges are. If you want to skip MSAA calculations, or you don’t need them in a 2D game, select __Disabled__. You can override this for individual cameras in the Camera Inspector. |
| __Render Scale__ | This slider scales the render target resolution (not the resolution of your current device). Use this when you want to render at a smaller resolution for performance reasons or to upscale rendering to improve quality.  This only scales the game rendering. UI rendering is left at the native resolution for the device. |



### Lighting

These settings affect the lights in your Scene. 

If you disable some of these settings, the relevant [keywords](shader-stripping.md) are [stripped from the Shader variables](shading-model.md#shaderStripping). If there are settings that you know for certain you won’t use in your game or app, you can disable them to improve performance and reduce build time.

| Property              | Description                                                  |
| --------------------- | ------------------------------------------------------------ |
| __Main Light__        | These settings affect the main [Directional Light](https://docs.unity3d.com/Manual/Lighting.html) in your Scene. You can select this by assigning it as a [Sun Source](https://docs.unity3d.com/Manual/GlobalIllumination.html) in the Lighting Inspector. If you don’t assign a sun source, the URP treats the brightest directional light in the Scene as the main light. You can choose between [Pixel Lighting](https://docs.unity3d.com/Manual/LightPerformance.html) and _None_. If you choose None, URP doesn’t render a main light,  even if you’ve set a sun source. |
| __Cast Shadows__      | Check this box to make the main light cast shadows in your Scene. |
| __Shadow Resolution__ | This controls how large the shadow map texture for the main light is. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |
| __Additional Lights__ | Here, you can choose to have additional lights to supplement your main light. Choose between [Per Vertex](https://docs.unity3d.com/Manual/LightPerformance.html), [Per Pixel](https://docs.unity3d.com/Manual/LightPerformance.html), or __Disabled__. |
| __Per Object Limit__  | This slider sets the limit for how many additional lights can affect each GameObject. |
| __Cast Shadows__      | Check this box to make the additional lights cast shadows in your Scene. |
| __Shadow Resolution__ | This controls the size of the textures that cast directional shadows for the additional lights. This is a sprite atlas that packs up to 16 shadow maps. High resolutions give sharper, more detailed shadows. If memory or rendering time is an issue, try a lower resolution. |




### Shadows

These settings affect how shadows look and behave. They also impact performance, so this is where you can make tweaks to get the best balance between visual quality and shadow rendering speed.


| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| __Distance__     | This controls how far ahead of the camera objects cast shadows, in Unity units. After this distance, URP doesn’t render shadows. For example, the value 100 means that objects more than 100 meters away from the camera do not cast shadows. Use this in large, open worlds, where rendering shadows far away can consume lots of memory. Or use it in top-down games with limited view distance. |
| __Cascades__     | Select the number of  [cascades](https://docs.unity3d.com/Manual/DirLightShadows.html) for shadows. A high number of cascades gives you more detailed shadows nearer the camera.The options are: __None__, __Two Cascades__ and __Four Cascades__. If you’re experiencing performance issues, try lowering the amount of cascades. You can also configure the distance for shadows in the  section below the setting. Further away from the camera, shadows become less detailed. |
| __Soft Shadows__ | If you have enabled shadows for either __Main Light__ or __Additional Light__, you can enable this to add a smoother filtering on the shadow maps. This gives you smooth edges on shadows. When enabled, the render pipeline performs a 5x5 Tent filter on desktop platforms and a 4 Tap filter on mobile devices. When disabled, the render pipeline samples the shadow once with default hardware filtering. If you disable this feature, you’ll get faster rendering, but sharper, possibly pixelated, shadow edges. |



### Post-processing

This section allows you to fine-tune global post-processing settings.

| Property         | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| __Grading Mode__ | Select the [color grading](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) mode to use for the Project.<br />&#8226; __High Dynamic Range__: This mode works best for high precision grading similar to movie production workflows. Unity applies color grading before tonemapping.<br />&#8226; __Low Dynamic Range__: This mode follows a more classic workflow. Unity applies a limited range of color grading after tonemapping. |
| __LUT Size__     | Set the size of the internal and external [look-up textures (LUTs)](https://docs.unity3d.com/Manual/PostProcessing-ColorGrading.html) that the Universal Render Pipeline uses for color grading. Higher sizes provide more precision, but have a potential cost of performance and memory use. You cannot mix and match LUT sizes, so decide on a size before you start the color grading process.<br />The default value, **32**, provides a good balance of speed and quality. |



### Advanced

This section allows you to fine-tune less commonly changed settings, which impact deeper rendering features and Shader combinations. 

| Property                   | Description                                                  |
| -------------------------- | ------------------------------------------------------------ |
| __SRP Batcher__            | Check this box to enable the SRP Batcher. This is useful if you have many different Materials that use the same Shader. The SRP Batcher is an inner loop that speeds up CPU rendering without affecting GPU performance. When you use the SRP Batcher, it replaces the SRP rendering code inner loop. |
| __Dynamic Batching__       | Enable [Dynamic Batching](https://docs.unity3d.com/Manual/DrawCallBatching.html), to make the render pipeline automatically batch small dynamic objects that share the same Material. This is useful for platforms and graphics APIs that do not support GPU instancing. If your targeted hardware does support GPU instancing, disable __Dynamic Batching__. You can change this at run time. |
| __Mixed Lighting__         | Enable [Mixed Lighting](https://docs.unity3d.com/Manual/LightMode-Mixed.html), to tell the pipeline to include mixed lighting shader variants in the build. |
| __Debug Level__            | Set the level of debug information that the render pipeline generates. The values are:<br />**Disabled**:  Debugging is disabled. This is the default.<br  />**Profiling**: Makes the render pipeline provide detailed information tags, which you can see in the FrameDebugger. |
| __Shader Variant Log Level__ | Set the level of information about Shader Stripping and Shader Variants you want to display when Unity finishes a build. Values are:<br /> **Disabled**: Unity doesn’t log anything.<br />**Only Universal**: Unity logs information for all of the [URP Shaders](shaders-in-universalrp.md).<br />**All**: Unity logs information for all Shaders in your build.<br /> You can see the information in Console panel when your build has finished. |



### Adaptive Performance

This section appears if Adaptive Performance package is installed. It allows to change settings how Adaptive performance and render pipeline interact.

| __Property__            | __Description__                                              |
| ----------------------- | ------------------------------------------------------------ |
| __Use adaptive performance__  | Allows Adaptive Performance to adjust rendering quality during runtime. |
