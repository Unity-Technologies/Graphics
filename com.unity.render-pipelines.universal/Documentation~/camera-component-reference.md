# Camera component reference

In the Universal Render Pipeline (URP), Unity exposes different properties of the Camera component in the Inspector depending on the Camera type. To change the type of the Camera, change the property **Render Mode**.

* Base Render Mode
    * [Projection](#Projection)
    * [Physical Camera](#PhysicalCamera)
    * [Rendering](#Rendering)
    * [Stack](#Stack)
    * [Environment](#Environment)
    * [Output](#Output)
* Overlay Render Mode
    * [Projection](#Projection)
    * [Physical Camera](#PhysicalCamera)
    * [Rendering](#Rendering)
    * [Environment](#Environment)

<a name="Projection"></a>
## Projection
|    **Property** ||**Description**|
| ------------------------ | - | :-------------------------------------------------------------------- |
|__Projection__ ||Toggles the camera's capability to simulate perspective. |
||*Perspective* |Camera will render objects with perspective intact. |
||*Orthographic* |Camera will render objects uniformly, with no sense of perspective. |
|**Size**||The viewport size of the Camera when set to Orthographic. |
|__Field of view__ ||Field of view axis. |
|__Field of view__||The width of the Camera's view angle, measured in degrees along the selected axis. |
|__Physical Camera__ ||Tick this box to enable the Physical Camera properties for this camera. <br/><br/> When the Physical Camera properties are enabled, Unity calculates the **Field of View** using the properties that simulate real-world camera attributes: **Focal Length**, **Sensor Size**, and **Lens Shift**. <br/><br/>Physical Camera properties are not visible in the Inspector until you tick this box.|
|__Clipping Planes__ ||Distances from the camera to start and stop rendering. |
||_Near_ |The closest point relative to the Camera that drawing will occur. |
||_Far_ |The furthest point relative to the Camera that drawing will occur. |

<a name="PhysicalCamera"></a>
### Physical Camera

|    **Property** ||**Description**|
| ------------------------ | - | :-------------------------------------------------------------------- |
| **Camera Body** ||
| |*Sensor Type*     | Specify the real-world camera format you want the camera to simulate. Choose the desired format from the list. <br/><br/>When you choose a camera format, Unity sets the the **Sensor Size > X** and **Y** properties to the correct values automatically. <br/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**.|
| |*Sensor Size*    | Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. You can enter custom values if needed. |
| |*Gate Fit*      | Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor). <br/><br/>For further information about resolution gate and film gate, see documentation on [Physical Cameras](https://docs.unity3d.com/Manual/PhysicalCameras.html). |
||&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Vertical_ |Fits the resolution gate to the height of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides. <br/><br/>When you choose this setting, changing the sensor width (**Sensor Size > X property**) has no effect on the rendered image.|
||&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Horizontal_ |Fits the resolution gate to the width of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom. <br/><br/>When you choose this setting, changing the sensor height (**Sensor Size > Y** property) has no effect on the rendered image.|
||&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Fill_ |Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image.|
||&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Overscan_ |Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image.|
||&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_None_ |Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio.|
| **Lens** ||
| |*Focal Length*    | Set the distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa. <br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly.|
| |*Shift*       | Shift the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. <br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines). <br/><br/>Shift the lens along either axis to make the camera frustum [oblique](https://docs.unity3d.com/Manual/ObliqueFrustum.html). |

<a name="Rendering"></a>
## Rendering

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Renderer__ |Controls which renderer this Camera uses. |
|__Post Processing__ |Enables post-processing effects. |
|__Anti-aliasing__          | Use the drop-down to select the method that this Camera uses for post-process anti-aliasing. A Camera can still use [multisample anti-aliasing](#base-output) (MSAA), which is a hardware feature, at the same time as post-process anti-aliasing.<br />&#8226; **None**: This Camera can process MSAA but does not process any post-process anti-aliasing.<br />&#8226; **Fast Approximate Anti-aliasing (FXAA)**: Smooths edges on a per-pixel level. This is the least resource intensive anti-aliasing technique in URP.<br />&#8226; **Subpixel Morphological Anti-aliasing (SMAA)**: Finds patterns in borders of the image and blends the pixels on these borders according to the pattern. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Quality_| Use the drop-down to select the quality of SMAA. The difference in resource intensity is fairly small between **Low** and **High**.<br />&#8226; **Low**: The lowest SMAA quality. This is the least resource-intensive option.<br />&#8226; **Medium**: A good balance between SMAA quality and resource intensity.<br />&#8226; **High**: The highest SMAA quality. This is the most resource-intensive option.<br /><br />This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down.|
|__Stop NaNs__| Enable the checkbox to make this Camera replace values that are Not a Number (NaN) with a black pixel. This stops certain effects from breaking, but is a resource-intensive process. Only enable this feature if you experience NaN issues that you can not fix. |
|__Dithering__ |Enable the checkbox to apply 8-bit dithering to the final render. This can help reduce banding on wide gradients and low light areas. |
|__Render Shadows__ |Enables shadow rendering. |
|__Priority__ |A Camera with a higher priority is drawn on top of a Camera with a lower priority. [-100, 100] |
|__Opaque Texture__ | Controls whether the Camera creates a CameraOpaqueTexture, which is a copy of the rendered view. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_On_ |Camera creates a CameraOpaqueTexture.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Off_ |Camera does not create a CameraOpaqueTexture. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Use Pipeline Settings_ |The value of this setting is determined by the Render Pipeline Asset.|
|__Depth Texture__ | Controls whether the Camera creates CameraDepthTexture, which is a copy of the rendered depth values. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_On_ |The Camera creates a CameraDepthTexture.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Off_ |The Camera does not create a CameraDepthTexture. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Use Pipeline Settings_ |The value of this setting is determined by the Render Pipeline Asset.|
|__Culling Mask__ |Which Layers the Camera renders to. |
|__Occlusion Culling__ |Enables Occlusion Culling. |

<a name="Stack"></a>
## Stack

> **_Warning:_** Stack can only be edited if **Render Mode** is set to **Base**

A camera stack allows to composite results of several cameras together. The camera stack consists of a Base camera and any number of additional Overlay cameras.

You can use the stack property add Overlay cameras to the stack and they will render in the order as defined in the stack. For more information on configuring and using Camera Stacks, see [Camera Stacking](camera-stacking.md).

<a name="Environment"></a>
## Environment

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Background Type__ |Controls how to initialize the color buffer at the start of this Camera's render loop. For more information, see [the documentation on clearing](cameras-advanced.md#clearing).|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Skybox_|Initializes the color buffer by clearing to a Skybox. Defaults to a background color if no Skybox is found.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Solid Color_|Initializes the color buffer by clearing to a given color.<br/>If you select this property, Unity shows the following extra property:<br/>__Background__: the Camera clears its color buffer to this color before rendering. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Uninitialized_|Does not initialize the color buffer. Choose this option only if your Camera or Camera Stack will draw to every pixel in the color buffer.|
| **Volumes** | The settings in this section define how Volumes affect this Camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Update&#160;Mode_ | Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Volume&#160;Mask_ | Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera.|
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Volume&#160;Trigger_ | Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you do not assign a Transform, the Camera uses its own Transform instead.|

<a name="Output"></a>
## Output

> **_Warning:_** Output can only be edited if **Render Mode** is set to **Base**

> **_Note:_** When a Camera's *Render Mode* is set to *Base* and its *Render Target* is set to *Texture*, Unity does not expose the following properties in the Inspector for the Camera:
> * HDR
> * MSAA
> * Allow Dynamic Resolution
> * Target Display

This is because the Render Texture determines these properties. To change them, do so on the Render Texture Asset.

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Output Texture__ | Renders this Camera's output to a RenderTexture if this field is assigned, otherwise renders to screen.
|__HDR__| Enables High Dynamic Range rendering for this camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
|__MSAA__| Enables multi sample antialiasing for this camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
|__Viewport Rect__|Four values that indicate where on the screen this camera view will be drawn. Measured in Viewport Coordinates (values 0-1). This property only appears when you select **Screen** from the **Output Target** drop-down. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_X_ |The beginning horizontal position that the camera view will be drawn. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Y_ |The beginning vertical position that the camera view will be drawn. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_W_ (Width) |Width of the camera output on the screen. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_H_ (Height) |Height of the camera output on the screen. |
|__Allow Dynamic Resolution__|Enables Dynamic Resolution rendering for this Camera.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
|__Target Display__|Defines which external device to render to.  Between 1 and 8.<br/>This property only appears when you select **Screen** from the **Output Target** drop-down.|
