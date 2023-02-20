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

| **Property** || **Description** |
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Projection** || Toggle the camera's capability to simulate perspective. |
|| *Perspective* | Camera will render objects with perspective intact. |
|| *Orthographic* | Camera will render objects uniformly, with no sense of perspective. |
| **Field of View Axis** || Set the axis Unity measures the Camera's field of view along.<br/><br/>Available options:<ul><li>**Vertical**</li><li>**Horizontal**</li></ul>This property is only visible when **Projection** is set to **Perspective**. |
| **Field of View** || Set the width of the Camera's view angle, measured in degrees along the selected axis.<br/><br/>This property is only visible when **Projection** is set to **Perspective**. |
| **Size** || Set the viewport size of the Camera.<br/><br/>This property is only visible when **Projection** is set to **Orthographic**. |
| **Clipping Planes** || Set the distances from the camera where rendering starts and stops. |
|| *Near* | The closest point relative to the Camera that drawing will occur. |
|| *Far* | The furthest point relative to the Camera that drawing will occur. |
| **Physical Camera** || Enable Physical Camera properties for this camera.<br/><br/>When the Physical Camera properties are enabled, Unity calculates the **Field of View** using the properties that simulate real-world camera attributes: **Focal Length**, **Sensor Size**, and **Lens Shift**.<br/><br/>Physical Camera properties are not visible in the Inspector until you tick this box.<br/><br/>This property is only visible when **Projection** is set to **Perspective**. |

### <a name="PhysicalCamera"></a>Physical Camera

|    **Property** || **Description** |
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Camera Body** ||
|| *Sensor Type*  | Specify the real-world camera format you want the camera to simulate. Choose the desired format from the list. <br/><br/>When you choose a camera format, Unity sets the the **Sensor Size > X** and **Y** properties to the correct values automatically. <br/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**. |
|| *Sensor Size* | Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. You can enter custom values if needed. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*X* | Set the horizontal size of the camera sensor. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Y* | Set the vertical size of the camera sensor. |
|| *Gate Fit* | Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor). <br/><br/>For further information about resolution gate and film gate, see documentation on [Physical Cameras](https://docs.unity3d.com/Manual/PhysicalCameras.html). |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Vertical* | Fits the resolution gate to the height of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides. <br/><br/>When you choose this setting, changing the sensor width (**Sensor Size > X property**) has no effect on the rendered image. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Horizontal* | Fits the resolution gate to the width of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom. <br/><br/>When you choose this setting, changing the sensor height (**Sensor Size > Y** property) has no effect on the rendered image. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Fill* | Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Overscan* | Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*None* | Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio. |
| **Lens** ||
|| *Focal Length* | Set the distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa. <br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly. |
|| *Shift* | Shift the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. <br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines). <br/><br/>Shift the lens along either axis to make the camera frustum [oblique](https://docs.unity3d.com/Manual/ObliqueFrustum.html). |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*X* | Set the horizontal offset of the lens from the Camera sensor. |
|| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Y* | Set the vertical offset of the lens from the Camera sensor. |

<a name="Rendering"></a>
## Rendering

<a name="Rendering"></a>
## Rendering

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Renderer** | Control which renderer this Camera uses. |
| **Post Processing** | Enable post-processing effects. |
| **Anti-Aliasing** | Select the method that this Camera uses for post-process anti-aliasing. A Camera can still use Multisample Anti-aliasing (MSAA), which is a hardware feature, at the same time as post-process anti-aliasing unless you use Temporal Anti-aliasing.</br></br>The following Anti-aliasing options are available:<ul><li>**None**: This Camera can process MSAA but does not process any post-process anti-aliasing.</li><li>**Fast Approximate Anti-aliasing (FXAA)**: Performs a full screen pass which smooths edges on a per-pixel level.</li><li>**Subpixel Morphological Anti-aliasing (SMAA)**: Finds edge patterns in the image and blends the pixels on these edges according to those patterns.</li></ul>For more information, see [Anti-aliasing in the Universal Render Pipeline](anti-aliasing.md).<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Quality (SMAA)* | Select the quality of SMAA. The difference in resource intensity is fairly small between **Low** and **High**.</br></br>Available options:<ul><li>**Low**</li><li>**Medium**</li><li>**High**</li></ul>This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down. |
| **Stop NaNs** | Enable the checkbox to make this Camera replace values that are Not a Number (NaN) with a black pixel. This stops certain effects from breaking, but is a resource-intensive process. Only enable this feature if you experience NaN issues that you can not fix.<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| **Dithering** | Enable the checkbox to apply 8-bit dithering to the final render. This can help reduce banding on wide gradients and low light areas.<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| **Clear Depth** | Enable to clear depth from previous camera on rendering.<br/><br/>This property is only visible when **Render Type** is set to **Overlay**. |
| **Render Shadows** | Enable shadow rendering. |
| **Priority** | A Camera with a higher priority is drawn on top of a Camera with a lower priority. Priority has a range from -100 to 100.<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| **Opaque Texture** | Control whether the Camera creates a CameraOpaqueTexture, which is a copy of the rendered view.<br/><br/>Available options:<ul><li>**Off**: Camera does not create a CameraOpaqueTexture.</li><li>**On**: Camera creates a CameraOpaqueTexture.</li><li>**Use Pipeline Settings**: The Render Pipeline Asset determines the value of this setting.</li></ul>This property is only visible when **Render Type** is set to **Base**. |
| **Depth Texture** | Control whether the Camera creates CameraDepthTexture, which is a copy of the rendered depth values.<br/><br/>Available options:<ul><li>**Off**: Camera does not create a CameraDepthTexture.</li><li>**On**: Camera creates a CameraDepthTexture.</li><li>**Use Pipeline Settings**: The Render Pipeline Asset determines the value of this setting.</li></ul>This property is only visible when **Render Type** is set to **Base**. |
| **Culling Mask** | Select which Layers the Camera renders to. |
| **Occlusion Culling** | Enable Occlusion Culling. |

## <a name="Stack"></a>Stack

> **Note:** This section is only available if **Render Type** is set to **Base**

A camera stack allows to composite results of several cameras together. The camera stack consists of a Base camera and any number of additional Overlay cameras.

You can use the stack property add Overlay cameras to the stack and they will render in the order as defined in the stack. For more information on configuring and using Camera Stacks, see [Camera Stacking](camera-stacking.md).

## <a name="Environment"></a>Environment

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Background Type** | Control how to initialize the color buffer at the start of this Camera's render loop. For more information, see [the documentation on clearing](cameras-advanced.md#clearing).<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Skybox* | Initializes the color buffer by clearing to a Skybox. Defaults to a background color if no Skybox is found. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Solid Color* | Initializes the color buffer by clearing to a given color.<br/>If you select this property, Unity shows the following extra property:<br/>**Background**: the Camera clears its color buffer to this color before rendering. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Uninitialized* | Does not initialize the color buffer. This means that the load action for that specific RenderTarget will be `DontCare` instead of `Load` or `Clear`. `DontCare` specifies that the previous contents of the RenderTarget don't need to be preserved.<br/><br/>Only use this option in order to optimize performance in situations where your Camera or Camera Stack will draw to every pixel in the color buffer, otherwise the behaviour of pixels the Camera doesn't draw is undefined.<br/><br/>**Note**: The results might look different between Editor and Player, as the Editor doesn't run on Tile-Based Deferred Rendering (TBDR) GPUs (found in mobile devices). If you use this option on TBDR GPUs, it causes uninitialized tile memory and the content is undefined. |
| **Volumes** | The settings in this section define how Volumes affect this Camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Update&#160;Mode* | Select how Unity updates Volumes.<br/><br/>Available options:<ul><li>**Every Frame**: Update Volumes with every frame Unity renders.</li><li>**Via Scripting**: Only update volumes when triggered by a script.</li><li>**Use Pipeline Settings**: Use the default setting for the Render Pipeline.</li></ul> |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Volume&#160;Mask* | Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Volume&#160;Trigger* | Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you do not assign a Transform, the Camera uses its own Transform instead. |

## <a name="Output"></a>Output

> **Note:** This section is only available if **Render Type** is set to **Base**

> **Note:** When a Camera's **Render Type** is set to **Base** and its **Render Target** is set to **Texture**, Unity does not show the following properties in the Inspector for the Camera:
> * Target Display
> * HDR
> * MSAA
> * Allow Dynamic Resolution

This is because the Render Texture determines these properties. To change them, do so on the Render Texture Asset.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Output Texture** | Render this Camera's output to a RenderTexture if this field is assigned, otherwise render to the screen. |
| **Target Display** | Define which external device to render to. Between 1 and 8.<br/><br/>This property only appears when you select **Screen** from the **Output Target** drop-down. |
| **Target Eye** | Select the target eye for this camera.<br/><br/>Available options:<ul><li>**Both**: Allows XR rendering from the selected Camera.</li><li>**None**: Disables XR rendering for the selected Camera.</li></ul> |
| **Viewport Rect** | Four values that indicate where on the screen this camera view is drawn. Measured in Viewport Coordinates (values 0-1).<br/><br/>This property only appears when you select **Screen** from the **Output Target** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*X* | The beginning horizontal position Unity uses to draw the camera view. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*Y* | The beginning vertical position Unity uses to draw the camera view. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*W* (Width) | Width of the camera output on the screen. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;*H* (Height) | Height of the camera output on the screen. |
| **HDR** | Enable High Dynamic Range rendering for this camera.<br/><br/>This property only appears when you select **Screen** from the **Output Target** drop-down. |
| **MSAA** | Enable [Multisample Anti-aliasing](anti-aliasing.md#msaa) for this camera.<br/><br/>This property only appears when you select **Screen** from the **Output Target** drop-down. |
| **Allow Dynamic Resolution** | Enable URP Dynamic Resolution rendering for this Camera.<br/><br/>This property only appears when you select **Screen** from the **Output Target** drop-down. |
