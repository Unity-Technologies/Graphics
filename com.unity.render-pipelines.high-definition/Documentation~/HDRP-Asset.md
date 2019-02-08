# The High Definition Render Pipeline Asset

The High Definition Render Pipeline (HDRP) Asset controls the global rendering settings of your Project and creates an instance of the rendering pipeline. A rendering pipeline instance contains intermediate resources and an implementation of the render pipeline. For more information, see documentation on Rendering Pipelines.

## Creating an HDRP Asset

A new Project using the HDRP template includes an HDRP Asset file named HDRenderPipelineAsset in the Assets/Settings folder.

If you upgrade a Project to HDRP and therefore do not use the HDRP template, follow the steps below to create and customize your own HDRP Asset.

1. In the Unity Editor, go to the Project window and navigate to the folder you want to create your HDRP Asset in. This folder must be inside the Assets folder; you can not create Assets in the Packages folder.

2. In the menu, navigate to __Assets > Create > Rendering__ and click __High Definition Render Pipeline Asset__ to create your HDRP Asset.

3. Name the __HDRP Asset__ and press the Return key to confirm it.

When you have created a HDRP Asset, you must assign it it to pipeline:. 

1. Select __Edit > Project Settings > Graphics__ and locate the __Scriptable Render Pipeline Settings__ property at the top. 

2. Either drag and drop the HDRP Asset into the property field, or use the object picker (located on the right of the field) to select it from a list of all HDRP Assets in your Unity Project.

You can create multiple HDRP Assets containing different settings. You can change the HDRP Asset your render pipeline uses by either manually selecting an HDRP Asset in the Graphics Settings window (as shown above), or by using the `GraphicsSettings.renderPipelineAsset` property via script.

Creating multiple HDRP Assets is useful when developing a Project that supports multiple platforms,for example, PC, Xbox One and PlayStation 4. In each HDRP Asset, you can change settings to suite the hardware of each platform and then assign the relevant one when building your Project for each platform.

## Render Pipeline Resources

The HDRP Resources Asset stores references to Shaders and Materials used by HDRP.  When you build your Unity Project, HDRP embeds all of the resources that the HDRP Resources Asset references. This is the Scriptable Render Pipeline equivalent of the Legacy Resources folder mechanism of Unity. It is useful because it allows you to set up multiple render pipelines in a Unity Project and, when you build the Project, Unity only embeds Shaders and Materials relevant for that pipeline.

Unity creates a HDRP Resources Asset when you create a HDRP Asset and references it automatically.

## Render Pipeline Editor Resources

## Diffusion Profile List

The [Diffusion Profile List](Diffusion-Profile.html) Asset stores Subsurface Scattering and Transmission Control profiles for your Project. Create a Diffusion Profile Settings Asset by navigating to __Assets > Create > Rendering__ and clicking __Diffusion Profile List__.

## Render Pipeline Features

These settings enable or disable HDRP features in your Unity Project. Unity does not allocate memory or build Shader variants for disabled features, so disable settings that you are not using to save memory. You can not enable disabled features at run time.

![](Images/HDRPAsset1.png)

| Property| Description |
|:---|:---|
| **Shadow Mask** | Enables support for [Shadowmask](https://docs.unity3d.com/Manual/LightMode-Mixed-Shadowmask.html) in your Project. |
| **SSR (screen space reflection)** | Enables support for [SSR](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html). SSR is a technique for calculating reflections by reusing screen space data. |
| **SSAO (screen space ambient occlusion)** | Enables support for SSAO. SSAO is a technique for approximating ambient occlusion efficiently in realtime. |
| **Subsurface Scattering** | Enables Subsurface Scattering (SSS). SSS describes light penetration of the surface of a translucent object. |
| **Subsurface Scattering - High quality** | Increase the SSS Sample Count and enable ultra quality Subsurface Scattering. Be aware that increasing the sample count greatly increases the cost of the Subsurface Scattering effect. |
| **Volumetrics** | Enables support for volumetrics. This allows you to use **Volumetric Fog** for the **Fog Type** in the [Visual Environment](Visual-Environment.html). |
| **Volumetrics - high quality** | Increases the resolution of volumetrics. This increases the quality of fog effects, but increases the cost of the effect greatly. |
| **LightLayers** | Enables support for Light Layers. You can assign a Layer to a Light which then only lights up Mesh Renderers with a matching rendering Layer. |
| **Support Lit Shader Mode** | Choose which mode HDRP uses for the [Lit Shader](Lit-Shader.html). Select **Forward Only** to force the HDRP renderer to only use forward rendering. Select **Deferred Only** to force the HDRP renderer to use deferred rendering for Lit Shaders (HDRP still renders advanced Materials using forward rendering). Select **Both** to allow the Camera to use deferred and forward rendering. Selecting **Both** allows you to switch between forward and deferred rendering for Lit Shaders at run time or per Camera. Selecting a specific mode reduces build time and Shader memory because HDRP requires less Shader variants, but it is not possible to switch from one mode to the other at run time. |
| **Support Multi Sampling Anti-Aliasing** | Enables support for Multi Sampling Anti-Aliasing (MSAA). This is only available when **Support Lit Shader Mode** is set to **Forward Only**. |
| **MSAA Sample Count** | Sets the number of samples HDRP uses for with MSAA. |
| **Decals** | Enable support for Decals. |
| **Motion Vectors** | Enables support for Motion Vectors. HDRP uses Motion Vectors for effects like screen space reflection (SSR) and motion blur. When disabled, motion blur has no effect and HDRP calculates SSR with lower quality. |
| **Runtime debug display** | Displays Material and Lighting properties at run time to help debugging. Disable this to reduce build time and Shader memory. Disables support for HDRP’s runtime debug display. This disables the following debug modes: All Material debug modes except GBuffer debug. The Lux meter, diffuse lighting only, and specular lighting only lighting debug modes. The overriding option for overriding albedo. |
| **Dithering cross fade** | Enables support for dithering cross fade. This allows HDRP to implement smooth transitions between a GameObject’s LOD levels. When disabled, this reduces build time if you are not using LOD fade. |
| **Distortion** | Enables support for distortion. If your Unity Project does not use distortion, disable this checkbox to reduce build time . |
| **Transparent Backface** | Enables support for transparent back-face render passes. If your Unity Project does not need to make a transparent back-face pass, disable this checkbox to reduce build time. |
| **Transparent Depth Prepass** | Enables support for transparent depth render prepasses. If your Unity Project does not need to make a transparent depth prepass, disable this checkbox to reduce build time . |
| **Transparent Depth Postpass** | Enables support for transparent depth render postpasses. If your Unity Project does not make use of a transparent depth postpass. Uncheck this checkbox to reduce build time . |



## HDRP Asset Default Frame Settings

Frame Settings control the rendering passes made by the main Camera at run time. For more information about Frame Setting, and how to use them, see the [HDRP Frame Settings documentation](Frame-Settings.html).

## Cookies

Use the Cookie settings to configure the maximum resolution of individual cookies and the maximum resolution of cookie texture arrays that define the number of cookies on screen at one time. Larger sizes use more memory, but result in higher quality images.

| Property| Description |
|:---|:---|
| **Cookie Size** | The maximum individual cookie size for the 2D cookies that HDRP uses for Directional and Spot Lights.  |
| **Texture Array Size** | The maximum Texture Array size for the 2D cookies that HDRP uses for Directional and Spot Lights. Increase this to increase the amount of 2D cookies HDRP can use concurrently on screen. |
| **Point Cookie Size** | The maximum [Point Cookie](https://docs.unity3d.com/Manual/Cookies.html) size for the Cube cookies that HDRP uses for Point Lights. |
| **Cubemap Array Size** | The maximum cube map Array size for the Cube cookies that HDRP uses for Point Lights. Increase this to increase the amount of cube map cookies HDRP can use concurrently on screen. |



## Reflection

Use the Reflection settings to configure the resolution of your reflections and whether Unity should compress the Reflection Probe caches or not.

| Property| Description |
|:---|:---|
| **Compress Reflection Probe Cache** | Compresses the Reflection Probe Cache to save space on disk. |
| **Reflection Cubemap Size** | The maximum resolution of the individual Reflection Probe [cube maps](https://docs.unity3d.com/Manual/class-Cubemap.html). |
| **Probe Cache Size** | The maximum resolution of the Probe Cache. Defines how many Probe cube maps HDRP can save in cache. |
| **Compress Planar Reflection Probe Cache** | Compresses the Planar Reflection Probe Cache. |
| **Planar Reflection Texture Size** | The maximum resolution of the Planar Reflection texture. |
| **Planar Probe Cache Size** | The maximum size of the Planer Reflection Probe cache. |
| **Support Fabric BSDF Convolution** | By default, Fabric Materials reuse Reflection Probes HDRP calculates for the Lit Shader (GGX BRDF). If you enable this option, HDRP calculates another version of each Reflection Probe for the Fabric Shader, creating more accurate lighting effects. This increases the cost as HDRP must condition two Reflection Probes instead of one. It also reduces the number of visible Reflection Probes in the current view by half as the size of the cache storing Reflection Probe data does not change and now must store both versions of each Reflection Probe. |



## Sky

These settings control skybox reflections and skybox lighting.

| Property| Description |
|:---|:---|
| **Reflection Size** | The maximum resolution of the cube map HDRP uses to represent the sky. |
| **Lighting Override Mask** | The [Volume](Volumes.html) layer mask HDRP uses to override sky lighting. Use this to decouple the display sky and lighting. |



## LightLoop

Use these settings to enable or disable settings relating to lighting in HDRP. 

| Property| Description |
|:---|:---|
| **Max Directional Lights On Screen** | The maximum number of Directional Lights HDRP can handle on screen at once. |
| **Max Punctual Lights On Screen** | The maximum number of [Point and Spot Lights](Glossary.html#PunctualLight) HDRP can handle on screen at once. |
| **Max Area Lights On Screen** | The maximum number of area Lights HDRP can handle on screen at once. |
| **Max Env Lights On Screen** | The maximum number of environment Lights HDRP can handle on screen at once.  |
| **Max Decals On Screen** | The maximum number of Decals HDRP can handle on screen at once. |



## Shadows

These settings adjust the size of the shadow mask. Smaller values causes Unity to discard more distant shadows, while higher values lead to Unity displaying more shadows at longer distances from the camera.

Higher values use more memory.

### Atlas

| Property| Description |
|:---|:---|
| **Resolution** | The resolution of the shadow atlas. |
| **16-bit** | Forces HDRP to use 16-bit shadow maps. |
| **Dynamic Rescale** | Allows HDRP to rescale the shadow atlas if all the shadows on the screen don’t fit onto the shadow atlas.  |



### Max Requests

| Property| Description |
|:---|:---|
| **Max Shadow on Screen** | The maximum number of shadows you can have in view. A  Spot Light casts a single shadow, a Point Light casts six shadows, and a Directional Light casts shadows equal to the number of cascades defined in the [HD Shadow Settings](HD-Shadow-Settings.html) override. |



### Filtering Qualities

| Property| Description |
|:---|:---|
| **Shadow Quality** | The filtering quality for Punctual Lights. Higher values increase the shadow quality in HDRP as better filtering near the edges of shadows reduce aliasing effects. HDRP 2018.3: Shadow quality only works when you set **Lit Shader Mode** to **Forward** in frame settings. **Deferred** mode uses Low. |



#### Filtering Quality Presets

| Preset| Algorithm |
|:---|:---|
| **Low** | Point/Spot: PCF 3x3 (4 taps). Directional: PCF Tent  5x5  (9 taps). |
| **Medium** | Point/Spot: PCF 5x5 (9 taps). Directional: PCF Tent  7x7  (16 taps). |
| **High** | Point/Spot: PCSS. Directional: PCSS. |



Select __PCSS__ filtering to enable additional high quality filtering settings in the Light component.

## Decals

These settings control the draw distance and resolution of the decals atlas HDRP uses when it renders decals projected on transparent objects.

| Property| Description |
|:---|:---|
| **Draw Distance** | The maximum distance from the Camera at which Unity draws Decals. |
| **Atlas Width** | The Decal Atlas width. |
| **Atlas Height** | The Decal Atlas height. |
| **Enable Metal and AO properties** | Allows Decals to affect Metal and AO material properties. Enabling this option has a performance impact. |



