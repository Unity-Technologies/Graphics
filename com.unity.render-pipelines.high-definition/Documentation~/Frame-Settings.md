# Frame Settings

Frame Settings are settings HDRP uses to render Cameras, real-time, baked, and custom reflections. You can set the default Frame Settings for each of these three individually from within a [HDRP Asset](HDRP-Asset.html). Open a HDRP Asset in the Inspector and navigate to the Default Frame Settings For section.

![](Images/FrameSettings1.png)

Default Frame Settings For is not just the title of the section, it also corresponds to the drop-down menu, to its right, that allows you to select which component to change the default Frame Settings for. Before you change any Frame Settings, select a component from the drop-down menu. The options are Camera, Baked or Custom Reflection, and Realtime Reflection. After you select a component, you can change the default settings HDRP uses to render a frame using that component. To make Cameras and Reflection Probes use their respective default Frame Settings, disable the **Custom Frame Settings** checkbox under the **General** settings of Cameras or under **Capture Settings** of Reflection Probes.

You can override the default Frame Settings on a per component basis. Enable the **Custom Frame Settings** checkbox to set specific Frame Settings for individual Cameras and Reflection Probes.This exposes the Frame Settings Override which gives you access to the same settings as in default Frame Settings within the HDRP Asset. Edit the settings within the Frame Settings Override to create a Frame Settings profile for an individual component.

Note that baked Reflection Probes use the Frame Settings at baking time only. After that, HDRP uses the baked texture without modifying it with updated Frame Settings. 

Note: Some options are grayed-out depending on whether you have enabled/disabled them in the Render Pipeline Supported Features section of your HDRP Asset.

Frame Settings affect all Cameras and Reflection Probes. HDRP handles Reflection Probes in the same way it does Cameras, this includes Frame Settings. All Cameras and Reflection Probes either use the default Frame Settings or a Frame Settings Override to render the Scene. 

## Properties

Frame Settings all include the following properties:

### Rendering

These settings determine the method that the Cameras and Reflection Probes using these Frame Settings use for their rendering passes. You can control properties such as the rendering method, whether or not to use MSAA, or even whether the Camera renders opaque Materials at all. Disabling these settings does not save on memory, but can improve performance.

| **Property**                      | **Description**                                              |
| --------------------------------- | ------------------------------------------------------------ |
| **Lit Shader Mode**               | Select the Shader Mode HDRP uses for the Lit Shader when the rendering component using these Frame Settings renders the Scene. |
| **Depth Prepass Within Deferred** | Only available when the Lit Shader Mode is set to Deferred. If you enable Decals then HDRP forces a depth prepass and you can not disable this feature. This feature fills the depth buffer with all Meshes, without rendering any color. It is an optimization option that depends on the Unity Project you are creating, meaning that you should measure the performance before and after you enable this feature to make sure it benefits your Project. |
| **MSAA within Forward**           | Only available when the Lit Shader Mode is set to Forward. Enable the checkbox to enable MSAA for the rendering components using these Frame Settings. |
| **Opaque Objects**                | Enable the checkbox to make HDRP render Materials that have their **Surface Type** set to **Opaque**. If you disable this settings, Cameras/Reflection Probes using these Frame Settings do not render any opaque GameObjects. |
| **Transparent Objects**           | Enable the checkbox to make HDRP render Materials that have their **Surface Type** set to **Transparent**. If you disable this setting, Cameras/Reflection Probes using these Frame Settings do not render any transparent GameObjects. |
| **Realtime Planar Reflection**    | Enable the checkbox to make HDRP support real-time [Planar Reflection Probe](Planar-Reflection-Probe.html) updates. HDRP updates Planar Reflection Probes every frame, because they are view dependent. Disabling this feature causes HDRP to stop updating Planar Reflection Probes every frame, but they still render in the Scene. |
| **Transparent Prepass**           | Enable the checkbox to make HDRP perform a Transparent Prepass. Enabling this feature causes HDRP to add polygons from transparent Materials to the depth buffer to improve sorting. |
| **Transparent Postpass**          | Enable the checkbox to make HDRP perform a Transparent Postpass. Enabling this feature causes HDRP to add polygons to the depth buffer that postprocessing uses. |
| **Transparents Write Velocity**   | Enable the checkbox to allow HDRP to write the velocity of transparent GameObjects to the velocity buffer. To make HDRP write transparent GameObjects to the velocity buffer, you must also enable the **Transparent Writes Velocity** checkbox on each transparent Material. Enabling this feature means that effects, such as motion blur, affect transparent GameObjects.  This is useful for alpha blended objects like hair. |
| **Motion Vectors**                | Enable the checkbox to make HDRP perform a Motion Vectors pass, allowing Cameras using these Frame Settings to use Motion Vectors. Disabling this feature means the Cameras using these Frame Settings do not calculate object motion vectors or camera motion vectors. |
| **Object Motion Vectors**         | Enable the checkbox to make HDRP support object motion vectors. Enabling this feature causes HDRP to calculate motion vectors for moving objects and objects with vertex animations. HDRP Cameras using these Frame Settings still calculate camera motion vectors if you disable this feature. |
| **Decals**                        | Enable the checkbox to make HDRP process decals. Enable this on cameras that you want to render decals. |
| **Rough Refraction**              | Enable the checkbox to make HDRP process Rough Refraction for Cameras/Reflection Probes using these Frame Settings. |
| **Distortion**                    | Enable the checkbox to make HDRP process Distortion. Enabling this feature causes HDRP to calculate a distortion pass. This allows Meshes with transparent Materials to distort the light that enters them. |
| **Postprocess**                   | Enable the checkbox to make HDRP perform a Postprocessing pass. Disable this feature to remove all postprocessing effects from this Camera/Reflection Probe. |
| **After Postprocess**             | Enable the checkbox to make HDRP render GameObjects that use an Unlit Material and have their **Render Pass** set to **After Postprocess**. This is useful to render GameObjects that are not affected by Post-processing effects. Disable this checkbox to make HDRP not render these GameObjects at all. |
| **LOD Bias Mode**                 | Use the drop-down to select the method that Cameras use to calculate their level of detail (LOD) bias. An LOD bias is a multiplier for a Camera’s LOD switching distance. A larger value increases the view distance at which a Camera switches the LOD to a lower resolution.<br />&#8226;**From Quality Settings**: The Camera uses the **LOD Bias** property from your Unity Project's **Quality Settings**. To change this value, open the **Project Settings** window (menu: **Edit > Project Settings**), go to **Quality > Other** and set the **LOD Bias** to the value you want.<br />&#8226;**Scale Quality Settings**: The Camera multiplies the **LOD Bias** property below by the **LOD Bias** property in your Unity Project's **Quality Settings**.<br />&#8226;**Fixed**: The Camera uses the **LOD Bias** property below. |
| **- LOD Bias**                    | Set the value that Cameras use to calculate their LOD bias. The Camera uses this value differently depending on the **LOD Bias Mode** you select. |
| **Maximum LOD Level Mode**        | Use the drop-down to select the mode that Cameras use to set their maximum level of detail. LODs begin at 0 (the most detailed) and increasingly get less detailed. The maximum level of detail is the least detailed LOD that this Camera renders. This is useful when you use realtime [Reflection Probes](Reflection-Probes-Intro.html) because they often do not need to use the highest LOD to capture their view of the Scene.<br />&#8226;**From Quality Settings**: The Camera uses the **Maximum LOD Level** property from your Unity Project's **Quality Settings**. To change this value, open the **Project Settings** window (menu: **Edit > Project Settings…**), go to **Quality > Other** and set the **Maximum LOD Level** to the value you want.<br />&#8226;**Offset Quality Settings**: The Camera adds the **Maximum LOD Level** property below to the **Maximum LOD Level** property in your Unity Project's **Quality Settings**.<br />&#8226;**Fixed**: The Camera uses the **Maximum LOD Level** property below. |
| **- Maximum LOD Level**           | Set the value that Cameras use to calculate their maximum level of detail. The Camera uses this value differently depending on the **Maximum LOD Level Mode** you select. |

### Lighting

These settings control lighting features for your rendering components. Here you can enable and disable lighting features at

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Shadow**                       | Enable the checkbox to make HDRP process Shadows. This makes this Camera/Reflection Probe capture shadows. |
| **Contact Shadows**              | Enable the checkbox to make HDRP process [Contact Shadows](Override-Contact-Shadows.html). Enabling this feature causes HDRP to calculate Contact Shadows for this Camera/Reflection Probe. |
| **Shadow Masks**                 | Enable the checkbox to make HDRP support Shadow Masks.      |
| **SSR**                          | Enable the checkbox to make HDRP process Screen Space Reflections (SSR). This allows HDRP to calculate SSR for this Camera/Reflection Probe. |
| **SSAO**                         | Enable the checkbox to make HDRP process Screen Space Ambient Occlusion (SSAO). This allows HDRP to calculate SSAO for this Camera/Reflection Probe. |
| **Subsurface Scattering**        | Enable the checkbox to make HDRP process subsurface scattering. Enabling this feature causes HDRP to simulate how light penetrates surfaces of translucent GameObjects, scatters inside them, and exits from different locations. |
| **Transmission**                 | Enable the checkbox to make HDRP process the transmission effect. This allows subsurface scattering Materials to use transmission, for example, light transmits through a leaf with a subsurface scattering Material. |
| **Atmospheric Scattering**       | Enable the checkbox to make HDRP process atmospheric scattering. This allows your Camera/Reflection Probe to process atmospheric scattering effects such as the [fog](HDRP-Features.html#FogOverview.html) from your Scene’s Volumes. |
| **Volumetrics**                  | Enable the checkbox to make HDRP process Volumetrics. Enabling this setting allows your rendering component to render volumetric fog and lighting. |
| **Reprojection For Volumetrics** | Enable the checkbox to improve the quality of volumetrics at runtime. Enabling this feature causes HDRP to use several previous frames to calculate the volumetric effects. Using these previous frames helps to reduce noise and smooth out the effects. |
| **Light Layers**                 | Enable the checkbox to make HDRP process Light Layers.      |
| **Exposure Control**             | Enable the checkbox to use the exposure values you can set on relevant components in HDRP. Disable this checkbox to use a neutral value (0) instead. |

### Async Compute

These settings control which effects, if any, can make use execute compute Shader commands in parallel.

| **Property**                       | **Description**                                              |
| ---------------------------------- | ------------------------------------------------------------ |
| **Async Compute**                  | Enable the checkbox to allow HDRP to execute certain compute Shader commands in parallel. |
| **Light List Async**               | Enable the checkbox to allow HDRP to build the Light List asynchronously. |
| **SSR Async**                      | Enable the checkbox to allow HDRP to calculate screen space reflection asynchronously. |
| **SSAO Async**                     | Enable the checkbox to allow HDRP to calculate screen space ambient occlusion asynchronously. |
| **Contact Shadows Async**          | Enable the checkbox to allow HDRP to calculate Contact Shadows asynchronously. |
| **Volumetrics Voxelization Async** | Enable the checkbox to allow HDRP to calculate volumetric voxelization asynchronously. |

### Light Loop

Use these settings to enable or disable settings relating to lighting in HDRP.

Note: These settings are for debugging purposes only so do not alter these values permanently.

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **FPTL For Forward Opaque**   | Enable the checkbox to make HDRP use Fine Pruned Tiled Lighting for Forward rendered opaque GameObjects. |
| **Big Tile Prepass**          | Enable the checkbox to make HDRP use an optimization using a prepass with bigger tiles for tile lighting computation. |
| **Deferred Tile**             | Enable the checkbox to make HDRP use tiles to calculate deferred lighting. Disable this checkbox to use a full-screen brute force pixel Shader instead. |
| **Compute Light Evaluation**  | Enable the checkbox to make HDRP compute lighting using a compute Shader and tile classification. Otherwise HDRP uses a generic pixel Shader. |
| **Compute Light Variants**    | Enable the checkbox to classify tiles by light type combinations. Enable Compute Light Evaluation to access this property. |
| **Compute Material Variants** | Enable the checkbox to classify tiles by Material variant combinations. Enable Compute Light Evaluation to access this property. |

## Debugging Frame Settings

You can use the [Render Pipeline Debug Window](Render-Pipeline-Debug-Window.html) to temporarily change Frame Settings without altering the actual Camera data.