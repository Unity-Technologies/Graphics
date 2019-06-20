# Frame Settings

Frame Settings are settings HDRP uses to render Cameras, realtime, baked, and custom reflections. You can set the default Frame Settings for each of these three individually from within a [HDRP Asset](HDRP-Asset.html). Open a HDRP Asset in the Inspector and navigate to the __Default Frame Settings For__ section.

![](Images/FrameSettings1.png)

__Default Frame Settings For__ is not just the title of the section, it also corresponds to the drop-down menu, to its right, that allows you to select which component to change the default Frame Settings for. Before you change any Frame Settings, select a component from the drop-down menu. The options are __Camera__, __Baked or Custom Reflection__, and __Realtime Reflection__. After you select a component, you can change the default settings HDRP uses to render a frame using that component. To make Cameras and Reflection Probes use their respective default Frame Settings, set the __Rendering Path__ (under the __General__ settings of Cameras, and under the __Capture Settings__ of Reflection Probes) to __Use Graphics Settings__.

You can override the default Frame Settings on a per component basis. You can set specific Frame Settings for individual Cameras and Reflection Probes by setting the their respective __Rendering Path__ to __Custom__. This exposes the __Frame Settings Override__ which gives you access to the same settings as in default Frame Settings within the HDRP Asset. Edit the settings within the __Frame Settings Override__ to create a Frame Settings profile for an individual component.

For Cameras and realtime Reflection Probes, you can edit all Frame Setting, that your HDRP Asset supports, during run time. This is not possible for baked Reflection Probes because the reflections they produce have already been baked.

Note: Some options are grayed-out depending on whether you have enabled/disabled them in the __Render Pipeline Supported Features__ section of your __HDRP Asset__.

Frame Settings affect all Cameras and Reflection Probes. HDRP handles Reflection Probes in the same way it does Cameras, this includes Frame Settings. All Cameras and Reflection Probes either use the default Frame Settings or a Frame Settings Override to render the Scene. Frame Settings all include the following properties:

## Rendering Passes

These settings enable or disable the rendering passes made by Cameras and Reflection Probes using these Frame Settings. Disabling these settings does not save on memory, but can improve performance.

 

| Property| Description |
|:---|:---|
| **Transparent Prepass** | Tick this checkbox to enable Transparent Prepass.  Enabling this feature causes HDRP to add polygons from transparent Materials to the depth buffer to improve sorting. |
| **Transparent Postpass** | Tick this checkbox to enable Transparent Postpass. Enabling this feature causes HDRP to add polygons to the depth buffer that postprocessing uses. |
| **Motion Vectors** | Tick this checkbox to tell HDRP to perform a Motion Vectors pass, allowing Cameras using these Frame Settings to use Motion Vectors. Disabling this feature means the Cameras using these Frame Settings do not calculate object motion vectors or camera motion vectors. |
| **Object Motion Vectors** | Tick this checkbox to enable object motion vectors. Enabling this feature causes HDRP to calculate motion vectors for moving objects and objects with vertex animations. HDRP Cameras using these Frame Settings still calculate camera motion vectors if you disable this feature. |
| **Decals** | Tick this checkbox to enable decals. Enable this on cameras that you want to render decals. |
| **Rough Refraction** | Tick this checkbox to enable Rough Refraction.  |
| **Distortion** | Tick this checkbox to enable Distortion. Enabling this feature causes HDRP to calculate a distortion pass. This allows Meshes with transparent Materials to distort the light that enters them. |
| **Postprocess** | Tick this checkbox to enable a Postprocessing pass. Disable this feature to remove all postprocessing effects from this Camera/Reflection Probe. |



## Rendering Settings

These settings control the method that the cameras using these Frame Settings use for its rendering passes. You can control properties such as the rendering method HDRP uses for this camera, whether or not this camera uses MSAA, or even whether the camera renders Opaque Object at all.

| Property| Description |
|:---|:---|
| **Lit Shader Mode** | Select the Shader Mode HDRP uses for the Lit Shader when the rendering component using these Frame Settings renders the Scene. |
| **MSAA** | Only available when the **Lit Shader Mode** is set to **Forward**. Tick this checkbox to enable MSAA for the rendering components using these Frame Settings. |
| **Depth Prepass With Deferred Renderer** | Only available when the **Lit Shader Mode** is set to **Deferred**. If you enable **Decals** then HDRP forces a depth prepass and you can not disable this feature. This feature fills the depth buffer with all Meshes, without rendering any color. It is an optimization option that depends on the Unity Project you are creating, meaning that you should measure the performance before and after you enable this feature to make sure it benefits your Project. |
| **Opaque Objects** | Tick this checkbox to enable rendering for Materials that have their **Surface Type** set to **Opaque**. If you disable this settings, rendering components using these Frame Settings do not render any opaque GameObjects. |
| **Transparent Objects** | Tick this checkbox to enable rendering for Materials that have their **Surface Type** set to **Transparent**. If you disable this setting, rendering components using these Frame Settings do not render any transparent GameObjects. |
| **Enable Realtime Planar Reflection** | Tick this checkbox to enable support for realtime planar Reflection Probe updates. HDRP updates Planar Reflection Probes every frame, because they are view dependent. Disabling this feature causes HDRP to stop updating Planar Reflection Probes every frame, but they still render in the Scene. |



## Lighting Settings

These settings control lighting features for your rendering components. Here you can enable and disable lighting features at 

| Property| Description |
|:---|:---|
| **Shadow** | Tick this checkbox to enable Shadows. |
| **Contact Shadows** | Tick this checkbox to enable Contact Shadows. |
| **Shadow Masks** | Tick this checkbox to enable Shadow Masks. |
| **SSR** | Tick this checkbox to enable Screen Space Reflections. |
| **SSAO** | Tick this checkbox to enable Screen Space Ambient Occlusion. |
| **Subsurface Scattering** | Tick this checkbox to enable Subsurface Scattering. With this enabled, Unity simulates how light penetrates surfaces of translucent GameObjects, scatters inside them, and exits from different locations. |
| **Transmission** | Tick this checkbox to enable Transmission. This allows subsurface scattering Materials to use transmission, for example, light transmits through a leaf with a subsurface scattering Material. |
| **Atmospheric Scattering** | Tick this checkbox to enable Atmospheric Scattering. |
| **Volumetrics** | Tick this checkbox to enable Volumetrics. Enabling this setting allows your rendering component to render volumetric fog and lighting. |
| **Reprojection For Volumetrics** | Tick this checkbox to improve the quality of volumetrics at run time. Enabling this feature causes HDRP to use several previous frames to calculate the volumetric effects. Using these previous frames helps to reduce noise and smooth out the effects. |
| **LightLayers** | Tick this checkbox to enable LightLayers.  |



## Async Compute Settings

These settings control which effects, if any, can make use of parallel compute Shader command execution.

| Property| Description |
|:---|:---|
| **Async Compute** | Tick this checkbox to allow HDRP to execute certain compute Shader commands in parallel. This only has an effect if the target platform supports async compute. |
| **Build Light List in Async** | Tick this checkbox to allow HDRP to build the Light List asynchronously. |
| **SSR in Async** | Tick this checkbox to allow HDRP to calculate screen space reflection asynchronously. |
| **SSAO in Async** | Tick this checkbox to allow HDRP to calculate screen space ambient occlusion asynchronously. |
| **Contact Shadows in Async** | Tick this checkbox to allow HDRP to calculate Contact Shadows asynchronously. |
| **Volumetrics Voxelization in Async** | Tick this checkbox to allow HDRP to calculate volumetric voxelization asynchronously. |



## Light Loop Settings

Use these settings to enable or disable settings relating to lighting in HDRP. 

Note: These settings are for debugging purposes only so do not alter these values permanently.

| Property| Description |
|:---|:---|
| **FPTL For Forward Opaque** | Tick this checkbox to enable Fine Pruned Tiled Lighting for Forward rendered opaque GameObjects. |
| **Big Tile Prepass** | Tick this checkbox to enable Big Tile Prepass. |
| **Compute Light Evaluation** | Tick this checkbox to enable Compute Light Evaluation. |
| **Compute Light Variants** | Enable **Compute Light Evaluation** to access this property. Tick this checkbox to enable Compute Light Variants. |
| **Compute Material Variants** | Enable **Compute Light Evaluation** to access this property. Tick this checkbox to enable Compute Material Variants. |