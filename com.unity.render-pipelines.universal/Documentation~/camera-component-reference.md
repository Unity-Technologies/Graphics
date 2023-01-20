# Camera component reference

In the Universal Render Pipeline (URP), Unity exposes different properties of the Camera component in the Inspector depending on the Camera type. To change the type of the Camera, change the property **Render Type**.

* Base Render Type
    * [Projection](#Projection)
    * [Physical Camera](#PhysicalCamera)
    * [Rendering](#Rendering)
    * [Stack](#Stack)
    * [Environment](#Environment)
    * [Output](#Output)
* Overlay Render Type
    * [Projection](#Projection)
    * [Physical Camera](#PhysicalCamera)
    * [Rendering](#Rendering)
    * [Environment](#Environment)

<a name="Projection"></a>
## Projection

|    **Property** || **Description** |
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Projection** || Toggles the camera's capability to simulate perspective. |
|| *Perspective* | Camera will render objects with perspective intact. |
|| *Orthographic* | Camera will render objects uniformly, with no sense of perspective. |
| **Size** || The viewport size of the Camera when set to Orthographic. |
| **FOV Axis** || Field of view axis. |
| **Field of view** || The width of the Camera's view angle, measured in degrees along the selected axis. |
| **Physical Camera** || Tick this box to enable the Physical Camera properties for this camera. <br/><br/> When the Physical Camera properties are enabled, Unity calculates the **Field of View** using the properties that simulate real-world camera attributes: **Focal Length**, **Sensor Size**, and **Lens Shift**. <br/><br/>Physical Camera properties are not visible in the Inspector until you tick this box.|
| **Clipping Planes** || Distances from the camera to start and stop rendering. |
|| *Near* | The closest point relative to the Camera that drawing will occur. |
|| *Far* | The furthest point relative to the Camera that drawing will occur. |

### <a name="PhysicalCamera"></a>Physical Camera

|    **Property** || **Description** |
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Camera Body** ||
| | *Sensor Type*     | Specify the real-world camera format you want the camera to simulate. Choose the desired format from the list. <br/><br/>When you choose a camera format, Unity sets the the **Sensor Size > X** and **Y** properties to the correct values automatically. <br/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**.|
| | *Sensor Size*    | Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. You can enter custom values if needed. |
| | *Gate Fit*      | Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor). <br/><br/>For further information about resolution gate and film gate, see documentation on [Physical Cameras](https://docs.unity3d.com/Manual/PhysicalCameras.html). |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Vertical* |Fits the resolution gate to the height of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides. <br/><br/>When you choose this setting, changing the sensor width (**Sensor Size > X property**) has no effect on the rendered image.|
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Horizontal* |Fits the resolution gate to the width of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom. <br/><br/>When you choose this setting, changing the sensor height (**Sensor Size > Y** property) has no effect on the rendered image.|
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Fill* |Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image.|
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Overscan* |Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image.|
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*None* |Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio.|
| **Lens** ||
| | *Focal Length*    | Set the distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa. <br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly.|
| | *Shift*       | Shift the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. <br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines). <br/><br/>Shift the lens along either axis to make the camera frustum [oblique](https://docs.unity3d.com/Manual/ObliqueFrustum.html). |

<a name="Rendering"></a>
## Rendering

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Renderer** | Controls which renderer this Camera uses. |
| **Post Processing** | Enables post-processing effects. |
| **Anti-Aliasing** | Use the drop-down to select the method that this Camera uses for post-process anti-aliasing. A Camera can still use Multisample Anti-aliasing (MSAA), which is a hardware feature, at the same time as post-process anti-aliasing unless you use Temporal Anti-aliasing.</br></br>The following Anti-aliasing options are available:<ul><li>**None**: This Camera can process MSAA but does not process any post-process anti-aliasing.</li><li>**Fast Approximate Anti-aliasing (FXAA)**: Performs a full screen pass which smooths edges on a per-pixel level.</li><li>**Subpixel Morphological Anti-aliasing (SMAA)**: Finds edge patterns in the image and blends the pixels on these edges according to those patterns.</li><li>**Temporal Anti-aliasing (TAA)**: Uses previous frames accumulated into a color history buffer to smooth edges over the course of multiple frames.</li></ul>For more information, see [Anti-aliasing in the Universal Render Pipeline](anti-aliasing.md). |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Quality (SMAA)* | Use the drop-down to select the quality of SMAA. The difference in resource intensity is fairly small between **Low** and **High**.</br></br>Available options:<ul><li>**Low**</li><li>**Medium**</li><li>**High**</li></ul>This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Quality (TAA)* | Use the drop-down to select the quality of TAA.</br></br>Available options:<ul><li>**Very Low**</li><li>**Low**</li><li>**Medium**</li><li>**High**</li><li>**Very High**</li></ul>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Contrast Adaptive Sharpening* | Enables high quality post sharpening to reduce TAA blur.</br></br>This setting is overridden when you enable [AMD FidelityFX Super Resolution (FSR)](universalrp-asset.md#quality) upscaling in the URP Asset as FSR handles sharpening when it performs post-upscale sharpening.</br></br>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Base Blend Factor* | Determines how much the history buffer blends with the current frame result. Higher values mean more history contribution, which improves the anti-aliasing, but also increases the chance of ghosting.</br></br>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Jitter Scale* | Determines the scale of the jitter applied when TAA is enabled. A lower value reduces visible flickering and jittering, but also reduces the effectiveness of the anti-aliasing.</br></br>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Mip Bias* | Determines how much texture mipmap selection is biased when rendering.</br></br>A positive bias makes a texture appear more blurry, while a negative bias sharpens the texture. However, a lower value also has a negative impact on performance.</br></br>**Note**: Requires mipmaps in textures.</br></br>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Variance Clamp Scale* | Determines the size of the color volume Unity uses to find nearby pixels when the color history is incorrect or unavailable. To do this, the clamp limits how much a pixel's color can vary from the color of the pixels that surround it.</br></br>Lower values can reduce ghosting, but produce more flickering. Higher values reduce flickering, but are prone to blur and ghosting.</br></br>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| **Stop NaNs** | Enable the checkbox to make this Camera replace values that are Not a Number (NaN) with a black pixel. This stops certain effects from breaking, but is a resource-intensive process. Only enable this feature if you experience NaN issues that you can not fix. |
| **Dithering** | Enable the checkbox to apply 8-bit dithering to the final render. This can help reduce banding on wide gradients and low light areas. |
| **Render Shadows** | Enables shadow rendering. |
| **Priority** | A Camera with a higher priority is drawn on top of a Camera with a lower priority. [-100, 100] |
| **Opaque Texture** | Controls whether the Camera creates a CameraOpaqueTexture, which is a copy of the rendered view. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*On* | Camera creates a CameraOpaqueTexture.|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Off* | Camera does not create a CameraOpaqueTexture. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Use Pipeline Settings* | The value of this setting is determined by the Render Pipeline Asset.|
| **Depth Texture** | Controls whether the Camera creates CameraDepthTexture, which is a copy of the rendered depth values. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*On* | The Camera creates a CameraDepthTexture.|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Off* | The Camera does not create a CameraDepthTexture. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Use Pipeline Settings* | The value of this setting is determined by the Render Pipeline Asset.|
| **Culling Mask** | Which Layers the Camera renders to. |
| **Occlusion Culling** | Enables Occlusion Culling. |

## <a name="Stack"></a>Stack

> **Note:** This section is only available if **Render Type** is set to **Base**

A camera stack allows to composite results of several cameras together. The camera stack consists of a Base camera and any number of additional Overlay cameras.

You can use the stack property add Overlay cameras to the stack and they will render in the order as defined in the stack. For more information on configuring and using Camera Stacks, see [Camera Stacking](camera-stacking.md).

## <a name="Environment"></a>Environment

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Background Type** | Controls how to initialize the color buffer at the start of this Camera's render loop. For more information, see [the documentation on clearing](cameras-advanced.md#clearing).|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Skybox* | Initializes the color buffer by clearing to a Skybox. Defaults to a background color if no Skybox is found.|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Solid Color* | Initializes the color buffer by clearing to a given color.<br/>If you select this property, Unity shows the following extra property:<br/>**Background**: the Camera clears its color buffer to this color before rendering. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Uninitialized* | Does not initialize the color buffer. This means that the load action for that specific RenderTarget will be `DontCare` instead of `Load` or `Clear`. `DontCare` specifies that the previous contents of the RenderTarget don't need to be preserved.<br/><br/>Only use this option in order to optimize performance in situations where your Camera or Camera Stack will draw to every pixel in the color buffer, otherwise the behaviour of pixels the Camera doesn't draw is undefined.<br/><br/>**Note**: The results might look different between Editor and Player, as the Editor doesn't run on Tile-Based Deferred Rendering (TBDR) GPUs (found in mobile devices). If you use this option on TBDR GPUs, it causes uninitialized tile memory and the content is undefined. |
| **Volumes** | The settings in this section define how Volumes affect this Camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Update&#160;Mode* | Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Volume&#160;Mask* | Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera.|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Volume&#160;Trigger* | Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you do not assign a Transform, the Camera uses its own Transform instead.|

## <a name="Output"></a>Output

> **Note:** This section is only available if **Render Type** is set to **Base**

> **Note:** When a Camera's **Render Type** is set to **Base** and its **Render Target** is set to **Texture**, Unity does not show the following properties in the Inspector for the Camera:
> * HDR
> * MSAA
> * Allow Dynamic Resolution
> * Target Display

This is because the Render Texture determines these properties. To change them, do so on the Render Texture Asset.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Output Texture** | Renders this Camera's output to a RenderTexture if this field is assigned, otherwise renders to screen.
| **HDR** | Enables High Dynamic Range rendering for this camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
| **MSAA** | Enables [Multisample Anti-aliasing](anti-aliasing.md#msaa) for this camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
| **Viewport Rect** |Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0-1). This property only appears when you select **Screen** from the **Output Target** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*X* |The beginning horizontal position that the camera view will be drawn. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Y* |The beginning vertical position that the camera view will be drawn. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*W* (Width) |Width of the camera output on the screen. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*H* (Height) |Height of the camera output on the screen. |
| **Allow Dynamic Resolution** |Enables Dynamic Resolution rendering for this Camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
| **Target Display** |Defines which external device to render to.  Between 1 and 8.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
