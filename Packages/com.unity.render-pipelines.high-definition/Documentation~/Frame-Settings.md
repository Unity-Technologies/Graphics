# Frame Settings

Frame Settings are settings HDRP uses to render Cameras, real-time, baked, and custom reflections. You can set the default values for Frame Settings for each of these three individually from within the [HDRP Global Settings](Default-Settings-Window.md) tab (menu: **Edit** > **Project Settings** > **Graphics** > **HDRP Global Settings**).

![](Images/FrameSettings1.png)

To make Cameras and Reflection Probes use their respective default values for Frame Settings, disable the **Custom Frame Settings** checkbox under the **General** settings of Cameras or under **Capture Settings** of Reflection Probes.

You can override the default value of a Frame Setting on a per component basis. Enable the **Custom Frame Settings** checkbox to set specific Frame Settings for individual Cameras and Reflection Probes. This exposes the Frame Settings Override which gives you access to the same settings as within the HDRP Global Settings. Edit the settings within the Frame Settings Override to create a Frame Settings profile for an individual component.

**Note**: Baked Reflection Probes use the Frame Settings at baking time only. After that, HDRP uses the baked texture without modifying it with updated Frame Settings.

**Note**: If [Virtual Texturing](https://docs.unity3d.com/Documentation/Manual/svt-streaming-virtual-texturing.html) is disabled in your project, the **Virtual Texturing** setting is grayed out.

Frame Settings affect all Cameras and Reflection Probes. HDRP handles Reflection Probes in the same way it does Cameras, this includes Frame Settings. All Cameras and Reflection Probes either use the default Frame Settings or a Frame Settings Override to render the Scene.

## Properties

Frame Settings all include the following properties:

### Rendering

These settings determine the method that the Cameras and Reflection Probes using these Frame Settings use for their rendering passes. You can control properties such as the rendering method, whether to use MSAA, or even whether the Camera renders opaque Materials at all. Disabling these settings doesn't save on memory, but can improve performance.

<table>
<thead>
  <tr>
    <th>Property</th>
    <th></th>
    <th>Description</th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Lit Shader Mode</strong></td>
    <td></td>
    <td>Select the Shader Mode HDRP uses for the Lit Shader when the rendering component using these Frame Settings renders the Scene.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Depth Prepass within Deferred</strong></td>
    <td>If you enable Decals then HDRP forces a depth prepass and you can not disable this feature. This feature fills the depth buffer with all Meshes, without rendering any color. It's an optimization option that depends on the Unity Project you are creating, meaning that you should measure the performance before and after you enable this feature to make sure it benefits your Project. This is only available if you set <strong>Lit Shader Mode</strong> to <strong>Deferred</strong>.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Clear GBuffers</strong></td>
    <td>Enable the checkbox to make HDRP clear GBuffers for Cameras using these Frame Settings. This is only available if you set Lit <strong>Shader Mode</strong> to <strong>Deferred</strong>.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>MSAA</strong></td>
    <td>Select the MSAA quality for the rendering components using these Frame Settings. This is only available if you set <strong>Lit Shader Mode</strong> to <strong>Forward<strong>.<br/> <strong>Note</strong>: Using a different value for multiple different cameras has an impact on memory consumption.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Alpha To Mask</strong></td>
    <td>Enable the checkbox to make HDRP render with Alpha to Mask Materials that have enabled it. This is only available if you enable <strong>MSAA</strong> within <strong>Forward</strong>.</td>
  </tr>
  <tr>
    <td><strong>Opaque Objects</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP render Materials that have their Surface Type set to Opaque. If you disable this settings, Cameras/Reflection Probes using these Frame Settings don't render any opaque GameObjects.</td>
  </tr>
  <tr>
    <td><strong>Transparent Objects</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP render Materials that have their Surface Type set to Transparent. If you disable this setting, Cameras/Reflection Probes using these Frame Settings don't render any transparent GameObjects.</td>
  </tr>
  <tr>
    <td><strong>Decals</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process decals. Enable this on cameras that you want to render decals.</td>
  </tr>
  <tr>
    <td><strong>Decal Layers</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Decal Layers.</td>
  </tr>
  <tr>
    <td><strong>Transparent Prepass</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP perform a Transparent Prepass. Enabling this feature causes HDRP to add polygons from transparent Materials to the depth buffer to improve sorting.</td>
  </tr>
  <tr>
    <td><strong>Transparent Postpass</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP perform a Transparent Postpass. Enabling this feature causes HDRP to add polygons to the depth buffer that post-processing uses.</td>
  </tr>
  <tr>
    <td><strong>Low Resolution Transparent</strong></td>
    <td></td>
    <td>Enable the checkbox to allow HDRP to perform a low resolution render pass. If you disable this checkbox, HDRP renders transparent Materials using the Low Resolution render pass in full resolution.</td>
  </tr>
  <tr>
    <td><strong>Ray Tracing</strong></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to use ray tracing features. This is only available if you enable <strong>Ray Tracing</strong> in your <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/HDRP-Asset.html">HDRP Asset</a>.</td>
  </tr>
  <tr>
    <td><strong>Custom Pass</strong></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to use custom passes. This is only enabled if you enable <strong>Custom Passes</strong> in your HDRP asset.</td>
  </tr>
  <tr>
    <td><strong>Motion Vectors</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP perform a Motion Vectors pass, allowing Cameras using these Frame Settings to use Motion Vectors. Disabling this feature means the Cameras using these Frame Settings don't calculate object motion vectors or camera motion vectors.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Opaque Object Motion</strong></td>
    <td>Enable the checkbox to make HDRP support object motion vectors. Enabling this feature causes HDRP to calculate motion vectors for moving objects and objects with vertex animations. HDRP Cameras using these Frame Settings still calculate camera motion vectors if you disable this feature.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Transparent Object Motion</strong></td>
    <td>Enable the checkbox to allow HDRP to write the velocity of transparent GameObjects to the velocity buffer. To make HDRP write transparent GameObjects to the velocity buffer, you must also enable the <strong>Transparent Writes Velocity</strong> checkbox on each transparent Material. Enabling this feature means that effects, such as motion blur, affect transparent GameObjects. This is useful for alpha blended objects like hair.</td>
  </tr>
  <tr>
    <td><strong>Refraction</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Refraction for Cameras/Reflection Probes using these Frame Settings. Refraction is when a transparent surface scatters light that passes through it. This add a resolve of ColorBuffer after the drawing of opaque materials to be use for Refraction effect during transparent pass.</td>
  </tr>
  <tr>
    <td><strong>Distortion</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Distortion. Enabling this feature causes HDRP to calculate a distortion pass. This allows Meshes with transparent Materials to distort the light that enters them.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Rough Distortion</strong></td>
    <td>Enable the checkbox to allow HDRP to modulate distortion based on the roughness of the material. If you enable this option, HDRP generates a color pyramid with mipmaps to process distortion. This increases the resource intensity of the distortion effect.</td>
  </tr>
  <tr>
    <td><strong>Post-process</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP perform a Post-processing pass. Disable this feature to remove all post-processing effects from this Camera/Reflection Probe.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Custom Post-process</strong></td>
    <td>Enable the checkbox to allow HDRP to execute custom post processes. Disable this feature to remove all custom post-processing effects from this Camera/Reflection Probe.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Stop NaN</strong></td>
    <td>Enable the checkbox to allow HDRP to replace pixel values that aren't a number (NaN) with black pixels for <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/HDRP-Camera.html">Cameras</a> that have Stop NaNs enabled.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Depth Of Field</strong></td>
    <td>Enable the checkbox to allow HDRP to add depth of field to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Depth-of-Field.html">Depth Of Field</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Motion Blur</strong></td>
    <td>Enable the checkbox to allow HDRP to add motion blur to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Motion-Blur.html">Motion Blur</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Panini Projection</strong></td>
    <td>Enable the checkbox to allow HDRP to add panini projection to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Panini-Projection.html">Panini Projection</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Bloom</strong></td>
    <td>Enable the checkbox to allow HDRP to add bloom to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Bloom.html">Bloom</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Lens Distortion</strong></td>
    <td>Enable the checkbox to allow HDRP to add lens distortion to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Lens-Distortion.html">Lens Distortion</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Chromatic Aberration</strong></td>
    <td>Enable the checkbox to allow HDRP to add chromatic aberration to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Chromatic-Aberration.html">Chromatic Aberration</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Vignette</strong></td>
    <td>Enable the checkbox to allow HDRP add a vignette to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Vignette.html">Vignette</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Color Grading</strong></td>
    <td>Enable the checkbox to allow HDRP to process color grading for Cameras.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Tonemapping</strong></td>
    <td>Enable the checkbox to allow HDRP to process tonemapping for Cameras.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Film Grain</strong></td>
    <td>Enable the checkbox to allow HDRP to add film grain to Cameras affected by a Volume containing the <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Post-Processing-Film-Grain.html">Film Grain</a> override.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Dithering</strong></td>
    <td>Enable the checkbox to allow HDRP to add dithering to <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/HDRP-Camera.html">Cameras</a> that have Dithering enabled.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Anti-aliasing</strong></td>
    <td>Enable the checkbox to allow HDRP to do a post-process antialiasing pass for Cameras. This method that HDRP uses is the method specified in the Camera's <strong>Anti-aliasing</strong> drop-down.</td>
  </tr>
  <tr>
    <td><strong>After Post-process</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP render GameObjects that use an Unlit Material and have their <strong>Render Pass</strong> set to <strong>After Post-process</strong>. This is useful to render GameObjects that aren't affected by Post-processing effects. Disable this checkbox to make HDRP not render these GameObjects at all.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Depth Test</strong></td>
    <td>Enable the checkbox to allow HDRP to perform a depth test for Shaders rendered in the After Post-process rendering pass.</td>
  </tr>
  <tr>
    <td><strong>LOD Bias Mode</strong></td>
    <td></td>
    <td>Use the drop-down to select the method that Cameras use to calculate their level of detail (LOD) bias. An LOD bias is a multiplier for a Camera’s LOD switching distance. A larger value increases the view distance at which a Camera switches the LOD to a lower resolution.<br>•<strong>From Quality Settings</strong>: The Camera uses the <strong>LOD Bias</strong> property from your Unity Project's <strong>Quality Settings</strong>. To change this value, open the <strong>Project Settings</strong> window (menu: <strong>Edit</strong> &gt; <strong>Project Settings</strong>), go to <strong>Quality</strong> &gt; <strong>HDRP</strong> &gt; <strong>Rendering</strong> and set the <strong>LOD Bias</strong> to the value you want.<br>•<strong>Scale Quality Settings</strong>: The Camera multiplies the <strong>LOD Bias</strong> property below by the <strong>LOD Bias</strong> property in your Unity Project's <strong>Quality Settings</strong>.<br>•<strong>Fixed</strong>: The Camera uses the <strong>LOD Bias</strong> property below.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>LOD Bias</strong></td>
    <td>Set the value that Cameras use to calculate their LOD bias. The Camera uses this value differently depending on the <strong>LOD Bias Mode</strong> you select.</td>
  </tr>
  <tr>
    <td><strong>Maximum LOD Level Mode</strong></td>
    <td></td>
    <td>Use the drop-down to select the mode that Cameras use to set their maximum level of detail. LODs begin at 0 (the most detailed) and increasingly get less detailed. The maximum level of detail is the least detailed LOD that this Camera renders. This is useful when you use realtime <a href="https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.1/manual/Reflection-Probes-Intro.html">Reflection Probes</a> because they often don't need to use the highest LOD to capture their view of the Scene.<br>•From <strong>Quality Settings</strong>: The Camera uses the <strong>Maximum LOD Level</strong> property from your Unity Project's <strong>Quality Settings</strong>. To change this value, open the <strong>Project Settings</strong> window (menu: <strong>Edit</strong> &gt; <strong>Project Settings…</strong>), go to <strong>Quality</strong> &gt; <strong>HDRP</strong> &gt; <strong>Rendering</strong> and set the <strong>Maximum LOD Level</strong> to the value you want.<br>•<strong>Offset Quality Settings</strong>: The Camera adds the <strong>Maximum LOD Level</strong> property below to the <strong>Maximum LOD Level</strong> property in your Unity Project's <strong>Quality Settings</strong>.<br>•<strong>Fixed</strong>: The Camera uses the <strong>Maximum LOD Level</strong> property below.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Maximum LOD Level</strong></td>
    <td>Set the value that Cameras use to calculate their maximum level of detail. The Camera uses this value differently depending on the <strong>Maximum LOD Level Mode</strong> you select.</td>
  </tr>
  <tr>
    <td><strong>Material Quality Level</strong></td>
    <td></td>
    <td>Select which material quality level to use when rendering from this Camera.<br>•From <strong>Quality Settings</strong>: The Camera uses the <strong>Material Quality Level</strong> property from your Unity Project's <strong>Quality Settings</strong>. To change this value, open the <strong>Project Settings</strong> window (menu: <strong>Edit</strong> &gt; <strong>Project Settings</strong>…), go to <strong>Quality</strong> &gt; <strong>HDRP</strong> &gt; <strong>Rendering</strong> and set the <strong>Material Quality Level</strong> to the value you want.</td>
  </tr>
</tbody>
</table>

### Lighting

These settings control lighting features for your rendering components. Here you can enable and disable lighting features at

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th></th>
    <th></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Shadow Maps</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Shadows. This makes this Camera/Reflection Probe capture shadows.</td>
  </tr>
  <tr>
    <td><strong>Contact Shadows</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process <a href="https://www.tablesgenerator.com/Override-Contact-Shadows.md">Contact Shadows</a>. Enabling this feature causes HDRP to calculate Contact Shadows for this Camera/Reflection Probe.</td>
  </tr>
  <tr>
    <td><strong>Screen Space Shadows</strong></td>
    <td></td>
    <td></td>
    <td>[DXR only] Enable the checkbox to allow <a href="https://www.tablesgenerator.com/Light-Component.md">Lights</a> to render shadow maps into screen space buffers to reduce lighting Shader complexity. This technique increases processing speed but also increases the memory footprint.</td>
  </tr>
  <tr>
    <td><strong>Shadowmask</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP support the <a href="https://www.tablesgenerator.com/Lighting-Mode-Shadowmask.md">Shadowmasks lighting mode</a>.</td>
  </tr>
  <tr>
    <td><strong>Screen Space Refection</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Screen Space Reflections (SSR). This allows HDRP to calculate SSR for this Camera/Reflection Probe.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Transparent</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Screen Space Reflections (SSR) on transparent materials.</td>
  </tr>
  <tr>
    <td><strong>Screen Space Global Illumination</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Screen Space Global Illumination (SSGI).</td>
  </tr>
  <tr>
    <td><strong>Screen Space Ambient Occlusion</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Screen Space Ambient Occlusion (SSAO). This allows HDRP to calculate SSAO for this Camera/Reflection Probe.</td>
  </tr>
  <tr>
    <td><strong>Transmission</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process the transmission effect. This allows subsurface scattering Materials to use transmission, for example, light transmits through a leaf with a subsurface scattering Material.</td>
  </tr>
  <tr>
    <td><strong>Fog</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process atmospheric scattering. This allows your Camera/Reflection Probe to process atmospheric scattering effects such as the <a href="https://www.tablesgenerator.com/HDRP-Features.md#fog">fog</a> from your Scene’s Volumes.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Volumetrics</strong></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Volumetrics. Enabling this setting allows your rendering component to render volumetric fog and lighting.</td>
  </tr>
  <tr>
    <td></td>
    <td></td>
    <td><strong>Reprojection</strong></td>
    <td>Enable the checkbox to improve the quality of volumetrics at runtime. Enabling this feature causes HDRP to use several previous frames to calculate the volumetric effects. Using these previous frames helps to reduce noise and smooth out the effects.</td>
  </tr>
  <tr>
    <td><strong>Light Layers</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process Light Layers.</td>
  </tr>
  <tr>
    <td><strong>Exposure Control</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to use the exposure values you can set on relevant components in HDRP. Disable this checkbox to use a neutral value (0 Ev100) instead.</td>
  </tr>
  <tr>
    <td><strong>Reflection Probe</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to use <a href="https://www.tablesgenerator.com/Reflection-Probe.md">Reflection Probes</a>.</td>
  </tr>
  <tr>
    <td><strong>Planar Reflection Probe</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to use <a href="https://www.tablesgenerator.com/Planar-Reflection-Probe.md">Planar Reflection Probes</a>.</td>
  </tr>
  <tr>
    <td><strong>Metallic Indirect Fallback</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to render Materials with base color as diffuse for this Camera. This renders metals as diffuse Materials. This is a useful Frame Setting to use for real-time Reflection Probes because it renders metals as diffuse Materials to stop them appearing black when Unity can't calculate several bounces of specular lighting.</td>
  </tr>
  <tr>
    <td><strong>Sky Reflection</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to use the Sky Reflection. The Sky Reflection affects specular lighting.</td>
  </tr>
  <tr>
    <td><strong>Direct Specular Lighting</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to allow this Camera to render direct specular lighting. This allows HDRP to disable direct view dependent lighting. It doesn't save any performance.</td>
  </tr>
  <tr>
    <td><strong>Subsurface Scattering</strong></td>
    <td></td>
    <td></td>
    <td>Enable the checkbox to make HDRP process subsurface scattering. Enabling this feature causes HDRP to simulate how light penetrates surfaces of translucent GameObjects, scatters inside them, and exits from different locations.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Quality Mode</strong></td>
    <td></td>
    <td>Use the drop-down to select how to set the quality level for Subsurface Scattering.<br> • <strong>From Quality Settings</strong>: The Camera uses the Subsurface Scattering Sample Budget property that's set in the Material Section of your Unity Project's Quality Settings.<br> • <strong>Override Quality Settings</strong>: Allows you to set a custom sample budget for sample surface scattering for this Camera in the field below.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Quality Level</strong></td>
    <td></td>
    <td>Use the drop-down to select the quality level when to set the <strong>Quality Mode</strong> to <strong>From Quality Settings</strong>.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Custom Sample Budget</strong></td>
    <td></td>
    <td>The custom number of samples to use for Subsurface Scattering calculations when to set the <strong>Quality Mode</strong> to <strong>Override Quality Settings</strong>.</td>
  </tr>
</tbody>
</table>

### Asynchronous Compute Shaders

These settings control which effects, if any, can make use execute compute Shader commands in parallel.
This is only supported on DX12 and Vulkan. If Asynchronous execution is disabled or not supported the effects will fallback on a synchronous version.

<table>
<thead>
  <tr>
    <th><strong>Property</strong></th>
    <th></th>
    <th><strong>Description</strong></th>
  </tr>
</thead>
<tbody>
  <tr>
    <td><strong>Asynchronous Execution</strong></td>
    <td></td>
    <td>Enable the checkbox to allow HDRP to execute certain compute Shader commands in parallel.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Light List</strong></td>
    <td>Enable the checkbox to allow HDRP to build the Light List asynchronously.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Screen Space Reflection</strong></td>
    <td>Enable the checkbox to allow HDRP to calculate screen space reflection asynchronously.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Screen Space Ambient Occlusion</strong></td>
    <td>Enable the checkbox to allow HDRP to calculate screen space ambient occlusion asynchronously.</td>
  </tr>
  <tr>
    <td></td>
    <td><strong>Volume Voxelization</strong></td>
    <td>Enable the checkbox to allow HDRP to calculate volumetric voxelization asynchronously.</td>
  </tr>
</tbody>
</table>

### Light Loop Debug

Use these settings to enable or disable settings relating to lighting in HDRP.

**Note**: These settings are for debugging purposes only. Each property here describes an optimization so disabling any of them has a negative impact on performance. You should only disable an optimization to isolate issues for debugging.

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **FPTL For Forward Opaque**   | Enable the checkbox to make HDRP use Fine Pruned Tiled Lighting for Forward rendered opaque GameObjects. |
| **Big Tile Prepass**          | Enable the checkbox to make HDRP use an optimization using a prepass with bigger tiles for tile lighting computation. |
| **Deferred Tile**             | Enable the checkbox to make HDRP use tiles to calculate deferred lighting. Disable this checkbox to use a full-screen brute force pixel Shader instead. |
| **Compute Light Evaluation**  | Enable the checkbox to make HDRP compute lighting using a compute Shader and tile classification. Otherwise HDRP uses a generic pixel Shader. |
| **Compute Light Variants**    | Enable the checkbox to classify tiles by light type combinations. Enable Compute Light Evaluation to access this property. |
| **Compute Material Variants** | Enable the checkbox to classify tiles by Material variant combinations. Enable Compute Light Evaluation to access this property. |

## Debugging Frame Settings

You can use the [Rendering Debugger](Render-Pipeline-Debug-Window.md) to temporarily change Frame Settings for a Camera without altering the actual Frame Settings data of the Camera itself. This means that, when you stop debugging, the Frame Settings for the Camera are as you set them before you started debugging.
