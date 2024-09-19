---
uid: urp-rendering-debugger-reference
---
# Rendering Debugger reference

The Rendering Debugger contains the following sections:

* [Display Stats](#display-stats)
* [Frequently Used](#frequently-used)
* [Rendering](#rendering)
* [Material](#material)
* [Lighting](#lighting)
* [Render Graph](#render-graph)
* [Probe Volume](#probe-volume-panel)

## Display Stats

The **Display Stats** panel shows statistics relevant to debugging performance issues in your project. You can only view this section of the Rendering Debugger in Play Mode. Refer to [Open the Rendering Debugger](rendering-debugger-use.md#control-the-overlay) for more information. 

### <a name="frame-stats"></a>Frame Stats

The Frame Stats section displays the average, minimum, and maximum value of each property. URP calculates each Frame Stat value over the 30 most recent frames.

| **Property**                 | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Frame Rate**               | The frame rate (in frames per second) for the current camera view. |
| **Frame Time**               | The total frame time for the current camera view.            |
| **CPU Main Thread Frame**    | The total time (in milliseconds) between the start of the frame and the time when the Main Thread finished the job. |
| **CPU  Render Thread Frame** | The time (in milliseconds) between the start of the work on the Render Thread and the time Unity waits to render the present frame ([Gfx.PresentFrame](https://docs.unity3d.com/2022.1/Documentation/Manual/profiler-markers.html)). |
| **CPU Present Wait**         | The time (in milliseconds) that the CPU spent waiting for Unity to render the present frame ([Gfx.PresentFrame](https://docs.unity3d.com/2022.1/Documentation/Manual/profiler-markers.html)) during the last frame. |
| **GPU Frame**                | The amount of time (in milliseconds) the GPU takes to render a given frame. |
| **Debug XR Layout**          | Display debug information for XR passes. <br>This mode is only available in editor and development builds. |

<a name="bottlenecks"></a>

### Bottlenecks

A bottleneck is a condition that occurs when one process performs significantly slower than other components, and other components depend on it. 

The **Bottlenecks** section describes the distribution of the last 60 frames across the CPU and GPU. You can only check the Bottleneck information when you build your player on a device. 

**Note**: Vsync limits the **Frame Rate** based on the refresh rate of your device’s screen. This means when you enable Vsync, the **Present Limited** category is 100% in most cases. To turn Vsync off, go to **Edit** > **Project settings** > **Quality** > **Current Active Quality Level** and set the **Vsync Count** set to **Don't Sync**.

#### Bottleneck categories

| **Category**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **CPU**             | The percentage of the last 60 frames in which the CPU limited the frame time. |
| **GPU**             | The percentage of the last 60 frames in which the GPU limited the frame time. |
| **Present limited** | The percentage of the last 60 frames in which the frame time was limited by the following presentation constraints:<br>&bull; Vertical Sync (Vsync): Vsync synchronizes rendering to the refresh rate of your display.<br>&bull;[Target framerate]([Application.targetFrameRate](https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html)): A function that you can use to manually limit the frame rate of an application. If a frame is ready before the time you specify in targetFrameRate, Unity waits before presenting the frame. |
| **Balanced**        | This percentage of the last 60 frames in which the frame time was not limited by any of the above categories. A frame that is 100% balanced indicates the processing time for both CPU and GPU is approximately equal. |

#### Bottleneck example

If Vsync limited 20 of the 60 most recent frames, the Bottleneck section might appear as follows: 

- **CPU** 0.0%: This indicates that URP did not render any of the last 60 frames on the CPU.
- **GPU** 66.6%: This indicates that the GPU limited 66.6% of the 60 most recent frames rendered by URP.
- **Present Limited** 33.3%: This indicates that presentation constraints (Vsync or the [target framerate](https://docs.unity3d.com/ScriptReference/Application-targetFrameRate.html)) limited 33.3% of the last 60 frames.
- **Balanced** 0.0%: This indicates that in the last 60 frames, there were 0 frames where the CPU processing time and GPU processing time were the same.

In this example, the bottleneck is the GPU.

<a name="detailed-stats"></a>

### Detailed Stats

The Detailed Stats section displays the amount of time in milliseconds that each rendering step takes on the CPU and GPU. URP updates these values once every frame based on the previous frame. 

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| Update every second with average | Calculate average values over one second and update every second. |
| Hide empty scopes                | Hide profiling scopes that use 0.00ms of processing time on the CPU and GPU. |
| Debug XR Layout                  | Enable to display debug information for [XR](https://docs.unity3d.com/Manual/XR.html) passes. This mode only appears in the editor and development builds. |

## Frequently Used

This section contains a selection of properties that users use often. The properties are from the other sections in the Rendering Debugger window. For information about the properties, refer to the sections [Material](#material), [Lighting](#lighting), and [Rendering](#rendering).


## Rendering

The properties in this section let you visualize different rendering features.

### Rendering Debug

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Map Overlays**               | Specifies which render pipeline texture to overlay on the screen. The options are:<ul><li>**None**: Renders the scene normally without a texture overlay.</li><li>**Depth**: Overlays the camera's depth texture on the screen.</li><li>**Additional Lights Shadow Map**: Overlays the [shadow map](https://docs.unity3d.com/Manual/shadow-mapping.html) that contains shadows cast by lights other than the main directional light.</li><li>**Main Light Shadow Map**: Overlays the shadow map that contains shadows cast by the main directional light.</li></ul> |
| **&nbsp;&nbsp;Map Size**       | The width and height of the overlay texture as a percentage of the view window URP displays it in. For example, a value of **50** fills up a quarter of the screen (50% of the width and 50% of the height). |
| **HDR**                        | Indicates whether to use [high dynamic range (HDR)](https://docs.unity3d.com/Manual/HDR.html) to render the scene. Enabling this property only has an effect if you enable **HDR** in your URP Asset. |
| **MSAA**                       | Indicates whether to use [Multisample Anti-aliasing (MSAA)](./../anti-aliasing.md#msaa) to render the scene. Enabling this property only has an effect if:<ul><li>You set **Anti Aliasing (MSAA)** to a value other than **Disabled** in your URP Asset.</li><li>You use the Game View. MSAA has no effect in the Scene View.</li></ul> |
| **Post-processing**            | Specifies how URP applies post-processing. The options are:<ul><li>**Disabled**: Disables post-processing.</li><li>**Auto**: Unity enables or disables post-processing depending on the currently active debug modes. If color changes from post-processing would change the meaning of a debug mode's pixel, Unity disables post-processing. If no debug modes are active, or if color changes from post-processing don't change the meaning of the active debug modes' pixels, Unity enables post-processing.</li><li>**Enabled**: Applies post-processing to the image that the camera captures.</li></ul> |
| **Additional Wireframe Modes** | Specifies whether and how to render wireframes for meshes in your scene. The options are:<ul><li>**None**: Doesn't render wireframes.</li><li>**Wireframe**: Exclusively renders edges for meshes in your scene. In this mode, you can see the wireframe for meshes through the wireframe for closer meshes.</li><li>**Solid Wireframe**: Exclusively renders edges and faces for meshes in your scene. In this mode, the faces of each wireframe mesh hide edges behind them.</li><li>**Shaded Wireframe**: Renders edges for meshes as an overlay. In this mode, Unity renders the scene in color and overlays the wireframe over the top.</li></ul> |
| **Overdraw**                   | Indicates whether to render the overdraw debug view. This is useful to check where Unity draws pixels over one other. |

#### Mipmap Streaming

| **Property** | **Description** |
|-|-|
| **Disable Mip Caching** | If you enable **Disable Mip Caching**, Unity doesn't cache mipmap levels in GPU memory, and constantly discards mipmap levels from GPU memory when they're no longer needed. This means the mipmap streaming debug views more accurately display which mipmap levels Unity uses at the current time. Enabling this setting increases the amount of data Unity transfers from disk to the CPU and the GPU. |
| **Debug View** | Set a mipmap streaming debug view. Options:<ul><li>**None**: Display the normal view.</li><li>**Mip Streaming Performance**: Use color to indicate which textures use mipmap streaming, and whether mipmap streaming limits the number of mipmap levels Unity loads.</li><li>**Mip Streaming Status**: Use color on materials to indicate whether their textures use mipmap streaming. Diagonal stripes mean some of the textures use a [`requestedMipmapLevel`](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D-requestedMipmapLevel.html) that overrides mipmap streaming. Yellow means Unity can't stream the texture, or the texture is assigned to terrain.</li><li>**Mip Streaming Activity**: Use color to indicate whether Unity recently streamed the textures.</li><li>**Mip Streaming Priority**: Use color to indicate the streaming priority of the textures. Set streaming priority for a texture in the [**Texture Import Settings** window](https://docs.unity3d.com/6000.0/Documentation/Manual/class-TextureImporter.html).</li><li>**Mip Count**: Display the number of mipmap levels Unity loads for the textures.</li><li>**Mip Ratio**: Use color to indicate the pixel density of the highest-resolution mipmap levels Unity uploads for the textures.</li></ul> |
| **Debug Opacity** | Set the opacity of the **Debug View** you select. 0 means not visible and 1 means fully visible. This property is visible only if **Debug View** is not set to **None**. |
| **Combined Per Material** | Set the **Debug View** to display debug information of all the textures on a material, not individual texture slots. This property is only visible if **Debug View** is set to **Mip Streaming Status** or **Mip Streaming Activity**. |
| **Material Texture Slot** | Set which texture Unity uses from each material to display debug information. For example, set **Material Texture Slot** to **Slot 3** to display debug information for the fourth texture. If a material has fewer textures than the **Material Texture Slot** value, Unity uses no texture. This property is visible only if **Combined Per Material** is disabled, and **Debug View** is not set to **None**. |
| **Display Status Codes** | Display more detailed statuses for textures that display as **Not streaming** or **Warning** in the **Mip Streaming Status** debug view. This property is visible only if **Debug View** is set to **Mip Streaming Status**. |
| **Activity Timespan** | Set how long a texture displays as **Just streamed**, in seconds. This property is visible only if **Debug View** is set to **Mip Streaming Activity**. |
| **Terrain Texture** | Set which terrain texture Unity displays. You can select either **Control** for the control texture, or one of the diffuse textures. This property is visible only if **Debug View** is not set to **None**. |

### Pixel Validation

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Pixel Validation Mode**        | Specifies which mode Unity uses to validate pixel color values. The options are:<ul><li>**None**: Renders the scene normally and doesn't validate any pixels.</li><li>**Highlight NaN, Inf and Negative Values**: Highlights pixels that have color values that are NaN, Inf, or negative.</li><li>**Highlight Values Outside Range**: Highlights pixels that have color values outside a particular range. Use **Value Range Min** and **Value Range Max**.</li></ul> |
| **&nbsp;&nbsp;Channels**         | Specifies which value to use for the pixel value range validation. The options are:<ul><li>**RGB**: Validates the pixel using the luminance value calculated from the red, green, and blue color channels.</li><li>**R**: Validates the pixel using the value from the red color channel.</li><li>**G**: Validates the pixel using the value from the green color channel.</li><li>**B**: Validates the pixel using the value from the blue color channel.</li><li>**A**: Validates the pixel using the value from the alpha channel.</li></ul>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |
| **&nbsp;&nbsp; Value Range Min** | The minimum valid color value. Unity highlights color values that are less than this value.<br/><br/>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |
| **&nbsp;&nbsp; Value Range Max** | The maximum valid color value. Unity highlights color values that are greater than this value.<br/><br/>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |

### Material

The properties in this section let you visualize different Material properties.

#### Material Filters

| **Property** | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| --- |---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Material&#160;Override** | Select a Material property to visualize on every GameObject on screen.<br/>The available options are:<ul><li>Albedo</li><li>Specular</li><li>Alpha</li><li>Smoothness</li><li>AmbientOcclusion</li><li>Emission</li><li>NormalWorldSpace</li><li>NormalTangentSpace</li><li>LightingComplexity</li><li>Metallic</li><li>SpriteMask</li><li>RenderingLayerMasks</li></ul>With the **LightingComplexity** value selected, Unity shows how many Lights affect areas of the screen space. <br/> With the **RenderingLayerMasks** value selected, you can filter the layers you want to debug either manually using the **Filter Layers** option or by selecting a light with the **Filter Rendering Layers by Light** option. Additionally, you can override the debug colors using **Layers Color**. |
| **Vertex&#160;Attribute** | Select a vertex attribute of GameObjects to visualize on screen.<br/>The available options are:<ul><li>Texcoord0</li><li>Texcoord1</li><li>Texcoord2</li><li>Texcoord3</li><li>Color</li><li>Tangent</li><li>Normal</li></ul>                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     |

#### Material Validation

| **Property** | **Description** |
| --- | --- |
| **Material&#160;Validation Mode** | Select which Material properties to visualize: Albedo, or Metallic. Selecting one of the properties shows the new context menu. |
| &#160;&#160;&#160;&#160;**Validation&#160;Mode:&#160;Albedo** | Selecting **Albedo** in the Material&#160;Validation Mode property shows the **Albedo Settings** section with the following properties:<br>**Validation&#160;Preset**: Select a pre-configured material, or Default Luminance to visualize luminance ranges.<br/>**Min Luminance**: Unity draws pixels where the luminance is lower than this value with red color.<br/>**Max Luminance**: Unity draws pixels where the luminance is higher than this value with blue color.<br/>**Hue Tolerance**: available only when you select a pre-set material. Unity adds the hue tolerance to the minimum and maximum luminance values.<br/>**Saturation Tolerance**: available only when you select a pre-set material. Unity adds the saturation tolerance to the minimum and maximum luminance values. |
| &#160;&#160;&#160;&#160;**Validation&#160;Mode:&#160;Metallic** | Selecting **Metallic** in the Material&#160;Validation Mode property shows the **Metallic Settings** section with the following properties:<br>**Min Value**: Unity draws pixels where the metallic value is lower than this value with red color.<br>**Max Value**: Unity draws pixels where the metallic value is higher than this value with blue color. |

## Lighting

The properties in this section let you visualize different settings and elements related to the lighting system, such as shadow cascades, reflections, contributions of the Main and the Additional Lights, and so on.

### Lighting Debug Modes

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Lighting Debug Mode** | Specifies which lighting and shadow information to overlay on-screen to debug. The options are:<ul><li>**None**: Renders the scene normally without a debug overlay.</li><li>**Shadow Cascades**: Overlays shadow cascade information so you can determine which shadow cascade each pixel uses. Use this to debug shadow cascade distances. For information on which color represents which shadow cascade, refer to the [Shadows section of the URP Asset](../universalrp-asset.md#shadows).</li><li>**Lighting Without Normal Maps**: Renders the scene to visualize lighting. This mode uses neutral materials and disables normal maps. This and the **Lighting With Normal Maps** mode are useful for debugging lighting issues caused by normal maps.</li><li>**Lighting With Normal Maps**: Renders the scene to visualize lighting. This mode uses neutral materials and allows normal maps.</li><li>**Reflections**: Renders the scene to visualize reflections. This mode applies perfectly smooth, reflective materials to every Mesh Renderer.</li><li>**Reflections With Smoothness**: Renders the scene to visualize reflections. This mode applies reflective materials without an overridden smoothness to every GameObject.</li></ul> |
| **Lighting Features**   | Specifies flags for which lighting features contribute to the final lighting result. Use this to view and debug specific lighting features in your scene. The options are:<ul><li>**Nothing**: Shortcut to disable all flags.</li><li>**Everything**: Shortcut to enable all flags.</li><li>**Global Illumination**: Indicates whether to render [global illumination](https://docs.unity3d.com/Manual/realtime-gi-using-enlighten.html).</li><li>**Main Light**: Indicates whether the main directional [Light](../light-component.md) contributes to lighting.</li><li>**Additional Lights**: Indicates whether lights other than the main directional light contribute to lighting.</li><li>**Vertex Lighting**: Indicates whether additional lights that use per-vertex lighting contribute to lighting.</li><li>**Emission**: Indicates whether [emissive](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterEmission.html) materials contribute to lighting.</li><li>**Ambient Occlusion**: Indicates whether [ambient occlusion](../post-processing-ssao.md) contributes to lighting.</li></ul> |

## Render Graph

The properties in this section let you change how the [render graph system](../render-graph.md) works.

| **Property** | **Description** |
| --- | --- |
| **Clear Render Targets At Creation**  | Clear render textures the first time the render graph system uses them. |
| **Clear Render Targets When Freed**  | Clear render textures when they're no longer used by render graph. |
| **Disable Pass Culling** | Disable URP culling render passes that have no impact on the final render. |
| **Enable Logging** | Enable logging to the **Console** window. |
| **Log Frame Information** | Log how URP uses the resources during the frame, in the **Console** window. |
| **Log Resources** | Log the resources URP uses during the frame, in the **Console** window. |

<a name="ProbeVolume"></a>

# Probe Volumes

These settings make it possible for you to visualize [Adaptive Probe Volumes](../probevolumes.md) in your Scene, and configure the visualization.

### Subdivision Visualization

| **Property** | **Sub-property** | **Description** |
|-|-|-|
| **Display Cells** || Display cells. Refer to [Understanding Adaptive Probe Volumes](../probevolumes-concept.md) for more information. |
| **Display Bricks** || Display bricks. Refer to [Understanding Adaptive Probe Volumes](../probevolumes-concept.md) for more information. |
| **Live Subdivision Preview** || Enable a preview of Adaptive Probe Volume data in the scene without baking. This might make the Editor slower. This setting appears only if you select **Display Cells** or **Display Bricks**. |
|| **Cell Updates Per Frame** | Set the number of cells, bricks, and probe positions to update per frame. Higher values might make the Editor slower.  The default value is 4. This property appears only if you enable **Live Subdivision Preview**. |
|| **Update Frequency** | Set how frequently Unity updates cell, bricks, and probe positions, in seconds. The default value is 1. This property appears only if you enable **Live Subdivision Preview**. |
| **Debug Draw Distance** || Set how far from the scene camera Unity draws debug visuals for cells and bricks, in meters. The default value is 500. |

### Probe Visualization

| **Property** | **Sub-property** | **Description** |
|-|-|-|
| **Display Probes** || Display probes. |
|| **Probe Shading Mode** | Set what the Rendering Debugger displays. The options are:<ul><li><strong>SH</strong>: Display the [spherical harmonics (SH) lighting data](https://docs.unity3d.com/Manual/LightProbes-TechnicalInformation.html) for the final color calculation. The number of bands depends on the **SH Bands** setting in the active [URP Asset](../universalrp-asset.md).</li><li><strong>SHL0</strong>: Display the spherical harmonics (SH) lighting data with only the first band.</li><li><strong>SHL0L1</strong>: Display the spherical Harmonics (SH) lighting data with the first two bands.</li><li><strong>Validity</strong>: Display whether probes are valid, based on the number of backfaces the probe samples. Refer to [Fix issues with Adaptive Probe Volumes](../probevolumes-fixissues.md) for more information about probe validity.</li><li><strong>Probe Validity Over Dilation Threshold</strong>: Display red if a probe samples too many backfaces, based on the **Validity Threshold** set in the [Adaptive Probe Volumes panel](../probevolumes-lighting-panel-reference.md). This means the probe can't be baked or sampled.</li><li><strong>Invalidated By Touchup Volumes</strong>: Display probes that a [Probe Adjustment Volume component](../probevolumes-adjustment-volume-component-reference.md) has made invalid.</li><li><strong>Size</strong>: Display a different color for each size of [brick](../probevolumes-concept.md).</li><li><strong>Sky Occlusion SH</strong>: If you enable [sky occlusion](../probevolumes-skyocclusion.md), this setting displays the amount of indirect light the probe receives from the sky that bounced off static GameObjects. The value is a scalar, so it displays as a shade of gray.</li><li><strong>Sky Direction</strong>: Display a green circle that represents the direction from the probe to the sky. This setting displays a red circle if Unity can't calculate the direction, or **Sky Direction** in the [Adaptive Probe Volumes panel](../probevolumes-lighting-panel-reference.md) is disabled.</li></ul>|
|| **Debug Size** | Set the size of the displayed probes. The default is 0.3. |
|| **Exposure Compensation** | Set the brightness of the displayed probes. Decrease the value to increase brightness. The default is 0. This property appears only if you set **Probe Shading Mode** to **SH**, **SHL0**, or **SHL0L1**. |
|| **Max Subdivisions Displayed** | Set the lowest probe density to display. For example, set this to 0 to display only the highest probe density. |
|| **Min Subdivisions Displayed** | Set the highest probe density to display. |
| **Debug Probe Sampling** || Display how probes are sampled for a pixel. In the Scene view, in the **Adaptive Probe Volumes** overlay, select **Select Pixel** to change the pixel. |
|| **Debug Size** | Set the size of the **Debug Probe Sampling** display. |
|| **Debug With Sampling Noise** | Enable sampling noise for this debug view. Enabling this gives more accurate information, but makes the information more difficult to read. |
| **Virtual Offset Debug** || Display the offsets Unity applies to Light Probe capture positions. |
|| **Debug Size** | Set the size of the arrows that represent Virtual Offset values. |
| **Debug Draw Distance** || Set how far from the scene camera Unity draws debug visuals for cells and bricks, in meters. The default is 200. |

### Streaming

Use the following properties to control how URP streams Adaptive Probe Volumes. Refer to [Streaming Adaptive Probe Volumes](../probevolumes-streaming.md) for more information.

| **Property** | **Description** |
| ------------ | --------------- |
| **Freeze Streaming** | Stop Unity from streaming probe data. |
| **Display Streaming Score** | If you enable **Display Cells**, this setting darkens cells that have a lower priority for streaming. Cells closer to the camera usually have the highest priority. |
| **Maximum cell streaming** | Stream as many cells as possible every frame. |
| **Display Index Fragmentation** | Open an overlay that displays how fragmented the streaming memory is. A green square is an area of used memory. The more spaces between the green squares, the more fragmented the memory. |
| **Index Fragmentation Rate** | Displays the amount of fragmentation as a numerical value, where 0 is no fragmentation. |
| **Verbose Log** | Log information about streaming. |

### Scenario Blending

Use the following properties to control how URP blends Lighting Scenarios. Refer to [Bake different lighting setups with Lighting Scenarios](../probevolumes-bakedifferentlightingsetups.md) for more information.

| **Property** | **Description** |
| - | - |
| **Number of Cells Blended Per Frame** | Determines the maximum number of cells Unity blends per frame. The default is 10,000. |
| **Turnover Rate** | Set the blending priority of cells close to the camera. The range is 0 to 1, where 0 sets the cells close to the camera with high priority, and 1 sets all cells with equal priority. Increase **Turnover Rate** to avoid cells close to the camera blending too frequently. |
| **Scenario To Blend With** | Select a Lighting Scenario to blend with the active Lighting Scenario. |
| **Scenario Blending Factor** | Set how far to blend from the active Lighting Scenario to the **Scenario To Blend With**. The range is 0 to 1, where 0 is fully the active Lighting Scenario, and 1 is fully the **Scenario To Blend With**. |
