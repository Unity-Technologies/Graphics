# Rendering Debugger

The **Rendering Debugger** window lets you visualize various lighting, rendering, and Material properties. The visualizations help you identify rendering issues and optimize Scenes and rendering configurations.

This section contains the following topics:

* [How to access the Rendering Debugger](#how-to-access).

    Information on how to access the **Rendering Debugger** window in the Editor, in the Play mode, and at runtime in Development builds.

* [Navigation at runtime](#navigation-at-runtime)

    How to navigate the **Rendering Debugger** interface at runtime.

* [Rendering Debugger window sections](#ui-sections)

    Descriptions of the elements and properties in the **Rendering Debugger** window.

## <a name="how-to-access"></a>How to access the Rendering Debugger

The Rendering Debugger window is available in the following modes:

| Mode       | Platform       | Availability                   | How to Open the Rendering Debugger |
| ---------- | -------------- | ------------------------------ | ---------------------------------- |
| Editor     | All            | Yes (window in the Editor)     | Select **Window > Analysis > Rendering Debugger** |
| Play mode  | All            | Yes (overlay in the Game view) | On a desktop or laptop computer, press **LeftCtrl+Backspace** (**LeftCtrl+Delete** on macOS)<br>On a console controller, press L3 and R3 (Left Stick and Right Stick) |
| Runtime    | Desktop/Laptop | Yes (only in Development builds) | Press **LeftCtrl+Backspace** (**LeftCtrl+Delete** on macOS) |
| Runtime    | Console        | Yes (only in Development builds) | Press L3 and R3 (Left Stick and Right Stick) |
| Runtime    | Mobile         | Yes (only in Development builds) | Use a three-finger double tap |

To enable all the sections of the **Rendering Debugger** in your built application, disable **Strip Debug Variants** in **Project Settings > Graphics > URP Global Settings**. Otherwise, you can only use the [Display Stats](#display-stats) section.

To disable the runtime UI, use the [enableRuntimeUI](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.DebugManager.html#UnityEngine_Rendering_DebugManager_enableRuntimeUI) property.

>[!NOTE]
> When using the **Rendering Debugger** window in the Development build, clear the **Strip Debug Variants** check box in **Project Settings > Graphics > URP Global Settings**.

## <a name="navigation-at-runtime"></a>Navigation at runtime

### Keyboard

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the arrow keys                                                                        |
| **Change the current tab**                         | Use the Page up and Page down keys (Fn + Up and Fn + Down keys respectively for MacOS)    |
| **Display the current active item independently of the debug window** | Press the right Shift key                                              |

### Xbox Controller

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the Directional pad (D-Pad)                                                           |
| **Change the current tab**                         | Use the Left Bumper and Right Bumper                                                      |
| **Display the current active item independently of the debug window** | Press the X button                                                     |

### PlayStation Controller

| Action                                             | Control                                                                                   |
|----------------------------------------------------|-------------------------------------------------------------------------------------------|
| **Change the current active item**                 | Use the Directional buttons                                                               |
| **Change the current tab**                         | Use the L1 button and R1 button                                                           |
| **Display the current active item independently of the debug window** | Press the Square button                                                |

## <a name="ui-sections"></a>Rendering Debugger window sections

The **Rendering Debugger** window contains the following sections:

* [Frequently Used](#frequently-used)

* [Material](#material)

* [Lighting](#lighting)

* [Rendering](#rendering)

The following illustration shows the Rendering Debugger window in the Scene view.

![Rendering Debugger window.](../Images/rendering-debugger/rendering-debugger-ui-sections.png)

### Frequently Used

This section contains a selection of properties that users use often. The properties are from the other sections in the Rendering Debugger window. For information about the properties, refer to the sections [Material](#material), [Lighting](#lighting), and [Rendering](#rendering).

### Material

The properties in this section let you visualize different Material properties.

#### Material Filters

| **Property** | **Description** |
| --- | --- |
| **Material&#160;Override** | Select a Material property to visualize on every GameObject on screen.<br/>The available options are:<ul><li>Albedo</li><li>Specular</li><li>Alpha</li><li>Smoothness</li><li>AmbientOcclusion</li><li>Emission</li><li>NormalWorldSpace</li><li>NormalTangentSpace</li><li>LightingComplexity</li><li>Metallic</li><li>SpriteMask</li></ul>With the **LightingComplexity** value selected, Unity shows how many Lights affect areas of the screen space. |
| **Vertex&#160;Attribute** | Select a vertex attribute of GameObjects to visualize on screen.<br/>The available options are:<ul><li>Texcoord0</li><li>Texcoord1</li><li>Texcoord2</li><li>Texcoord3</li><li>Color</li><li>Tangent</li><li>Normal</li></ul> |

#### Material Validation

| **Property** | **Description** |
| --- | --- |
| **Material&#160;Validation Mode** | Select which Material properties to visualize: Albedo, or Metallic. Selecting one of the properties shows the new context menu. |
| &#160;&#160;&#160;&#160;**Validation&#160;Mode:&#160;Albedo** | Selecting **Albedo** in the Material&#160;Validation Mode property shows the **Albedo Settings** section with the following properties:<br>**Validation&#160;Preset**: Select a pre-configured material, or Default Luminance to visualize luminance ranges.<br/>**Min Luminance**: Unity draws pixels where the luminance is lower than this value with red color.<br/>**Max Luminance**: Unity draws pixels where the luminance is higher than this value with blue color.<br/>**Hue Tolerance**: available only when you select a pre-set material. Unity adds the hue tolerance to the minimum and maximum luminance values.<br/>**Saturation Tolerance**: available only when you select a pre-set material. Unity adds the saturation tolerance to the minimum and maximum luminance values. |
| &#160;&#160;&#160;&#160;**Validation&#160;Mode:&#160;Metallic** | Selecting **Metallic** in the Material&#160;Validation Mode property shows the **Metallic Settings** section with the following properties:<br>**Min Value**: Unity draws pixels where the metallic value is lower than this value with red color.<br>**Max Value**: Unity draws pixels where the metallic value is higher than this value with blue color. |

### Lighting

The properties in this section let you visualize different settings and elements related to the lighting system, such as shadow cascades, reflections, contributions of the Main and the Additional Lights, and so on.

#### Lighting Debug Modes

| **Property**            | **Description**                                              |
| ----------------------- | ------------------------------------------------------------ |
| **Lighting Debug Mode** | Specifies which lighting and shadow information to overlay on-screen to debug. The options are:<ul><li>**None**: Renders the scene normally without a debug overlay.</li><li>**Shadow Cascades**: Overlays shadow cascade information so you can see which shadow cascade each pixel uses. Use this to debug shadow cascade distances. For information on which color represents which shadow cascade, refer to [Shadows section of the URP Asset](../universalrp-asset.md#shadows).</li><li>**Lighting Without Normal Maps**: Renders the scene to visualize lighting. This mode uses neutral materials and disables normal maps. This and the **Lighting With Normal Maps** mode are useful for debugging lighting issues caused by normal maps.</li><li>**Lighting With Normal Maps**: Renders the scene to visualize lighting. This mode uses neutral materials and allows normal maps.</li><li>**Reflections**: Renders the scene to visualize reflections. This mode applies perfectly smooth, reflective materials to every Mesh Renderer.</li><li>**Reflections With Smoothness**: Renders the scene to visualize reflections. This mode applies reflective materials without an overridden smoothness to every GameObject.</li></ul> |
| **Lighting Features**   | Specifies flags for which lighting features contribute to the final lighting result. Use this to view and debug specific lighting features in your scene. The options are:<ul><li>**Nothing**: Shortcut to disable all flags.</li><li>**Everything**: Shortcut to enable all flags.</li><li>**Global Illumination**: Indicates whether to render [global illumination](https://docs.unity3d.com/Manual/realtime-gi-using-enlighten.html).</li><li>**Main Light**: Indicates whether the main directional [Light](../light-component.md) contributes to lighting.</li><li>**Additional Lights**: Indicates whether lights other than the main directional light contribute to lighting.</li><li>**Vertex Lighting**: Indicates whether additional lights that use per-vertex lighting contribute to lighting.</li><li>**Emission**: Indicates whether [emissive](https://docs.unity3d.com/Manual/StandardShaderMaterialParameterEmission.html) materials contribute to lighting.</li><li>**Ambient Occlusion**: Indicates whether [ambient occlusion](../post-processing-ssao.md) contributes to lighting.</li></ul> |

### Rendering

The properties in this section let you visualize different rendering features.

#### Rendering Debug

| **Property**                   | **Description**                                              |
| ------------------------------ | ------------------------------------------------------------ |
| **Map Overlays**               | Specifies which render pipeline texture to overlay on the screen. The options are:<ul><li>**None**: Renders the scene normally without a texture overlay.</li><li>**Depth**: Overlays the camera's depth texture on the screen.</li><li>**Additional Lights Shadow Map**: Overlays the [shadow map](https://docs.unity3d.com/Manual/shadow-mapping.html) that contains shadows cast by lights other than the main directional light.</li><li>**Main Light Shadow Map**: Overlays the shadow map that contains shadows cast by the main directional light.</li></ul> |
| **&nbsp;&nbsp;Map Size**       | The width and height of the overlay texture as a percentage of the view window URP displays it in. For example, a value of **50** fills up a quarter of the screen (50% of the width and 50% of the height). |
| **HDR**                        | Indicates whether to use [high dynamic range (HDR)](https://docs.unity3d.com/Manual/HDR.html) to render the scene. Enabling this property only has an effect if you enable **HDR** in your URP Asset. |
| **MSAA**                       | Indicates whether to use [Multisample Anti-aliasing](anti-aliasing.md#msaa) to render the scene. Enabling this property only has an effect if:<ul><li>You set **Anti Aliasing (MSAA)** to a value other than **Disabled** in your URP Asset.</li><li>You use the Game View. MSAA has no effect in the Scene View.</li></ul> |
| **Post-processing**            | Specifies how URP applies post-processing. The options are:<ul><li>**Disabled**: Disables post-processing.</li><li>**Auto**: Unity enables or disables post-processing depending on the currently active debug modes. If color changes from post-processing would change the meaning of a debug mode's pixel, Unity disables post-processing. If no debug modes are active, or if color changes from post-processing don't change the meaning of the active debug modes' pixels, Unity enables post-processing.</li><li>**Enabled**: Applies post-processing to the image that the camera captures.</li></ul> |
| **Additional Wireframe Modes** | Specifies whether and how to render wireframes for meshes in your scene. The options are:<ul><li>**None**: Doesn't render wireframes.</li><li>**Wireframe**: Exclusively renders edges for meshes in your scene. In this mode, you can see the wireframe for meshes through the wireframe for closer meshes.</li><li>**Solid Wireframe**: Exclusively renders edges and faces for meshes in your scene. In this mode, the faces of each wireframe mesh hide edges behind them.</li><li>**Shaded Wireframe**: Renders edges for meshes as an overlay. In this mode, Unity renders the scene in color and overlays the wireframe over the top.</li></ul> |
| **Overdraw**                   | Indicates whether to render the overdraw debug view. This is useful to see where Unity draws pixels over one other. |

#### Pixel Validation

![](../Images/rendering-debugger/pixel-validation.png)<br/>*The Pixel Validation subsection.*

| **Property**                     | **Description**                                              |
| -------------------------------- | ------------------------------------------------------------ |
| **Pixel Validation Mode**        | Specifies which mode Unity uses to validate pixel color values. The options are:<ul><li>**None**: Renders the scene normally and doesn't validate any pixels.</li><li>**Highlight NaN, Inf and Negative Values**: Highlights pixels that have color values that are NaN, Inf, or negative.</li><li>**Highlight Values Outside Range**: Highlights pixels that have color values outside a particular range. Use **Value Range Min** and **Value Range Max**.</li></ul> |
| **&nbsp;&nbsp;Channels**         | Specifies which value to use for the pixel value range validation. The options are:<ul><li>**RGB**: Validates the pixel using the luminance value calculated from the red, green, and blue color channels.</li><li>**R**: Validates the pixel using the value from the red color channel.</li><li>**G**: Validates the pixel using the value from the green color channel.</li><li>**B**: Validates the pixel using the value from the blue color channel.</li><li>**A**: Validates the pixel using the value from the alpha channel.</li></ul>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |
| **&nbsp;&nbsp; Value Range Min** | The minimum valid color value. Unity highlights color values that are less than this value.<br/><br/>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |
| **&nbsp;&nbsp; Value Range Max** | The maximum valid color value. Unity highlights color values that are greater than this value.<br/><br/>This property only appears if you set **Pixel Validation Mode** to **Highlight Values Outside Range**. |
