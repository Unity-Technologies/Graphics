# High Definition Render Pipeline/Built-in Render Pipeline comparison

The tables that follow provide an overview of the Features that the High Definition Render Pipeline (HDRP) supports, compared to Unity's [Built-in Render Pipeline](https://docs.unity3d.com/Manual/built-in-render-pipeline.html).

## Camera

| **Feature**                 | **Built-in Render Pipeline**                                     | **High Definition Render Pipeline (HDRP)**                      |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **HDR rendering**       | Yes. The Built-in Renderer uses an HDR Texture Format.       | Yes |
| **HDR output** | Yes | No, however this feature is in research for HDRP. For more information, see [High dynamic range](https://docs.unity3d.com/Manual/HDR.html). |
| **Anti-Aliasing**       | Yes. The Built-in Renderer supports multi-sample anti-aliasing (MSAA) for the Forward Renderer.<br/><br/>If you use the Post-processing v2 package, this Render Pipeline also supports:<br/>&#8226; Temporal anti-aliasing (TAA).<br/>&#8226; Fast approximate anti-aliasing(FXAA).<br/>&#8226; Subpixel morphological anti-aliasing (SMAA). | Yes. HDRP supports:<br/>&#8226; MSAA, for the Forward Renderer.<br/>&#8226; TAA.<br/>&#8226; FXAA.<br/>&#8226; SMAA. |
| **Physical Camera**     | Yes. The **Built-in Render Pipeline** only uses physical camera properties to calculate the Camera's field of view. | Yes. HDRP uses physical camera properties to:<br/>&#8226; Calculate the Camera's field of view.<br/>&#8226; Calculate the exposure of the Scene.<br/>&#8226; Calculate the result of certain post-processing effects.. |
| **Multi Display**       | Yes                                                          | Yes                                                          |
| **Stacking**            | Yes                                                          | Not supported                                                |
| **Flare Layer**         | Yes                                                          | Not supported                                                |
| **Depth Texture**       | Yes                                                          | Yes                                                          |
| **Depth + Normals Texture** | Yes                                                          | Yes                                                          |
| **Color Texture**       | Not supported                                                | Yes                                                          |
| **Motion vectors**      | Yes                                                          | Yes                                                          |
| ***Dynamic Resolution*** |                                                              |                                                              |
| **Software**            | Yes. Limited.                                                | Yes. On all platforms.                                       |
| **Hardware**            | Not supported                                                | Yes but only for consoles.                                   |

## Realtime Lights

| **Feature**                | **Built-in Render Pipeline**                                     | **High Definition Render Pipeline (HDRP)**                              |
| ---------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| ***Light Types***      |                                                              |                                                              |
| **Directional**        | Yes                                                          | Yes                                                          |
| **Spot**               | Yes                                                          | Yes. Supports the following shapes:<br/>&#8226; Cone.<br/>&#8226; Pyramid.<br/>&#8226; Box. |
| **Point**              | Yes                                                          | Yes                                                          |
| **Area**               | Yes. Supports the following shapes:<br/>&#8226; Rectangle.<br/>&#8226; Disk (baked only). | Yes. Supports the following shapes:<br/>&#8226; Rectangle.<br/>&#8226; Tube.<br/>&#8226; Disk (baked only). |
| **Inner Spot Angle**   | Not supported                                                | Yes                                                          |
| **Shading**            | Multiple Passes                                              | Tiled/Clustered                                              |
| ***Culling***          |                                                              |                                                              |
| **Per-Object**         | Yes                                                          | Yes                                                          |
| **Per-Layer**          | Yes                                                          | Yes                                                          |
| ***Light Limits***     |                                                              | See Quality Settings                                         |
| **Main Directional Light** | 1                                                            | Unlimited, but HDRP only supports shadowing for one Directional Light at a time. |
| **Per Object**         | Unlimited                                                    | Unlimited                                                    |
| **Per Camera**         | Unlimited                                                    | Unlimited                                                    |
| **Attenuation**        | Legacy                                                       | InverseSquared                                               |
| **Vertex Lights**      | Yes                                                          | Not supported                                                |
| **SH Lights**          | Yes                                                          | Not supported                                                |

## Realtime Shadows

| **Feature**               | **Built-in Render Pipeline**                                     | **High Definition Render Pipeline (HDRP)**                           |
| --------------------- | ------------------------------------------------------------ | --------------------------------------------------------- |
| ***Light Types***     |                                                              |                                                           |
| **Directional**       | Yes                                                          | Yes, but only one at a time.                              |
| **Spot**              | Yes                                                          | Yes                                                       |
| **Point**             | Yes                                                          | Yes                                                       |
| **Area**              | Not supported                                                | Yes, but only for the Rectangle shape.                    |
| ***Shadow Projection*** |                                                              |                                                           |
| **Stable Fit**        | Yes                                                          | Yes                                                       |
| **Close Fit**         | Yes                                                          | Yes                                                       |
| ***Shadow Cascades*** |                                                              |                                                           |
| **Number of Cascades** | 1, 2, or 4                                                   | 1 to 4                                                    |
| **Control by Percentage** | Yes                                                          | Yes                                                       |
| **Control by Distance** | Not supported                                                | Yes                                                       |
| ***Shadow Resolve Type*** |                                                              |                                                           |
| **Lighting Pass**     | Yes                                                          | Yes                                                       |
| **Screen Space Pass** | Yes                                                          | Yes                                                       |
| **Shadow Bias**       | Yes. Supports the following types:<br/>&#8226; Constant clip space offset.<br/>&#8226; Normal bias. | Yes. Supports the following types:<br/>&#8226; Slope bias.<br/>&#8226; Normal bias. |

## Batching

| **Feature**                  | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------------ | ------------------------ | ------------------------------- |
| ***Static Batching***    |                          |                                 |
| **By Shader**            | Not supported            | Yes                             |
| **By Material**          | Yes                      | Yes                             |
| **Dynamic Batching**     | Yes                      | Yes                             |
| **Dynamic Batching Shadows** | Yes                      | Yes                             |
| **GPU Instancing**       | Yes                      | Yes                             |

## Color Space

| **Feature** | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------- | ------------------------ | ------------------------------- |
| **Linear** | Yes                      | Yes                             |
| **Gamma** | Yes                      | Not supported                   |

## Global Illumination (Back End)

| **Feature**            | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------ | ------------------------ | ------------------------------- |
| **Enlighten**      | Yes                      | Not supported                   |
| **Enlighten Realtime** | Yes                      | Not supported                   |
| **Progressive CPU** | Yes                      | Yes                             |
| **Progressive GPU** | Yes                      | Yes                             |

## Mixed Lighting

| **Feature**              | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**   |
| -------------------- | ------------------------ | --------------------------------- |
| **Subtractive**      | Yes                      | Not supported                     |
| **Baked Indirect**   | Yes                      | Yes                               |
| **Shadow Mask**      | Yes                      | Yes. This is a per-Light setting. |
| **Distance Shadow Mask** | Yes                      | Yes. This is a per-Light setting. |

## Global Illumination (Light Probes)

| **Feature**             | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------- | ------------------------ | ------------------------------- |
| **Blending**        | Yes                      | Yes                             |
| **Proxy Volume (LPPV)** | Yes                      | Yes                             |
| **Custom Provided** | Yes                      | Yes                             |
| **Occlusion Probes** | Yes                      | Yes                             |

## Global Illumination (Reflection Probes)

| **Feature**                 | **Built-in Render Pipeline**                                     | **High Definition Render Pipeline (HDRP)**                              |
| ----------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Realtime**            | Yes                                                          | yes                                                          |
| **Baked**               | Yes                                                          | Yes                                                          |
| ***Sampling***          |                                                              |                                                              |
| **Anchor Override**     | Yes                                                          | Not supported                                                |
| **Simple**              | Yes                                                          | See [Reflection Hierarchy](Reflection-in-HDRP.md). |
| **Blend Probes**        | Yes                                                          | See [Reflection Hierarchy](Reflection-in-HDRP.md). |
| **Blend Probes and Skybox** | Yes                                                          | See [Reflection Hierarchy](Reflection-in-HDRP.md). |
| ***Projection***        |                                                              |                                                              |
| **Box**                 | Yes                                                          | Yes                                                          |
| **Sphere**              | No                                                           | Yes                                                          |
| **Proxy Volume**        | Not supported                                                | Yes                                                          |
| **Other Reflections**   | The **Built-in Render Pipeline** also supports screen space reflection. | HDRP supports the following other reflection methods:<br/>&#8226; Planar Reflection Probes.<br/>&#8226; Screen space reflection.<br/>&#8226; Ray-traced reflection. |

## Global Illumination (Lightmap Modes)

| **Feature**         | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| --------------- | ------------------------ | ------------------------------- |
| **Non-Directional** | Yes                      | Yes                             |
| **Directional** | Yes                      | Yes                             |

## Global Illumination (Environmental)

| **Feature**      | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**                              |
| ------------ | ------------------------ | ------------------------------------------------------------ |
| ***Source*** |                          |                                                              |
| **Skybox**   | Yes                      | Uses a single sky to bake global illumination, otherwise uses sky settings per Volume. You can [create own sky via script](Creating-a-Custom-Sky.md) and Material. |
| **Gradient** | Yes                      | Uses a single sky to bake global illumination, otherwise uses sky settings per Volume. You can [create own sky via script](Creating-a-Custom-Sky.md) and Material. |
| **Color**    | Yes                      | Uses a single sky to bake global illumination, otherwise uses sky settings per Volume. You can [create own sky via script](Creating-a-Custom-Sky.md) and Material. |
| ***Ambient Mode*** |                          |                                                              |
| **Realtime** | Yes                      | Yes                                                          |
| **Baked**    | Yes                      | Yes                                                          |

## Sky

| **Feature**    | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**                              |
| ---------- | ------------------------ | ------------------------------------------------------------ |
| **Procedural** | Yes                      | Yes. This sky type is deprecated, but you can still use if if you install the Procedural Sky Sample. |
| **6 Sided** | Yes                      | Yes. The HDRI Sky supports cubemaps, which the Unity importer can build from 6-sided maps. |
| **Cubemap** | Yes                      | See HDRI Sky                                                 |
| **Panoramic** | Yes                      | Yes. The HDRI Sky supports cubemaps, which the Unity importer can build from panoramic maps. |
| **Physical** | No                       | Yes                                                          |
| **Gradient** | No                       | Yes                                                          |

## Fog

| **Feature**             | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**                              |
| ------------------- | ------------------------ | ------------------------------------------------------------ |
| **Linear**          | Yes                      | Not supported                                                |
| **Exponential**     | Yes                      | Yes. The [Fog Override](Override-Fog.md) uses exponential fog. |
| **Exponential Squared** | Yes                      | Not supported                                                |
| **Local Volumetric** | No                       | Yes                                                          |

## Shaders (General)

| **Feature**                     | **Built-in Render Pipeline**                      | **High Definition Render Pipeline (HDRP)**                              |
| --------------------------- | --------------------------------------------- | ------------------------------------------------------------ |
| **Shader Graph**            | Not supported                                 | Yes                                                          |
| **Surface Shaders**         | Yes                                           | Not supported                                                |
| **Camera-relative Rendering** | Not supported                                 | Yes. See [Camera Relative Rendering](Camera-Relative-Rendering.md). |
| ***Standard Lit Shader***   |                                               |                                                              |
| **Metallic Workflow**       | Yes                                           | Yes                                                          |
| **Specular Workflow**       | Yes                                           | Yes                                                          |
| ***Surface Type and Blend Mode*** |                                               |                                                              |
| **Opaque**              | Yes                                           | Yes                                                          |
| **Faded (Alpha Blend)**     | Yes                                           | Yes. HDRP also supports premultiplied alpha.                 |
| **Transparent**             | Yes                                           | Yes                                                          |
| **Cutout**                  | Yes                                           | Yes                                                          |
| **Additive**                | Not supported                                 | Yes                                                          |
| **Multiply**                | Not supported                                 | Not supported                                                |
| **Decals**                  | Not supported                                 | Yes. To create decals in HDRP, you can use a decal Mesh or a decal projector. |
| **Detail Maps**             | Yes. You can assigned albedo and normal maps. | Yes. HDRP uses the [mask and detail maps](Mask-Map-and-Detail-Map.md) to combine maps such ask albedo, normals, and smoothness |
| **Advanced Material Options** | None                                          | HDRP supports the following advanced Materials:<br/>&#8226; Anisotropic.<br/>&#8226; Subsurface Scattering.<br/>&#8226; Iridescence.<br/>&#8226; Translucence. |
| ***Surface Inputs***        |                                               |                                                              |
| **Albedo (Base Map)**       | Yes                                           | Yes                                                          |
| **Specular**                | Yes                                           | Yes                                                          |
| **Metallic**                | Yes                                           | Yes. Uses the mask map.                                      |
| **Smoothness**              | Yes                                           | Yes. Uses the mask map.                                      |
| **Ambient Occlusion**       | Yes                                           | Yes. Uses the mask map.                                      |
| **Normal Map**              | Yes                                           | Yes                                                          |
| **Detail Map**              | Yes                                           | Yes                                                          |
| **Detail Normal Map**       | Yes                                           | Yes                                                          |
| **Heightmap**               | Yes                                           | Yes. Supports both pixel and vertex displacement.            |
| **Detail Mask**             | Yes                                           | Yes. Uses the mask map.                                      |
| **Light Cookies**           | Yes. Supports grayscale Textures.    | Yes. Supports RGB Textures.                           |
| **Parallax Mapping**        | Yes                                           | Yes. Uses vertex displacement.                               |
| **Light Distance Fade**     | Not supported                                 | Yes                                                          |
| **Shadow Distance Fade**    | Yes                                           | Yes                                                          |
| **Shadow Cascade Blending** | Not supported                                 | Yes                                                          |
| **GPU Instancing**          | Yes                                           | Yes                                                          |
| **GPU Tessellation**        | Not supported                                 | Yes. Uses the LitTesselation Shader.                         |
| **Double Sided GI**         | Yes                                           | Yes                                                          |
| **Two Sided**               | Not supported                                 | Yes                                                          |
| **Order In Layer**          | Not supported                                 | Not supported                                                |
| ***Advanced Materials***    |                                               |                                                              |
| **ClearCoat**               | Not supported                                 | Yes                                                          |
| **Hair**                    | Not supported                                 | Yes                                                          |
| **Fabric**                  | Not supported                                 | Yes                                                          |

## LOD Management
In the Built-in Render Pipeline, you manage levels of detail (LOD) from the QualitySettings. Each quality setting defines a LOD Bias and a Maximum LOD value. As such, they are global to the quality setting and you cannot change them on a per camera basis. In HDRP, there are scalability settings that allow you to change the LOD settings per camera by using either predetermined values contained in the HDRP Asset of the current quality level or overridden values. For more information, see [HDRP Asset](HDRP-Asset.md) and [Frame Settings](Frame-Settings.md).

Managing LOD in this way has two consequences:
- Default LOD settings for a quality level are now stored in the HDRP Asset instead of the Quality Settings.
- Built-in APIs such as QualitySettings.lodBias or QualitySettings.maximumLODLevel no longer work. Instead, you need to change these properties through the camera Frame Settings. If you use the Built-in APIs, they have no effect at all.

## Render Pipeline Hooks

| **Feature**                                                  | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| -------------------------------------------------------- | ------------------------ | ------------------------------- |
| **Camera.RenderWithShader**                              | Yes                      | Not supported                   |
| **Camera.AddCommandBuffer(Camera.Remove[All]CommandBuffer)** | Yes                      | Not supported                   |
| **Camera.Render**                                        | Yes                      | Yes                             |
| **Light.AddCommandBuffer(LightRemove[All]CommandBuffer)** | Yes                      | Not supported                   |
| **OnPreCull**                                            | Yes                      | Not supported                   |
| **OnPreRender**                                          | Yes                      | Not supported                   |
| **OnPostRender**                                         | Yes                      | Not supported                   |
| **OnRenderImage**                                        | Yes                      | Not supported                   |
| **OnRenderObject**                                       | Yes                      | Not supported                   |
| **OnWillRenderObject**                                   | Yes                      | Not supported                   |
| **OnBecameVisible**                                      | Yes                      | Not supported                   |
| **OnBecameInvisible**                                    | Yes                      | Not supported                   |
| **Camera Replacement Material**                          | Not supported            | Not supported                   |
| **RenderPipeline.BeginFrameRendering**                   | Not supported            | Yes                             |
| **RenderPipeline.EndFrameRendering**                     | Not supported            | Yes                             |
| **RenderPipeline.BeginCameraRendering**                  | Not supported            | Yes                             |
| **RenderPIpeline.EndCameraRendering**                    | Not supported            | Yes                             |
| **UniversalRenderPipeline.RenderSingleCamera**           | Not supported            | Not supported                   |
| **ScriptableRenderPass**                                 | Not supported            | Not supported                   |
| **Custom Renderers**                                     | Not supported            | Not supported                   |
| **CustomPass**                                           | Not supported            | Yes                             |
| **Custom Post Process Pass**                             | Not supported            | Yes                             |

## Post-processing

| **Feature**                     | **Built-in Render Pipeline**                                     | **High Definition Render Pipeline (HDRP)**                              |
| --------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------ |
| **Implementation**          | Uses Post-Processing Version 2 package.                      | Native Post-Processing solution embedded in HDRP package     |
| **Ambient Occlusion**       | Yes. The **Built-in Render Pipeline** supports:<br/>&#8226; Multi-scale ambient occlusion. | Yes. HDRP supports:<br/>&#8226; Ground truth ambient occlusion.<br/>&#8226; [Ray-traced ambient occlusion](Ray-Traced-Ambient-Occlusion.md). |
| **Exposure**                | Yes. The **Built-in Render Pipeline** supports:<br/>&#8226; Fixed exposure.<br/>&#8226; Automatic exposure. | Yes. HDRP supports:<br/>&#8226; Fixed exposure.<br/>&#8226; Automatic (Eye adaptation).<br/>&#8226; Curve Mapping.<br/>&#8226; Physical Camera settings |
| **Bloom**                   | Yes                                                          | Yes                                                          |
| **Chromatic Aberration**    | Yes                                                          | Yes                                                          |
| **Color Grading / Tonemapping** | Yes                                                          | Yes                                                          |
| **Depth of Field**          | Yes. This includes Bokeh.                                    | Yes. This includes Bokeh.                                    |
| **Film Grain**              | Yes                                                          | Yes                                                          |
| **Lens Distortion**         | Yes                                                          | Yes                                                          |
| ***Motion Blur***           |                                                              |                                                              |
| **Object**                  | Yes                                                          | Yes                                                          |
| **Camera**                  | Yes                                                          | Yes                                                          |
| **Vignette**                | Yes                                                          | Yes                                                          |
| **Panini Projection**       | Not supported                                                | Yes                                                          |

## CPU Particles

| **Feature**           | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**         |
| ----------------- | ------------------------ | --------------------------------------- |
| **Soft Particles** | Yes                      | Yes                                     |
| **Distortion**    | Yes                      | Yes. This is available in Shader Graph. |
| **Flipbook Blending** | Yes                      | Yes. This is available in Shader Graph. |
| **Trail**         | Yes                      | Yes                                     |
| ***Shaders***     |                          |                                         |
| **Lit**           | Yes                      | Yes                                     |
| **Simple Lighting** | Yes. Uses Blinn Phong.   | Yes                                     |
| **Unlit**         | Yes                      | Yes                                     |
| **GPU Instancing** | Yes                      | No                                      |

## GPU Particles with VFX Graph

| **Feature**                    | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| -------------------------- | ------------------------ | ------------------------------- |
| **Integration with Vfx Graph** | Not supported            | Yes                             |
| **Soft Particles**         | Not supported            | Yes                             |
| **Distortion**             | Not supported            | Yes                             |
| **Flipbook Blending**      | Not supported            | Yes                             |
| **Trail**                  | Not supported            | Yes                             |
| **Half-resolution**        | Not supported            | Yes                             |
| ***Shaders***              |                          |                                 |
| **Lit**                    | Not supported            | Yes                             |
| **Simple Lighting**        | Not supported            | Yes                             |
| **Unlit**                  | Not supported            | Yes                             |

## Visual Effect Components

| **Feature**            | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)**                              |
| ------------------ | ------------------------ | ------------------------------------------------------------ |
| **Halo**           | Yes                      | Not supported                                                |
| **Lens Flare**     | Yes                      | Not supported                                                |
| **Trail Renderer** | Yes                      | Yes. You can also use the VFX Graph to create a custom trail effect. |
| **Billboard Renderer** | Yes                      | Yes, but only with the VFX Graph.                  |
| **Projector**      | Yes                      | Not supported                                                |

## Terrain

| **Feature**                  | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------------ | ------------------------ | ------------------------------- |
| **Speed Tree**           | Yes                      | SpeedTree 8 only. Expect visual changes to normals for correctness in 21.2. |
| **Paint Trees**          | Yes                      | Yes                             |
| **Detail**               | Yes                      | Not supported                   |
| **Wind Zone**            | Yes                      | Not supported                   |
| **Number of Layers**     | Unlimited                | 8                               |
| **GPU Instanced Rendering** | Yes                      | Yes                             |
| **Terrain Holes**        | Yes                      | Yes                             |
| ***Shaders***            |                          |                                 |
| **Physically Based**     | Not supported            | Yes                             |
| **Simple Lit (Blinn-Phong)** | Yes                      | Not supported                   |
| **Unlit**                | Not supported            | Not supported                   |

## 2D

| **Feature**       | **Built-in Render Pipeline**                           | **High Definition Render Pipeline (HDRP)** |
| ------------- | -------------------------------------------------- | ------------------------------- |
| **Sprite**    | Yes                                                | Not supported                |
| **Tilemap**   | Yes                                                | Not supported                |
| **Sprite Shape** | Yes                                                | Not supported                |
| **Pixel-Perfect** | Yes. Using the standalone 2D Pixel Perfect Package. | Not supported                   |
| **2D Lights** | Not supported                                      | Not supported                   |
| **2D Shadows** | Not supported                                      | Not supported                   |

## UI (Canvas Renderer)

| **Feature**                | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ------------------------ | ------------------------------- |
| **Screen Space - Overlay** | Yes                      | Yes                             |
| **Screen Space - Camera** | Yes                      | Yes                             |
| **World Space**        | Yes                      | Yes                             |
| **Text Mesh Pro**      | Yes                      | Yes                             |

## Ray Tracing

| **Feature**                        | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------------------ | ------------------------ | ------------------------------- |
| **Ray-traced Ambient Occlusion** | No                       | Yes                             |
| **Ray-traced Contact Shadows** | No                       | Yes                             |
| **Ray-traced Global Illumination** | No                       | Yes                             |
| **Ray-traced Reflections**     | No                       | Yes                             |
| **Ray-traced Shadows**         | No                       | Yes                             |
| **Ray-traced Recursive Rendering** | No                       | Yes                             |
| **Path Tracing**               | No                       | Yes                             |

## VR

| **Feature**                   | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------------------- | ------------------------ | ------------------------------- |
| **Multipass**             | Yes                      | Yes                             |
| **Single Pass (double-wide)** | Yes                      | Not supported                   |
| **Single Pass Instanced** | Yes                      | Windows and PSVR only           |
| **Multiview**             | Yes                      | In research                     |
| ***Platforms***           |                          |                                 |
| **Oculus Rift**           | Yes                      | Yes                             |
| **Oculus Quest**          | Yes                      | Not supported                   |
| **Oculus Go**             | Yes                      | Not supported                   |
| **Gear VR**               | Yes                      | Not supported                   |
| **PSVR**                  | Yes                      | Yes                             |
| **HoloLens**              | Yes                      | Not supported                   |
| **WMR**                   | Yes                      | Yes                             |
| **Magic Leap One**        | Yes                      | Not supported                   |

## AR

| **Feature**       | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ------------- | ------------------------ | ------------------------------- |
| **AR Foundation** | Yes                    | No                        |

## Debug

| **Feature**          | **Built-in Render Pipeline** | **High Definition Render Pipeline (HDRP)** |
| ---------------- | ------------------------ | ------------------------------- |
| **Scene view modes** | Yes                      | Yes                             |
