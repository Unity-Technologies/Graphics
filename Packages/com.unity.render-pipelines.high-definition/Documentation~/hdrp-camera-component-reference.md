# HDRP camera component reference

The High Definition Render Pipeline (HDRP) adds extra properties and methods to Unity's [standard Camera](https://docs.unity3d.com/ScriptReference/Camera.html) to control HDRP features, such as [Frame Settings](Frame-Settings.md). Although HDRP displays these extra properties in the Camera component Inspector, HDRP stores them in the [HDAdditionalCameraData](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/api/UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.html) component. This means if you use a script to access properties or methods for the Camera, be aware that they may be inside the HDAdditionalCameraData component. For the full list of properties and methods HDRP stores in the HDAdditionalCameraData component, see the [scripting API](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/api/UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.html).

## Properties

The HDRP Camera shares many properties with Unity's [standard Camera](https://docs.unity3d.com/Manual/class-Camera.html).

The Camera Inspector includes the following groups of properties:

* [Projection](#Projection)
    * [Physical Camera](#PhysicalCamera)
* [Rendering](#Rendering)
* [Environment](#Environment)
* [Output](#Output)

<a name="Projection"></a>
### Projection

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Projection**                  | Use the drop-down to select the projection mode for the Camera.<br />&#8226; **Perspective**: The Camera simulates perspective when it renders GameObjects. This means that GameObjects further from the Camera appear smaller than GameObjects that are closer.<br />&#8226; **Orthographic**: The Camera renders GameObjects uniformly with no perspective. This means that GameObjects further from the Camera appear to be the same size as GameObjects that are closer. Currently, HDRP doesn't support this projection mode. If you select this projection mode, any HDRP feature that requires lighting doesn't work consistently. This also applies in the Scene view when the Scene view Camera uses orthographic (isometric) projection mode. However, this projection mode does work consistently with [Unlit](unlit-material.md) Materials. |
| **Size**                        | Set the size of the orthographic Camera.<br/>This property only appears when you select **Orthographic** from the **Projection** drop-down. |
| **Field of View Axis**          | Use the drop-down to select the axis that you want the field of view to relate to.<br />&#8226; **Vertical**: Allows you to set the **Field of View** using the vertical axis.<br />&#8226; **Horizontal**: Allows you to set the **Field of View** using the horizontal axis. This property only appears when you select **Perspective** from the **Projection** drop-down. |
| **Field of View**               | Use the slider to set the viewing angle for the Camera, in degrees.<br />This property only appears when you select **Perspective** from the **Projection** drop-down. |
| **Physical Camera**             | Enable the checkbox to make the Camera use its **Physical Settings** to calculate its viewing angle. This property only appears when you select **Perspective** from the **Projection** drop-down. |
| **Clipping Planes**             | Set the distances from the Camera at which Unity uses it to start and stop rendering GameObjects.<br />&#8226; **Near**: The distance from the Camera at which Unity begins to use it to draw GameObjects. The Camera doesn't render anything that's closer to it than this distance.<br />&#8226; **Far**: The distance from the Camera at which Unity ceases to use it to draw GameObjects. The Camera doesn't render anything that's further away from it than this distance. |

<a name="PhysicalCamera"></a>
### Physical Camera

|    **Property** ||**Description**|
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Camera Body** ||
| |**Sensor Type**     | Use the drop-down to select the real-world camera format that you want the Camera to simulate. When you select a Camera **Sensor Type**, Unity automatically sets the **Sensor Size** to the correct values for that format. If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**. |
| |**Sensor Size**    | Set the size, in millimeters, of the real-world camera sensor. Unity sets the **X** and **Y** values automatically when you select the **Sensor Type**. You can enter custom values to fine-tune your sensor. |
| |**ISO**           | Set the sensibility of the real-world camera sensor. Higher values increase the Camera's sensitivity to light and result in faster exposure times. This property affects [Exposure](Override-Exposure.md) if you set its **Mode** to **Use Physical Camera**. |
| |**Shutter Speed**   | Set the exposure time for the camera. Lower values result in less exposed pictures. Use the drop-down to select the units for the exposure time. You can use **Seconds** or **1/Seconds**. This property affects [Exposure](Override-Exposure.md) if you set its **Mode** to **Use Physical Camera**. |
| |**Gate Fit**      | Use the drop-down to select the method that Unity uses to set the size of the resolution gate (aspect ratio of the device you run the application on) relative to the film gate (aspect ratio of the Physical Camera sensor).   **Vertical**: Fits the resolution gate to the height of the film gate. If the sensor aspect ratio is larger than the device aspect ratio, Unity crops the rendered image at the sides. If the sensor aspect ratio is smaller than the device aspect ratio, Unity overscans the rendered image at the sides. If you select this method, changing the sensor width (**Sensor Size** > **X** property) has no effect on the rendered image.<br />&#8226; **Horizontal**: Fits the resolution gate to the width of the film gate. If the sensor aspect ratio is larger than the device aspect ratio, Unity overscans the rendered image on the top and bottom. If the sensor aspect ratio is smaller than the device aspect ratio, Unity crops the rendered image on the top and bottom. If you select this method, changing the sensor height (**Sensor Size** > **Y** property) has no effect on the rendered image.<br />&#8226; **Fill**: Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image.<br />&#8226; **Overscan**: Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image.<br />&#8226; **None**: Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the device aspect ratio. |
| **Lens** ||
| |**Focal Length**    | Set the distance, in millimeters, between the Camera sensor and the Camera lens. Lower values result in a wider **Field of View**, and vice versa. This property affects [Depth of Field](Post-Processing-Depth-of-Field.md) if you set its **Focus Mode** to **Use Physical Camera**. |
| |**Shift**       | Set the horizontal and vertical shift from the center. Values are multiples of the sensor size; for example, a shift of 0.5 along the **X** axis offsets the sensor by half its horizontal size. You can use lens shifts to correct distortion that occurs when the Camera is at an angle to the subject (for example, converging parallel lines).  Shift the lens along either axis to make the Camera frustum [oblique](https://docs.unity3d.com/Manual/ObliqueFrustum.html). |
| |**Aperture**        | Use the slider to set the ratio of the f-stop or [f-number](Glossary.md#f-number) aperture. The smaller the value is, the shallower the depth of field is and more light reaches the sensor. This property affects [Depth of Field](Post-Processing-Depth-of-Field.md) if you set its **Focus Mode** to **Use Physical Camera**. This property also affects [Exposure](Override-Exposure.md) if you set its **Mode** to **Use Physical Camera**. |
|| **Focus Distance**     | Sets the distance of the focus plane from the Camera. This property is only used in DoF computations if the **Focus Distance Mode** in the [Depth of Field](Post-Processing-Depth-of-Field.md) volume component is set to **Camera**. |
| |**Blade Count**     | Use the slider to set the number of diaphragm blades the Camera uses to form the aperture. This property affects the look of the [Depth of Field](Post-Processing-Depth-of-Field.md) [bokeh](Glossary.md#Bokeh). |
| |**Curvature**      | Use the remapper to map an aperture range to blade curvature. Aperture blades become more visible on bokeh at higher aperture values. Tweak this range to define how the bokeh looks at a given aperture. The minimum value results in fully-curved, perfectly-circular bokeh, and the maximum value results in fully-shaped bokeh with visible aperture blades. This property affects the look of the [Depth of Field](Post-Processing-Depth-of-Field.md) bokeh. |
| |**Barrel Clipping** | Use the slider to set the strength of the “cat eye” effect. You can see this effect on bokeh as a result of lens shadowing (distortion along the edges of the frame). This property affects the look of the [Depth of Field](Post-Processing-Depth-of-Field.md) bokeh. |
| |**Anamorphism**    | Use the slider to stretch the sensor to simulate an anamorphic look. Positive values distort the Camera vertically, negative values distort the Camera horizontally. This property affects the look of the [Depth of Field](Post-Processing-Depth-of-Field.md) bokeh and the [Bloom](Post-Processing-Bloom.md) effect if you enable its *Anamorphic* property. |


<a name="Rendering"></a>
## Rendering

| **Property** | **Description** |
|-|-|
| **HDRP Dynamic Resolution**    | Enable the checkbox to make this Camera support dynamic resolution for buffers linked to it. |
| **Allow DLSS**                  | Enables NVIDIA Deep Learning Super Sampling (DLSS). This property has an effect only if you add DLSS to your [HDRP Asset](HDRP-Asset.md). For more information, refer to [DLSS settings](#dlss-settings). |
| **Allow FSR2** | Enables AMD FidelityFX Super Resolution 2.0 (FSR2). This property has an effect only if you add FSR2 to your [HDRP Asset](HDRP-Asset.md). For more information, refer to [FSR2 settings](#fsr2-settings). |
| **Override FSR Sharpness** | Enables an **FSR Sharpness** slider that lets you set the sharpness of the FidelityFX Super Resolution 1.0 (FSR1) upscale filter. A value of 1.0 means maximum sharpness. A value of 0 means no sharpening. This property has an effect only if you set **Default Upscale Filter** to **FSR1** in your [HDRP Asset](HDRP-Asset.md). |
| **Post Anti-aliasing**        | This Camera can use [multisample anti-aliasing (MSAA)](Anti-Aliasing.md#MSAA), at the same time as post-process anti-aliasing. This is because MSAA is a hardware feature. To control post-process anti-aliasing, use the [Frame Settings](Frame-Settings.md).<br />&#8226; **No Anti-aliasing**: This Camera processes MSAA but doesn't perform any post-process anti-aliasing. <br/>&#8226; **Fast Approximate Anti-aliasing (FXAA)**: Smooths edges on a per-pixel level. This is the most efficient anti-aliasing technique in HDRP.<br />&#8226; **Temporal Anti-aliasing (TAA)**: Uses frames from a history buffer to smooth edges more effectively than fast approximate anti-aliasing.<br />&#8226; **Subpixel Morphological Anti-aliasing (SMAA)**: Finds patterns in borders of the image and blends the pixels on these borders according to the pattern. |
| **Dithering**                   | Enable the checkbox to apply 8-bit dithering to the final render. This can help reduce banding on wide gradients and low light areas. |
| **Stop NaNs**                   | Enable the checkbox to make this Camera replace values that aren't a number (NaN) with a black pixel. This stops certain effects from breaking, but is a resource-intensive process. Only enable this feature if you experience NaN issues that you can't fix. |
| **Culling Mask**                | Use the drop-down to set the Layer Mask that the Camera uses to exclude GameObjects from the rendering process. The Camera only renders Layers that you include in the Layer Mask. |
| **Occlusion Culling**           | Enable the checkbox to make this Camera not render GameObjects that aren't currently visible. For more information, refer to the [Occlusion Culling documentation](<https://docs.unity3d.com/Manual/OcclusionCulling.html>). |
| **Exposure Target** | The GameObject to center the [Auto Exposure](Override-Exposure.md) procedural mask around. |

<a name="dlss-settings"></a>
### DLSS settings

The following properties are available only if you enable **Allow DLSS**.

| **Property** | **Description** |
|-|-|
| **Use DLSS Custom Quality**        | Indicates whether this Camera overrides the DLSS quality mode specified in the [HDRP Asset](HDRP-Asset.md). |
| **DLSS Mode** | Sets whether DLSS prioritizes quality or performance. The options are:<ul><li>**Maximum Quality**</li><li>**Balanced**</li><li>**Maximum Performance**</li><li>**Ultra Performance**</li></ul>This property is available only if you enable **Use DLSS Custom Quality**. |
| **Use DLSS Custom Attributes**     | Overrides the DLSS properties specified in the [HDRP Asset](HDRP-Asset.md), on this camera. |
| **DLSS Use Optimal Settings**      | Enables DLSS controlling sharpness and screen percentage automatically. This property is available only if you enable **Use DLSS Custom Attributes**. |

<a name="fsr2-settings"></a>
### FSR2 settings

The following properties are available only if you enable **Allow FSR2**.

| **Property** | **Description** |
|-|-|
| **Use FSR2 Custom Quality** | Indicates whether this camera overrides the FSR2 quality mode specified in the [HDRP Asset](HDRP-Asset.md). |
| **FSR2 Use Optimal Settings** | Enables the **FSR2 Mode** property. This property is available only if you enable **Use FSR2 Custom Quality**. |
| **FSR2 Mode** | Sets whether FSR2 prioritizes quality or performance. The options are:<ul><li>**Quality**</li><li>**Balanced**</li><li>**Performance**</li><li>**Ultra Performance**</li></ul>This property is available only if you enable **FSR2 Use Optimal Settings**. |
| **Use FSR2 Custom Attributes** | Overrides the FSR2 properties specified in the [HDRP Asset](HDRP-Asset.md), on this camera. |
| **FSR2 Enable Sharpness** | Enables an **FSR2 Sharpness** slider that lets you set the sharpness of the FSR2 upscale filter. A value of 1.0 means maximum sharpness. A value of 0 means no sharpening. You can also set the sharpness in your [HDRP Asset](hdrp-asset.md). This property is available only if you enable **Use FSR2 Custom Attributes**. |

<a name="taa-settings"></a>
### TAA settings

The following properties are available only if you set **Post Anti-aliasing** to **Temporal Anti-aliasing (TAA)**.

| **Property** | **Description** |
|-|-|
| **Quality Preset**         | The quality level of TAA. The default settings for higher presets aren't guaranteed to produce better results than lower presets. The result depends on the content in your scene. However, the high quality presets give you more options that you can use to adapt the anti-aliasing to your content. |
| **Sharpening Mode** | Specifies the sharpening method to use.<br />&#8226; **Low quality**: Provides fast sharpening, but might produce lower quality results or artifacts compared to the other sharpening methods.<br />&#8226; **Post Sharpen**: Provides higher-quality sharpening than **Low Quality**, but is more resource-intensive.<br />&#8226; **Contrast Adaptive Sharpening**: AMD's FidelityFX Contrast Adaptive Sharpening. Provides higher-quality sharpening than **Low Quality**, but gives you less control. |
| **Sharpen Strength**    | The intensity of the sharpening filter that Unity applies to the result of TAA. This reduces the soft look that TAA can produce. High values can cause ringing issues (dark lines along the edges of geometry) |
| **Ringing Reduction** | Controls how much of the sharpening result HDRP takes from the result without ringing. Reduces unnatural dark outlines, but might also decrease sharpening. Values above 0.0 lead to a small extra cost. This property appears only when you set **TAA Sharpening Mode** to **Post Sharpen** |
| **History Sharpening**  | Sets the strength of the history sharpening effect. When this value is above 0, Unity samples the history buffer with a bicubic filter that sharpens the result of TAA. You can use this to produce a sharper image during motion. A high value can cause ringing issues (dark lines along the edges of geometry). If you set this value to 0, it increases the performance of TAA because Unity simplifies the history buffer sampling|
| **Anti-flickering**     | Sets the strength of TAA's anti-flickering effect. Use this to reduce some cases of flickering. Increasing this value might lead to more [ghosting](Glossary.md#Ghosting) or [disocclusion](Glossary.md#Disocclusion) artifacts. <br />This property is only visible when **TAA Quality Preset** is set to a value above **Low**. |
| **Speed rejection**       | Controls the threshold at which Unity rejects history buffer contribution for TAA. You can increase this value to remove ghosting artifacts. This works because Unity rejects history buffer contribution when a GameObject's current speed and reprojected speed history are very different. When you increase this value, it might also reintroduce some aliasing for fast-moving GameObjects. Setting this value to 0 increases the performance of TAA because Unity doesn't process speed rejection. |
| **Anti-ringing**        | Enable this property to reduce the ringing artifacts caused by high history sharpening values. When you enable this property, it reduces the effect of the history sharpening. This property is only visible when TAA Quality Preset is set to **High**. |
| **Base blend factor** | Determines how much the history buffer is blended together with the current frame result. Higher values mean more history contribution, which leads to better anti-aliasing, but is also more prone to ghosting.<br />This property is only visible when Advanced properties are displayed for the camera. |
| **Jitter Scale** | Controls the scale of jitter, which is the random offset HDRP applies to the camera position at each frame. Use a low value to reduce flickering and jittering at the cost of more aliasing. |

<a name="smaa-settings"></a>
### SMAA settings

The following properties are available only if you set **Post Anti-aliasing** to **Subpixel Morphological Anti-aliasing (SMAA)**.

| **Property** | **Description** |
|-|-|
| **SMAA Quality Preset**         | Use the drop-down to select the quality of SMAA. The difference in resource intensity is small between **Low** and **High**.<br />&#8226; **Low**: The lowest SMAA quality. This is the least resource-intensive option.<br />&#8226; **Medium**: A good balance between SMAA quality and resource intensity.<br />&#8226; **High**: The highest SMAA quality. This is the most resource-intensive option. This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down. |

<a name="Environment"></a>
## Environment

| **Property**                    | **Description**                                              |
| ------------------------------- | ------------------------------------------------------------ |
| **Background Type**             | Use the drop-down to select the type of background that the Camera fills the screen with before it renders a frame.<br />&#8226; **Sky**: The Camera fills the screen with the sky defined in the [Visual Environment](visual-environment-volume-override-reference.md) of the current [Volume](understand-volumes.md) settings.<br />&#8226; **Color**: The Camera fills the screen with the color set in **Background Color**.<br />&#8226; **None**: The Camera doesn't clear the screen and the color buffer is left uninitialized. In this case, there are no guarantees on what the contents of the buffer are when you start drawing. It could be content from the previous frame or content from another camera. Because of this, use this option with caution. |
| **Background Color**            | Use the HDR color picker to select the color that the Camera uses to clear the screen before it renders a frame. The Camera uses this color if:You select **Color** from the **Background Type** drop-down. You select **Sky** from the **Background Type** drop-down and there is no valid sky for the Camera to use. |
| **Volume Layer Mask**           | Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera. |
| **Volume Anchor Override**      | Assign a Transform that the [Volume](understand-volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you don't assign a Transform, the Camera uses its own Transform instead. |
| **Probe Layer Mask**            | Use the drop-down to set the Layer Mask that the Camera uses to exclude environment lights (light from Planar Reflection Probes and Reflection Probes). The Camera only uses Reflection Probes on Layers that you include in this Layer Mask. |
| **Fullscreen Passthrough**      | Enable the checkbox to make this Camera skip rendering settings and directly render in full screen. This is useful for video. |
| **Custom Frame Settings**       | Enable the checkbox to override the default [Frame Settings](Frame-Settings.md) for this Camera. This exposes a new set of Frame Settings that you can use to change how this Camera renders the Scene. |

<a name="Output"></a>
## Output

| **Property**       | **Description**                                              |
| ------------------ | ------------------------------------------------------------ |
| **Target Display** | Use the drop-down to select which device this Camera renders to. |
| **Target Texture** | Assign a RenderTexture that this Camera renders to. If you assign this property, the Camera no longer renders to the screen. |
| **Depth**          | Set the Camera's position in the draw order. Unity processes Cameras with a smaller **Depth** first, then processes Cameras with a larger **Depth** on top. |
| **ViewPort Rect**  | Set the position and size of this Camera's output on the screen.<br />&#8226; **X**: The beginning horizontal position of the output.<br />&#8226; **Y**: The beginning vertical position of the output.<br />&#8226; **W**: The width of the output.<br />&#8226; **H**: The height of the output. |

## Preset
When using Preset of a HD Camera, only a subset of properties are supported. Unsupported properties are hidden.
