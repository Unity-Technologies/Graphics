# Frame Settings

Frame Settings are settings HDRP uses to render Cameras, real-time, baked, and custom reflections. You can set the default Frame Settings for each of these three individually from within the [HDRP Default Settings](Default-Settings-Window.md) tab (menu: **Edit > Project Settings > HDRP Default Settings**).

![](Images/FrameSettings1.png)

**Default Frame Settings For** is not just the title of the section, it also corresponds to the drop-down menu, to its right, that allows you to select which component to change the default Frame Settings for. Before you change any Frame Settings, select a component from the drop-down menu. The options are Camera, Baked or Custom Reflection, and Realtime Reflection. After you select a component, you can change the default settings HDRP uses to render a frame using that component. To make Cameras and Reflection Probes use their respective default Frame Settings, disable the **Custom Frame Settings** checkbox under the **General** settings of Cameras or under **Capture Settings** of Reflection Probes.

You can override the default Frame Settings on a per component basis. Enable the **Custom Frame Settings** checkbox to set specific Frame Settings for individual Cameras and Reflection Probes.This exposes the Frame Settings Override which gives you access to the same settings as in default Frame Settings within the HDRP Asset. Edit the settings within the Frame Settings Override to create a Frame Settings profile for an individual component.

Note that baked Reflection Probes use the Frame Settings at baking time only. After that, HDRP uses the baked texture without modifying it with updated Frame Settings. 

Note: Some options are grayed-out depending on whether you have enabled/disabled them in the Render Pipeline Supported Features section of your HDRP Asset.

Frame Settings affect all Cameras and Reflection Probes. HDRP handles Reflection Probes in the same way it does Cameras, this includes Frame Settings. All Cameras and Reflection Probes either use the default Frame Settings or a Frame Settings Override to render the Scene. 

## Properties

Frame Settings all include the following properties:

### Rendering

These settings determine the method that the Cameras and Reflection Probes using these Frame Settings use for their rendering passes. You can control properties such as the rendering method, whether or not to use MSAA, or even whether the Camera renders opaque Materials at all. Disabling these settings does not save on memory, but can improve performance.

| **Property**                        | **Description**                                              |
| ----------------------------------- | ------------------------------------------------------------ |
| **Lit Shader Mode**                 | Select the Shader Mode HDRP uses for the Lit Shader when the rendering component using these Frame Settings renders the Scene. |
| - **Depth Prepass within Deferred** | If you enable Decals then HDRP forces a depth prepass and you can not disable this feature. This feature fills the depth buffer with all Meshes, without rendering any color. It is an optimization option that depends on the Unity Project you are creating, meaning that you should measure the performance before and after you enable this feature to make sure it benefits your Project. This is only available if you set **Lit Shader Mode** to **Deferred**. |
| - **Clear GBuffers**                | Enable the checkbox to make HDRP clear GBuffers for Cameras using these Frame Settings. This is only available if you set **Lit Shader Mode** to **Deferred**. |
| - **MSAA within Forward**           | Enable the checkbox to enable MSAA for the rendering components using these Frame Settings. This is only available if you set **Lit Shader Mode** to **Forward**. |
| - **Alpha To Mask**                 | Enable the checkbox to make HDRP render with **Alpha to Mask** Materials that have enabled it. This is only available if you enable **MSAA within Forward**.|
| **Opaque Objects**                  | Enable the checkbox to make HDRP render Materials that have their **Surface Type** set to **Opaque**. If you disable this settings, Cameras/Reflection Probes using these Frame Settings do not render any opaque GameObjects. |
| **Transparent Objects**             | Enable the checkbox to make HDRP render Materials that have their **Surface Type** set to **Transparent**. If you disable this setting, Cameras/Reflection Probes using these Frame Settings do not render any transparent GameObjects. |
| **Decals**                          | Enable the checkbox to make HDRP process decals. Enable this on cameras that you want to render decals. |
| **Decal Layers**                    | Enable the checkbox to make HDRP process Decal Layers. |
| **Transparent Prepass**             | Enable the checkbox to make HDRP perform a Transparent Prepass. Enabling this feature causes HDRP to add polygons from transparent Materials to the depth buffer to improve sorting. |
| **Transparent Postpass**            | Enable the checkbox to make HDRP perform a Transparent Postpass. Enabling this feature causes HDRP to add polygons to the depth buffer that post-processing uses. |
| **Low Resolution Transparent**      | Enable the checkbox to allow HDRP to perform a low resolution render pass. If you disable this checkbox, HDRP renders transparent Materials using the **Low Resolution** render pass in full resolution. |
| **Ray Tracing**                     | Enable the checkbox to allow this Camera to use ray tracing features. This is only available if you enable ray tracing support in your [HDRP Asset](HDRP-Asset.md). |
| **Custom Pass**                     | Enable the checkbox to allow this Camera to use custom passes. This is only enabled if you enable custom passes in your HDRP asset.|
| **Motion Vectors**                  | Enable the checkbox to make HDRP perform a Motion Vectors pass, allowing Cameras using these Frame Settings to use Motion Vectors. Disabling this feature means the Cameras using these Frame Settings do not calculate object motion vectors or camera motion vectors. |
| - **Opaque Object Motion**          | Enable the checkbox to make HDRP support object motion vectors. Enabling this feature causes HDRP to calculate motion vectors for moving objects and objects with vertex animations. HDRP Cameras using these Frame Settings still calculate camera motion vectors if you disable this feature. |
| - **Transparent Object Motion**     | Enable the checkbox to allow HDRP to write the velocity of transparent GameObjects to the velocity buffer. To make HDRP write transparent GameObjects to the velocity buffer, you must also enable the **Transparent Writes Velocity** checkbox on each transparent Material. Enabling this feature means that effects, such as motion blur, affect transparent GameObjects.  This is useful for alpha blended objects like hair. |
| **Refraction**                      | Enable the checkbox to make HDRP process Refraction for Cameras/Reflection Probes using these Frame Settings. Refraction is when a transparent surface scatters light that passes through it. This add a resolve of ColorBuffer after the drawing of opaque materials to be use for Refraction effect during transparent pass. |
| **Distortion**                      | Enable the checkbox to make HDRP process Distortion. Enabling this feature causes HDRP to calculate a distortion pass. This allows Meshes with transparent Materials to distort the light that enters them. |
| - **Rough Distortion** | Enable the checkbox to allow HDRP to modulate distortion based on the roughness of the material. If you enable this option, HDRP generates a color pyramid with mipmaps to process distortion. This increases the resource intensity of the distortion effect. |
| **Post-process**                    | Enable the checkbox to make HDRP perform a Post-processing pass. Disable this feature to remove all post-processing effects from this Camera/Reflection Probe. |
| - **Custom Post-process**           | Enable the checkbox to allow HDRP to execute custom post processes. Disable this feature to remove all custom post-processing effects from this Camera/Reflection Probe. |
| - **Stop NaN**                        | Enable the checkbox to allow HDRP to replace pixel values that are not a number (NaN) with black pixels for [Cameras](HDRP-Camera.md) that have **Stop NaNs** enabled. |
| - **Depth Of Field**                  | Enable the checkbox to allow HDRP to add depth of field to Cameras affected by a Volume containing the [Depth Of Field](Post-Processing-Depth-of-Field.md) override. |
| - **Motion Blur**                     | Enable the checkbox to allow HDRP to add motion blur to Cameras affected by a Volume containing the [Motion Blur](Post-Processing-Motion-Blur.md) override. |
| - **Panini Projection**               | Enable the checkbox to allow HDRP to add panini projection to Cameras affected by a Volume containing the [Panini Projection](Post-Processing-Panini-Projection.md) override. |
| - **Bloom**                           | Enable the checkbox to allow HDRP to add bloom to Cameras affected by a Volume containing the [Bloom](Post-Processing-Bloom.md) override. |
| - **Lens Distortion**                 | Enable the checkbox to allow HDRP to add lens distortion to Cameras affected by a Volume containing the [Lens Distortion](Post-Processing-Lens-Distortion.md) override. |
| - **Chromatic Aberration**            | Enable the checkbox to allow HDRP to add chromatic aberration to Cameras affected by a Volume containing the [Chromatic Aberration](Post-Processing-Chromatic-Aberration.md) override. |
| - **Vignette**                        | Enable the checkbox to allow HDRP add a vignette to Cameras affected by a Volume containing the [Vignette](Post-Processing-Vignette.md) override. |
| - **Color Grading**                   | Enable the checkbox to allow HDRP to process color grading for Cameras. |
| - **Tonemapping**                     | Enable the checkbox to allow HDRP to process tonemapping for Cameras. |
| - **Film Grain**                      | Enable the checkbox to allow HDRP to add film grain to Cameras affected by a Volume containing the [Film Grain](Post-Processing-Film-Grain.md) override. |
| - **Dithering**                       | Enable the checkbox to allow HDRP to add dithering to  [Cameras](HDRP-Camera.md) that have **Dithering** enabled. |
| - **Anti-aliasing**                   | Enable the checkbox to allow HDRP to do a post-process anti-aliasing pass for Cameras. This method that HDRP uses is the method specified in the Camera's **Anti-aliasing** drop-down. |
| **After Post-process**              | Enable the checkbox to make HDRP render GameObjects that use an Unlit Material and have their **Render Pass** set to **After Postprocess**. This is useful to render GameObjects that are not affected by Post-processing effects. Disable this checkbox to make HDRP not render these GameObjects at all. |
| - **Depth Test**                     | Enable the checkbox to allow HDRP to perform a depth test for Shaders rendered in the **After Post-process** rendering pass. |
| **LOD Bias Mode**                   | Use the drop-down to select the method that Cameras use to calculate their level of detail (LOD) bias. An LOD bias is a multiplier for a Camera’s LOD switching distance. A larger value increases the view distance at which a Camera switches the LOD to a lower resolution.<br />&#8226;**From Quality Settings**: The Camera uses the **LOD Bias** property from your Unity Project's **Quality Settings**. To change this value, open the **Project Settings** window (menu: **Edit > Project Settings**), go to **Quality > HDRP > Rendering** and set the **LOD Bias** to the value you want.<br />&#8226;**Scale Quality Settings**: The Camera multiplies the **LOD Bias** property below by the **LOD Bias** property in your Unity Project's **Quality Settings**.<br />&#8226;**Fixed**: The Camera uses the **LOD Bias** property below. |
| - **LOD Bias**                      | Set the value that Cameras use to calculate their LOD bias. The Camera uses this value differently depending on the **LOD Bias Mode** you select. |
| **Maximum LOD Level Mode**          | Use the drop-down to select the mode that Cameras use to set their maximum level of detail. LODs begin at 0 (the most detailed) and increasingly get less detailed. The maximum level of detail is the least detailed LOD that this Camera renders. This is useful when you use realtime [Reflection Probes](Reflection-Probes-Intro.md) because they often do not need to use the highest LOD to capture their view of the Scene.<br />&#8226;**From Quality Settings**: The Camera uses the **Maximum LOD Level** property from your Unity Project's **Quality Settings**. To change this value, open the **Project Settings** window (menu: **Edit > Project Settings…**), go to **Quality > HDRP > Rendering** and set the **Maximum LOD Level** to the value you want.<br />&#8226;**Offset Quality Settings**: The Camera adds the **Maximum LOD Level** property below to the **Maximum LOD Level** property in your Unity Project's **Quality Settings**.<br />&#8226;**Fixed**: The Camera uses the **Maximum LOD Level** property below. |
| - **Maximum LOD Level**             | Set the value that Cameras use to calculate their maximum level of detail. The Camera uses this value differently depending on the **Maximum LOD Level Mode** you select. |
| **Material Quality Level**          | Select which material quality level to use when rendering from this Camera. <br />&#8226;**From Quality Settings**: The Camera uses the **Material Quality Level** property from your Unity Project's **Quality Settings**. To change this value, open the **Project Settings** window (menu: **Edit > Project Settings…**), go to **Quality > HDRP > Rendering** and set the **Material Quality Level** to the value you want. |

### Lighting

These settings control lighting features for your rendering components. Here you can enable and disable lighting features at

| **Property**                       | **Description**                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **Shadow Maps**                    | Enable the checkbox to make HDRP process Shadows. This makes this Camera/Reflection Probe capture shadows. |
| **Contact Shadows**                | Enable the checkbox to make HDRP process [Contact Shadows](Override-Contact-Shadows.md). Enabling this feature causes HDRP to calculate Contact Shadows for this Camera/Reflection Probe. |
| **Screen Space Shadows**           | [DXR only] Enable the checkbox to allow [Lights](Light-Component.md) to render shadow maps into screen space buffers to reduce lighting Shader complexity. This technique increases processing speed but also increases the memory footprint. |
| **Shadowmask**                     | Enable the checkbox to make HDRP support the [Shadowmasks lighting mode](Lighting-Mode-Shadowmask.md).       |
| **Screen Space Refection**         | Enable the checkbox to make HDRP process Screen Space Reflections (SSR). This allows HDRP to calculate SSR for this Camera/Reflection Probe. |
| - **Transparent**                  | Enable the checkbox to make HDRP process Screen Space Reflections (SSR) on transparent materials. |
| **Screen Space Global Illumination** | Enable the checkbox to make HDRP process Screen Space Global Illumination (SSGI). |
| **Screen Space Ambient Occlusion** | Enable the checkbox to make HDRP process Screen Space Ambient Occlusion (SSAO). This allows HDRP to calculate SSAO for this Camera/Reflection Probe. |
| **Transmission**                   | Enable the checkbox to make HDRP process the transmission effect. This allows subsurface scattering Materials to use transmission, for example, light transmits through a leaf with a subsurface scattering Material. |
| **Fog**                            | Enable the checkbox to make HDRP process atmospheric scattering. This allows your Camera/Reflection Probe to process atmospheric scattering effects such as the [fog](HDRP-Features.md#fog) from your Scene’s Volumes. |
| - **Volumetrics**                    | Enable the checkbox to make HDRP process Volumetrics. Enabling this setting allows your rendering component to render volumetric fog and lighting. |
| - - **Reprojection**   | Enable the checkbox to improve the quality of volumetrics at runtime. Enabling this feature causes HDRP to use several previous frames to calculate the volumetric effects. Using these previous frames helps to reduce noise and smooth out the effects. |
| **Light Layers**                   | Enable the checkbox to make HDRP process Light Layers.       |
| **Exposure Control**               | Enable the checkbox to use the exposure values you can set on relevant components in HDRP. Disable this checkbox to use a neutral value (0 Ev100) instead. |
| **Reflection Probe**               | Enable the checkbox to allow this Camera to use [Reflection Probes](Reflection-Probe.md). |
| **Planar Reflection Probe**        | Enable the checkbox to allow this Camera to use [Planar Reflection Probes](Planar-Reflection-Probe.md). |
| **Metallic Indirect Fallback**     | Enable the checkbox to render Materials with base color as diffuse for this Camera. This renders metals as diffuse Materials. This is a useful Frame Setting to use for real-time Reflection Probes because it renders metals as diffuse Materials to stop them appearing black when Unity can't calculate several bounces of specular lighting. |
| **Sky Reflection**                 | Enable the checkbox to allow this Camera to use the Sky Reflection. The Sky Reflection affects specular lighting. |
| **Direct Specular Lighting**       | Enable the checkbox to allow this Camera to render direct specular lighting. This allows HDRP to disable direct view dependent lighting. It doesn't save any performance. |
| **Subsurface Scattering**          | Enable the checkbox to make HDRP process subsurface scattering. Enabling this feature causes HDRP to simulate how light penetrates surfaces of translucent GameObjects, scatters inside them, and exits from different locations. |
| - **Quality Mode**        | Use the drop-down to select how the quality level for Subsurface Scattering is set.<br /> &#8226; **From Quality Settings**: The Camera uses the Subsurface Scattering **Sample Budget** property that is set in the Material Section of your Unity Project's **Quality Settings**.<br /> &#8226; **Override Quality Settings**: Allows you to set a custom sample budget for sample surface scattering for this Camera in the field below.|
| - **Quality Level**       | Use the drop-down to select the quality level when the **Quality Mode** is set to **From Quality Settings**.|
| - **Custom Sample Budget**       | The custom number of samples to use for for Subsurface Scattering calculations when the **Quality Mode** is set to **Override Quality Settings**.|

### Asynchronous Compute Shaders

These settings control which effects, if any, can make use execute compute Shader commands in parallel.

| **Property**                       | **Description**                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **Asynchronous Execution**         | Enable the checkbox to allow HDRP to execute certain compute Shader commands in parallel. |
| - **Light List**                     | Enable the checkbox to allow HDRP to build the Light List asynchronously. |
| - **Screen Space Reflection**        | Enable the checkbox to allow HDRP to calculate screen space reflection asynchronously. |
| - **Screen Space Ambient Occlusion** | Enable the checkbox to allow HDRP to calculate screen space ambient occlusion asynchronously. |
| - **Volume Voxelization**            | Enable the checkbox to allow HDRP to calculate volumetric voxelization asynchronously. |

### Light Loop Debug

Use these settings to enable or disable settings relating to lighting in HDRP.

Note: These settings are for debugging purposes only. Each property here describes an optimization so disabling any of them has a negative impact on performance. You should only disable an optimization to isolate issues for debugging.

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **FPTL For Forward Opaque**   | Enable the checkbox to make HDRP use Fine Pruned Tiled Lighting for Forward rendered opaque GameObjects. |
| **Big Tile Prepass**          | Enable the checkbox to make HDRP use an optimization using a prepass with bigger tiles for tile lighting computation. |
| **Deferred Tile**             | Enable the checkbox to make HDRP use tiles to calculate deferred lighting. Disable this checkbox to use a full-screen brute force pixel Shader instead. |
| **Compute Light Evaluation**  | Enable the checkbox to make HDRP compute lighting using a compute Shader and tile classification. Otherwise HDRP uses a generic pixel Shader. |
| **Compute Light Variants**    | Enable the checkbox to classify tiles by light type combinations. Enable Compute Light Evaluation to access this property. |
| **Compute Material Variants** | Enable the checkbox to classify tiles by Material variant combinations. Enable Compute Light Evaluation to access this property. |

## Debugging Frame Settings

You can use the [Render Pipeline Debug Window](Render-Pipeline-Debug-Window.md) to temporarily change Frame Settings for a Camera without altering the actual Frame Settings data of the Camera itself. This means that, when you stop debugging, the Frame Settings for the Camera are as you set them before you started debugging.
