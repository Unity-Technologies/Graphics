# Render Pipeline Debug

The **Render Pipeline Debug** window is a specific window for the Scriptable Render Pipeline that contains debugging and visualization tools. You can use these tools to quickly understand and solve any issues you might encounter. It contains mostly graphics-related tools but you can extend it to include tools for any other field, such as animation. The **Render Pipeline Debug** window separates debug items into different sections as follows:

- [Render Graph](#RenderGraphPanel)
- [Decals](#DecalsPanel)
- [Display Stats](#StatsPanel)
- [Material](#MaterialPanel)
- [Lighting](#LightingPanel)
- [Volume](#VolumePanel)
- [Rendering](#RenderingPanel)
- [Camera](#CameraPanel)

![](Images/RenderPipelineDebug1.png)

The Render Pipeline Debug window.

## Using the Render Pipeline Debug window

To open the Render Pipeline Debug window in the Editor, go to  **Window > Render Pipeline > Render Pipeline Debug**. You can also open this window at runtime in Play Mode, or in the standalone Unity Player on any device on **Development build**. Use the keyboard shortcut Ctrl+Backspace (Ctrl+Delete on macOS) or press L3 and R3 (Left Stick and Right Stick) on a controller to open the window.

You can display read-only items such as the FPS counter independently of the **Render Pipeline Debug** window. This means that when you disable the **Render Pipeline Debug** window, they are still visible in the top right corner of the screen. This is particularly useful if you want to track particular values without cluttering the screen.

### Navigation at runtime

To change the current active item:

- **Keyboard**: Use the arrow keys.
- **Xbox controller**: Use the Directional pad (D-Pad).
- **PlayStation controller**: Use the Directional buttons.

To change the current tab:

- **Keyboard**: Use the Page up and Page down keys (Fn + Up and Fn + Down keys respectively for MacOS).
- **Xbox controller**: Use the Left Bumper and Right Bumper.
- **PlayStation controller**: Use the L1 button and R1 button.

To display the current active item independently of the debug window:

- **Keyboard**: Press the right Shift key.
- **Xbox controller**: Press the X button.
- **PlayStation controller**: Press the Square button.

<a name="RenderGraphPanel"></a>

## Render Graph panel

The **Render Graph** panel has tools that you can use to debug the [Render Graph](https://docs.unity3d.com/2020.2/Documentation/Manual/render-graph.html) used by HDRP.

| **Debug Option**  | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Clear Render Targets at creation**  | Enable the checkbox to make the Render Graph system clear render targets the first time it uses them |
| **Disable Pass Culling**              | Enable the checkbox to render passes which have no impact on the final render. |
| **Immediate Mode**                    | Enable the checkbox to make the Render Graph system evaluate passes immediately after it creates them. |
| **Log Frame Information**             | Press the button to log in the Console informations about the passes rendered during a frame. |
| **Log Resources**                     | Press the button to log in the Console the list of resources used when rendering a frame. |

<a name="DecalsPanel"></a>

## Decals panel

The **Decals** panel has tools that you can use to debug [decals](Decal-Shader.md) in your project.

| **Debug Option**  | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Display Atlas** | Enable the checkbox to display the decal atlas for a Camera in the top left of that Camera's view. |
| **Mip Level**     | Use the slider to select the mip level for the decal atlas. The higher the mip level, the blurrier the decal atlas. |

<a name="StatsPanel"></a>

## Display Stats panel

The **display stats** panel is only visible in play mode and can be used to debug performance issues in your project.

| **Debug Option**  | **Description**                                              |
| ----------------- | ------------------------------------------------------------ |
| **Frame Rate** | Shows the frame rate in frames per second for the current camera view. |
| **Frame Time** | Shows the total frame time for the current camera view. |
| **RT Mode** | If ray tracing is enabled, shows the ray tracing Tier used during rendering.  |
| **Count Rays** | If ray tracing is enabled, enable the checkbox to count the number of traced rays per effect (In MRays / frame). |
| **- Ambient Occlusion** | The number of rays that were traced for Ambient Occlusion (AO) computations, when RT AO is enabled.   |
| **- Shadows Directional** | The number of rays that were traced for directional lights, when RT shadows are enabled.  |
| **- Shadows Area** | The number of rays that were traced towards area lights, when RT shadows are enabled.  |
| **- Shadows Point/Spot** | The number of rays that were traced towards punctual (point/spot) lights, when RT shadows are enabled.  |
| **- Reflection Forward** | The number of rays that were traced for reflection computations using forward shading. |
| **- Reflection Deferred** | The number of rays that were traced for reflection computations using deferred shading. |
| **- Diffuse GI Forward** | The number of rays that were traced for diffuse Global Illumination (GI) computations using forward shading. |
| **- Diffuse GI Deferred** | The number of rays that were traced for diffuse Global Illumination (GI) computations using deferred shading. |
| **- Recursive** | The number of rays that were traced for diffuse Global Illumination (GI) computations when recursive RT is enabled. |
| **- Total** | The total number of rays that were traced. |
| **Debug XR Layout** | Enable the checkbox to display XR passes debug informations.<br/>This mode is only available in the editor and development builds. |

<a name="MaterialPanel"></a>

## Material panel

The **Material** panel has tools that you can use to visualize different Material properties.

| **Debug Option**             | **Description**                                              |
| ---------------------------- | ------------------------------------------------------------ |
| **Common Material Property** | Use the drop-down to select a Material property to visualize on every GameObject on screen. All HDRP Materials share the properties available. |
| **Material**                 | Use the drop-down to select a Material property to visualize on every GameObject on screen using a specific Shader. The properties available depend on the HDRP Material type you select in the drop-down. |
| **Engine**                   | Use the drop-down to select a Material property to visualize on every GameObject on a screen that uses a specific Shader. The properties available are the same as **Material** but are in the form that the lighting engine uses them (for example, **Smoothness** is **Perceptual Roughness**). |
| **Attributes**               | Use the drop-down to select a 3D GameObject attribute, like Texture Coordinates or Vertex Color, to visualize on screen. |
| **Properties**               | Use the drop-down to select a property that the debugger uses to highlight GameObjects on screen. The debugger highlights GameObjects that use a Material with the property that you select. |
| **GBuffer**                  | Use the drop-down to select a property to visualize from the GBuffer for deferred Materials. |
| **Material Validator**       | Use the drop-down to select properties to show validation colors for.<br/>&#8226; **Diffuse Color**: Select this option to check if the diffuse colors in your Scene adheres to an acceptable [PBR](Glossary.md#PhysicallyBasedRendering) range. If the Material color is out of this range, the debugger displays it in the **Too High Color** color if it is above the range, or in the **Too Low Color** if it is below the range.<br/>&#8226; **Metal or SpecularColor**: Select this option to check if a pixel contains a metallic or specular color that adheres to an acceptable PBR range. If it does not, the debugger highlights it in the **Not A Pure Metal Color**.For information about the acceptable PBR ranges in Unity, see the [Material Charts documentation](https://docs.unity3d.com/Manual/StandardShaderMaterialCharts.html). |
| **- Too High Color**         | Use the color picker to select the color that the debugger displays when a Material's diffuse color is above the acceptable PBR range.<br/>This property only appears when you select **Diffuse Color** or **Metal or SpecularColor** from the **Material Validator** drop-down. |
| **- Too Low Color**          | Use the color picker to select the color that the debugger displays when a Material's diffuse color is below the acceptable PBR range.<br/>This property only appears when you select **Diffuse Color** or **Metal or SpecularColor** from the **Material Validator** drop-down. |
| **- Not A Pure Metal Color** | Use the color picker to select the color that the debugger displays if a pixel defined as metallic has a non-zero albedo value. The debugger only highlights these pixels if you enable the **True Metals** checkbox.<br/>This property only appears when you select **Diffuse Color** or **Metal or SpecularColor** from the **Material Validator** drop-down. |
| **- Pure Metals**            | Enable the checkbox to make the debugger highlight any pixels which Unity defines as metallic, but which have a non-zero albedo value. The debugger uses the **Not A Pure Metal Color** to highlight these pixels.<br/>This property only appears when you select **Diffuse Color** or **Metal or SpecularColor** from the **Material Validator** drop-down. |

If the geometry or the shading normal is denormalized, the view renders the target pixel red.

<a name="LightingPanel"></a>

## Lighting panel

The **Lighting** panel has tools that you can use to visualize various components of the lighting system in your Scene, like, shadowing and direct/indirect lighting.

| **Shadow Debug Option** | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Debug Mode**          | Use the drop-down to select which shadow debug information to overlay on the screen.<br/>&#8226;**None**: Select this mode to remove the shadow debug information from the screen.<br/>&#8226; **VisualizePunctualLightAtlas**: Select this mode to overlay the shadow atlas for [punctual Lights](Glossary.md#PunctualLight) in your Scene.<br/>&#8226; **VisualizeDirectionalLightAtlas**: Select this mode to overlay the shadow atlas for Directional Lights in your Scene.<br/>&#8226; **VisualizeAreaLightAtlas**: Select this mode to overlay the shadow atlas for area Lights in your Scene.<br/>&#8226; **VisualizeShadowMap**: Select this mode to overlay a single shadow map for a Light in your Scene.<br/>&#8226; **SingleShadow**: Select this mode to replace the Scene's lighting with a single Light. To select which Light to isolate, see **Use Selection** or **Shadow Map Index**. |
| **- Use Selection**     | Enable the checkbox to show the shadow map for the Light you select in the Scene.<br/>This property only appears when you select **VisualizeShadowMap** or **SingleShadow** from the **Shadow Debug Mode** drop-down. |
| **- Shadow Map Index**  | Use the slider to select the index of the shadow map to view. To use this property correctly, you must have at least one [Light](Light-Component.md) in your Scene that uses shadow maps. |
| **Global Scale Factor** | Use the slider to set the global scale that HDRP applies to the shadow rendering resolution. |
| **Clear Shadow Atlas**  | Enable the checkbox to clear the shadow atlas every frame.   |
| **Range Minimum Value** | Set the minimum shadow value to display in the various shadow debug overlays. |
| **Range Maximum Value** | Set the maximum shadow value to display in the various shadow debug overlays. |
| **Log Cached Shadow Atlas Status** | Press the button to log in the Console the list of lights placed in the the cached shadow atlas. |

| **Lighting Debug Option**             | **Description**                                              |
| ------------------------------------- | ------------------------------------------------------------ |
| **Show Lights By Type**               | Allows the user to enable or disable lights in the scene based on their type. |
| **- Directional Lights**              | Enable the checkbox to see Directional Lights in your Scene. Disable this checkbox to remove Directional Lights from your Scene's lighting. |
| **- Punctual Lights**                 | Enable the checkbox to see [Punctual Lights](Glossary.md#PunctualLight) in your Scene. Disable this checkbox to remove Punctual Lights from your Scene's lighting. |
| **- Area Lights**                     | Enable the checkbox to see Area Lights in your Scene. Disable this checkbox to remove Aera Lights from your Scene's lighting. |
| **- Reflection Probes**               | Enable the checkbox to see Reflection Probes in your Scene. Disable this checkbox to remove Reflection Probes from your Scene's lighting. |
| **Exposure**                          | Allows you to select an [Exposure](Override-Exposure.md) debug mode to use. |
| **- Debug Mode**                      | Use the drop-down to select a debug mode. See [Exposure](Override-Exposure.md#exposure-debug-modes) documentation for more information. |
| - **Show Tonemap curve**              | Enable the checkbox to overlay the tonemap curve to the histogram debug view.<br/>This property only appears when you select **HistogramView** from **Debug Mode**. |
| **- Center Around Exposure**          | Enable the checkbox to center the histogram around the current exposure value.<br/>This property only appears when you select **HistogramView** from **Debug Mode**. |
| **- Display RGB Histogram**           | Enable the checkbox to display the Final Image Histogram as an RGB histogram instead of just luminance.<br />This property only appears when you select **FinalImageHistogramView** from **Debug Mode**. |
| **- Display Mask Only**               | Enable the checkbox to display only the metering mask in the picture-in-picture.When disabled, the mask displays after weighting the scene color instead. <br />This property only appears when you select **MeteringWeighted** from **Debug Mode**. |
| **- Debug Exposure Compensation**     | Set an additional exposure compensation for debug purposes.  |
| **Debug Mode**                        | Use the drop-down to select a lighting mode to debug. For example, you can visualize diffuse lighting, specular lighting, direct diffuse lighting, direct specular lighting, indirect diffuse lighting, indirect specular lighting, emissive lighting and Directional Light shadow cascades. |
| **Hierarchy Debug Mode**              | Use the drop-down to select a light type to show the direct lighting for or a Reflection Probe type to show the indirect lighting for. |
| **Light Layers Visualization**        | Enable the checkbox to visualize light layers of objects in your Scene. |
| **- Use Selected Light**              | Enable the checkbox to visualize objects affected by the selected light. |
| **- Switch to Light's Shadow Layers** | Enable the checkbox to visualize objects casting shadows for the selected light. |
| **- Filter Layers**                   | Use the drop-down to filter light layers that you want to visialize. Objects having a matching layer will be displayed in a specific color. |
| **- Layers Color**                    | Use the color pickers to select the display color of each light layer. |

| **Material Overrides**               | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Override Smoothness**              | Enable the checkbox to override the smoothness for the entire Scene. |
| **- Smoothness**                     | Use the slider to set the smoothness override value that HDRP uses for the entire Scene. |
| **Override Albedo**                  | Enable the checkbox to override the albedo for the entire Scene. |
| **- Albedo**                         | Use the color picker to set the albedo color that HDRP uses for the entire Scene. |
| **Override Normal**                  | Enable the checkbox to override the normals for the entire Scene with object normals for lighting debug. |
| **Override Specular Color**          | Enable the checkbox to override the specular color for the entire Scene. |
| **- Specular Color**                 | Use the color picker to set the specular color that HDRP uses for the entire Scene. |
| **Override Emissive Color**          | Enable the checkbox to override the emissive color for the entire Scene. |
| **- Emissive Color**                 | Use the color picker to set the emissive color that HDRP uses for the entire Scene. |

| **Debug Option**                     | **Description**                                              |
| ------------------------------------ | ------------------------------------------------------------ |
| **Fullscreen Debug Mode**            | Use the drop-down to select a fullscreen lighting effect to debug. For example, you can visualize [Contact Shadows](Override-Contact-Shadows.md), the depth pyramid, and indirect diffuse lighting. |
| **Tile/Cluster Debug**               | Use the drop-down to select an internal HDRP lighting structure to visualize on screen.<br/>&#8226; **None**: Select this option to turn off this debug feature.<br/>&#8226; **Tile**: Select this option to show an overlay of each lighting tile, and the number of lights in them.<br/>&#8226; **Cluster**: Select this option to show an overlay of each lighting cluster that intersects opaque geometry, and the number of lights in them.<br/>&#8226; **Material Feature Variants**: Select this option to show the index of the lighting Shader variant that HDRP uses for a tile. You can find variant descriptions in the *lit.hlsl* file. |
| **- Tile/Cluster Debug By Category** | Use the drop-down to select the Light type that you want to show the Tile/Cluster debug information for. The options include [Light Types](Light-Component.md), [Decals](Decal-Projector.md), and [Density Volumes](Density-Volume.md).<br/>This property only appears when you select **Tile** or **Cluster** from the **Tile/Cluster Debug** drop-down. |
| **- Cluster Debug Mode** | Use the drop-down to select the visualization mode for the cluster. The options are:<br/> **VisualizeOpaque**: Shows cluster information on opaque geometry.<br/> **VisualizeSlice**: Shows cluster information at a set distance from the camera.<br/>This property only appears when you select **Cluster** from the **Tile/Cluster Debug** drop-down.. |
| **- Cluster Distance** | Use this slider to set the distance from the camera at which to display the cluster slice. This property only appears when you select **VisualizeSlice** from the **Cluster Debug Mode** drop-down. |
| **Display Sky Reflection**           | Enable the checkbox to display an overlay of the cube map that the current sky generates and HDRP uses for lighting. |
| **- Sky Reflection Mipmap**          | Use the slider to set the mipmap level of the sky reflection cubemap. Use this to view the sky reflection cubemap's different mipmap levels.<br/>This property only appears when you enable the **Display Sky Reflection** checkbox. |
| **Display Light Volumes**            | Enable the checkbox to show an overlay of all light bounding volumes. |
| **- Light Volume Debug Type**        | Use the drop-down to select the method HDRP uses to display the light volumes.<br/>&#8226; **Gradient**: Select this option to display the light volumes as a gradient.<br/>&#8226; **ColorAndEdge**: Select this option to display the light volumes as a plain color (a different color for each Light Type) with a red border for readability.<br/>This property only appears when you enable the **Display Light Volumes** checkbox. |
| **- Max Debug Light Count**          | Use the slider to rescale the gradient. Lower this value to make the screen turn red faster. Use this property to change the maximum acceptable number of lights for your application and still see areas in red.<br/>This property only appears when you set the **Display Light Volumes** mode to **Gradient**. |
| **Display Cookie Atlas**             | Enable the checkbox to display an overlay of the cookie atlas. |
| **- Mip Level**                      | Use the slider to set the mipmap level of the cookie atlas.<br/>This property only appears when you enable the **Display Cookie Atlas** checkbox. |
| **- Clear Cookie Atlas**             | Enable the checkbox to clear the cookie atlas at each frame.<br/>This property only appears when you enable the **Display Cookie Atlas** checkbox. |
| **Display Planar Reflection Atlas**  | Enable the checkbox to display an overlay of the planar reflection atlas. |
| **- Mip Level**                      | Use the slider to set the mipmap level of the planar reflection atlas.<br/>This property only appears when you enable the **Display Planar Reflection Atlas** checkbox. |
| **- Clear Planar Atlas**             | Enable the checkbox to clear the planar reflection atlas at each frame.<br/>This property only appears when you enable the **Display Planar Reflection Atlas** checkbox. |
| **Debug Overlay Screen Ratio**       | Set the size of the debug overlay textures with a ratio of the screen size. The default value is 0.33 which is 33% of the screen size. |

<a name="VolumePanel"></a>

## Volume panel

The **Volume** panel has tools that you can use to visualize the Volume Components affecting a camera.

| **Debug Option**       | **Description**                                      |
| ---------------------- | ---------------------------------------------------- |
| **Component**          | Use the drop-down to select which volume component to visualize. |
| **Camera**             | Use the drop-down to select which camera to use as volume anchor. |
| **Parameter**          | List of parameters for the selected component. |
| **Interpolated Value** | Current value affecting the choosen camera for each parameter. |
| **Other columns**      | Each one of the remaining columns display the parameter values of a volume affecting the selected **Camera**. They are sorted from left to right by decreasing influence. |

<a name="RenderingPanel"></a>

## Rendering panel

The **Rendering** panel has tools that you can use to visualize various HDRP rendering features.

| **Debug Option**              | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Fullscreen Debug Mode**     | Use the drop-down to select a rendering mode to display as an overlay on the screen.<br/>&#8226; **Motion Vectors**: Select this option to display motion vectors. Note that object motion vectors are not visible in the Scene view.<br/>&#8226; **NaN Tracker**: Select this option to display an overlay that highlights [NaN](https://en.wikipedia.org/wiki/NaN) values.<br/>&#8226; **ColorLog**: Select this option to show how the raw, log-encoded buffer looks before color grading takes place.<br/>&#8226; **DepthOfFieldCoc**: Select this option to display the circle of confusion for the depth of field effect. The circle of confusion shows how much the depth of field effect blurs a given pixel/area.<br/>&#8226; **Quad Overdraw**: Select this option to display an overlay that highlights gpu quads running multiple fragment shaders. This is mainly caused by small or thin triangles. Use LODs to reduce the amount of overdraw when objects are far away. (This mode is currently not supported on Metal and PS4).<br/>&#8226; **Vertex Density**: Select this option to display an overlay that highlights pixels running multiple vertex shaders. A vertex can be run multiple times when part of different triangles. This helps finding models that need LODs. (This mode is currently not supported on Metal).<br/>&#8226; **TransparencyOverdraw**: Select this option to view the number of transparent pixels that draw over one another. This represents the amount of on-screen overlapping of transparent pixel. This is useful to see the amount of pixel overdraw for transparent GameObjects from different points of view in the Scene. This debug option displays each pixel as a heat map going from black (which represents no transparent pixels) through blue to red (at which there are **Max Pixel Cost** number of transparent pixels). <br/>&#8226; **RequestedVirtualTextureTiles**: Select this option to display what texture tile each pixel uses. Pixels that this debug view renders with the same color request the same texture tile to be streamed into video memory by the streaming virtual texturing system. This debug view is useful to see which areas of the screen use textures that the virtual texturing system steams into video memory. It can help to identify issues with the virtual texture streaming system. |
| - **Max Pixel Cost**          | The scale of the transparency overdraw heat map. For example, a value of 10 displays a red pixel if 10 transparent pixels overlap. Any number of overdraw above this value also displays as red.<br/>This property only appears if you set **Fullscreen Debug Mode** to **TransparencyOverdraw**. |
| **MipMaps**                   | Use the drop-down to select a mipmap streaming property to debug.<br/>&#8226; **None**: Select this option to disable this debug feature.<br/>&#8226; **MipRatio**: Select this option to display a heat map of pixel to texel ratio. A blue tint represents areas with too little Texture detail (the Texture is too small). A bed tint represents areas with too much Texture detail (the Texture is too large for the screen area). If the debugger shows the original colour for a pixel, this means that the level of detail is just right.<br/>&#8226; **MipCount**: Select this option to display mip count as grayscale from black to white as the number of mips increases (for up to 14 mips, or 16K size). Red inidates Textures with more than 14 mips. Magenta indicates Textures with 0 mips or that the Shader does not support mip count.<br/>&#8226; **MipCountReduction**: Select this option to display the difference between the current mip count and the original mip count as a green scale. A brighter green represents a larger reduction (that mip streaming saves more Texture memory). Magenta means that the debugger does not know the original mip count.<br/>&#8226; **StreamingMipBudget**: Select this option to display the mip status due to streaming budget. Green means that streaming Textures saves some memory. Red means that mip levels are lower than is optimal, due to full Texture memory budget. White means that streaming Textures saves no memory.<br/>&#8226; **StreamingMip**: Select this option to display the same information as **StreamingMipBudget**, but to apply the colors to the original Textures. |
| **- Terrain Texture**         | Use the drop-down to select the terrain Texture to debug the mipmap for. This property only appears when you select an option other than **None** from the **MipMaps** drop-down. |

| **Color Picker**      | **Description**                                              |
| --------------------- | ------------------------------------------------------------ |
| **Debug Mode**        | Use the drop-down to select the format of the color picker display. |
| **Font Color**        | Use the color picker to select a color for the font that the Color Picker uses for its display. |

The **Color Picker** works with whichever debug mode HDRP displays at the time. This means that you can see the values of various components of the rendering like Albedo or Diffuse Lighting. By default, this displays the value of the main High Dynamic Range (HDR) color buffer.

| **Debug Option**              | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **False Color Mode**          | Enable the checkbox to define intensity ranges that the debugger uses to show a color temperature gradient for the current frame. The color temperature gradient goes from blue, to green, to yellow, to red. |
| **- Range Threshold 0**       | Set the first split for the intensity range.<br/>This property only appears when you enable the **False Color Mode** checkbox. |
| **- Range Threshold 1**       | Set the second split for the intensity range.<br/>This property only appears when you enable the **False Color Mode** checkbox. |
| **- Range Threshold 2**       | Set the third split for the intensity range.<br/>This property only appears when you enable the **False Color Mode** checkbox. |
| **- Range Threshold 3**       | Set the final split for the intensity range.<br/>This property only appears when you enable the **False Color Mode** checkbox. |
| **MSAA Samples**              | Use the drop-down to select the number of samples the debugger uses for [MSAA](Anti-Aliasing.md#MSAA). |
| **Freeze Camera for Culling** | Use the drop-down to select a Camera to freeze in order to check its culling. To check if the Camera's culling works correctly, freeze the Camera and move occluders around it. |
| **Enable Render Graph**       | Enable the checkbox to use the Render Graph for rendering. |

<a name="CameraPanel"></a>

## Camera panels

In the **Render Pipeline Debug** window , each active Camera in the Scene has its own debug window. Use the Camera's debug window to temporarily change that Camera's [Frame Settings](Frame-Settings.md) without altering the Camera data in the Scene. The Camera window helps you to understand why a specific feature does not work correctly. You can access all of the information that HDRP uses the render the Camera you select.

**Note**: The Camera debug window is only available for Cameras, not Reflection Probes.

The following columns are available for each Frame Setting:

| **Column**     | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Debug**      | Displays Frame Setting values you can modify for the selected Camera. You can use these to temporarily alter the Camera’s Frame Settings for debugging purposes. You cannot enable Frame Setting features that your HDRP Asset does not support. |
| **Sanitized**  | Displays the Frame Setting values that the selected Camera uses after Unity checks to see if your HDRP Asset supports them. |
| **Overridden** | Displays the Frame Setting values that the selected Camera overrides. If you do not check the **Custom Frame Settings** checkbox, check it and do not override any settings, this column is identical to the **Default** column. |
| **Default**    | Displays the default Frame Setting values in your current [HDRP Asset](HDRP-Asset.md). |

Unity processes **Sanitized**, **Overridden**, and **Default** in a specific order. First it checks the **Default** Frame Settings, then checks the selected Camera’s **Overridden** Frame Settings. Finally, it checks whether the HDRP Asset supports the selected Camera’s Frame Settings and displays that result in the **Sanitized** column.

### Interpreting the Camera window

![](Images/RenderPipelineDebug2.png)

- In the image above, the **Light Layers** checkbox is disabled at the **Sanitized** step. This means that, although **Light Layers** is enabled in the Frame Settings this Camera uses, it is not enabled in the HDRP Asset’s **Render Pipeline Supported Features**.
- Also in the image above, the **Decals** checkbox is disabled at the **Overridden** step. This means that **Decals** is enabled in the default Camera Frame Settings and then **Decals** is disabled for that specific Camera’s **Custom Frame Settings**.
