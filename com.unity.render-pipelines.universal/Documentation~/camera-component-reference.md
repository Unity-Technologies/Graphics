# Camera component reference

In the Universal Render Pipeline (URP), Unity exposes different properties of the Camera component in the Inspector depending on the Camera type.

* [Base Camera component reference](#base-camera)
* [Overlay Camera component reference](#overlay-camera)

<a name="base-camera"></a>
## Base Camera component reference

![Base Camera](Images/camera-inspector-base.png)

The Camera Inspector has the following sections when the Camera has its **Render Mode** set to **Base**. To read more about a section, click the corresponding link below, or scroll down on the page:

* [Projection](#base-projection)
* [Rendering](#base-rendering)
* [Environment](#base-environment)
* [Output](#base-output)
* [Stack](#base-stack)

<a name="base-projection"></a>
## Projection

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Render Mode__ |Controls which type of Camera this is. |
|__Projection__ |Toggles the camera's capability to simulate perspective. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Perspective_ |Camera will render objects with perspective intact. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Orthographic_ |Camera will render objects uniformly, with no sense of perspective. |
|__Size__|The viewport size of the Camera when set to Orthographic. |
|__FOV Axis__ |Field of view axis. |
|__Field of view__|The width of the Camera's view angle, measured in degrees along the selected axis. |
|__Physical Camera__ |Tick this box to enable the Physical Camera properties for this camera. <br/><br/> When the Physical Camera properties are enabled, Unity calculates the **Field of View** using the properties that simulate real-world camera attributes: **Focal Length**, **Sensor Size**, and **Lens Shift**. <br/><br/>Physical Camera properties are not visible in the Inspector until you tick this box.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Focal Length_ |Set the distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa. <br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Sensor Type_ |Specify the real-world camera format you want the camera to simulate. Choose the desired format from the list. <br/><br/>When you choose a camera format, Unity sets the the **Sensor Size > X** and **Y** properties to the correct values automatically. <br/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Sensor Size_ |Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. You can enter custom values if needed.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_X_ |The width of the sensor. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Y_ |The height of the sensor. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Lens Shift_ |Shift the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. <br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines). <br/><br/>Shift the lens along either axis to make the camera frustum [oblique](ObliqueFrustum). |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_X_ |The horizontal sensor offset. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Y_ |The vertical sensor offset. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Gate Fit_ |Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor). <br/><br/>For further information about resolution gate and film gate, see documentation on [Physical Cameras](PhysicalCameras).|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Vertical_ |Fits the resolution gate to the height of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides. <br/><br/>When you choose this setting, changing the sensor width (**Sensor Size > X property**) has no effect on the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Horizontal_ |Fits the resolution gate to the width of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom. <br/><br/>When you choose this setting, changing the sensor height (**Sensor Size > Y** property) has no effect on the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Fill_ |Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Overscan_ |Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_None_ |Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio.|
|__Clipping Planes__ |Distances from the camera to start and stop rendering. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Near_ |The closest point relative to the Camera that drawing will occur. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Far_ |The furthest point relative to the Camera that drawing will occur. |

<a name="base-rendering"></a>
## Rendering

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Renderer__ |Controls which renderer this Camera uses. |
|__Post Processing__ |Enables post-processing effects. |
|__Anti-aliasing__          | Use the drop-down to select the method that this Camera uses for post-process anti-aliasing. A Camera can still use [multisample anti-aliasing](#base-output) (MSAA), which is a hardware feature, at the same time as post-process anti-aliasing.<br />&#8226; **None**: This Camera can process MSAA but does not process any post-process anti-aliasing.<br />&#8226; **Fast Approximate Anti-aliasing (FXAA)**: Smooths edges on a per-pixel level. This is the least resource intensive anti-aliasing technique in URP.<br />&#8226; **Subpixel Morphological Anti-aliasing (SMAA)**: Finds patterns in borders of the image and blends the pixels on these borders according to the pattern. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Quality_| Use the drop-down to select the quality of SMAA. The difference in resource intensity is fairly small between **Low** and **High**.<br />&#8226; **Low**: The lowest SMAA quality. This is the least resource-intensive option.<br />&#8226; **Medium**: A good balance between SMAA quality and resource intensity.<br />&#8226; **High**: The highest SMAA quality. This is the most resource-intensive option.<br /><br />This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down.|
|__Stop NaN__| Enable the checkbox to make this Camera replace values that are Not a Number (NaN) with a black pixel. This stops certain effects from breaking, but is a resource-intensive process. Only enable this feature if you experience NaN issues that you can not fix. |
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

<a name="base-environment"></a>
## Environment

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Background Type__ |Controls how to initialize the color buffer at the start of this Camera's render loop. For more information, see [the documentation on clearing](cameras-advanced.md#clearing).|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Skybox_|Initializes the color buffer by clearing to a Skybox. Defaults to a background color if no Skybox is found.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Solid Color_|Initializes the color buffer by clearing to a given color.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Uninitialized_|Does not initialize the color buffer. Choose this option only if your Camera or Camera Stack will draw to every pixel in the color buffer.|
|__Background__ |The Camera clears its color buffer to this colour before rendering.<br/>This property only appears when you select **Solid Color** from the **Background Type** drop-down.|
|__Volume Mask__| Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera.|
|__Volume Trigger__| Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you do not assign a Transform, the Camera uses its own Transform instead.|

<a name="base-output"></a>
## Output

Note that when a Camera's *Render Mode* is set to *Base* and its *Render Target* is set to *Texture*, Unity does not expose the following properties in the Inspector for the Camera:

* HDR
* MSAA
* Allow Dynamic Resolution
* Target Display

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

<a name="base-stack"></a>
## Stack

A camera stack allows to composite results of several cameras together. The camera stack consists of a Base camera and any number of additional Overlay cameras.

You can use the stack property add Overlay cameras to the stack and they will render in the order as defined in the stack. For more information on configuring and using Camera Stacks, see [Camera Stacking](camera-stacking.md).

__Important note:__ In this version of URP, Camera Stacking is not supported when using the 2D Renderer or when using the VR Multi Pass mode. Support for these will be added in upcoming versions of URP.

<a name="overlay-camera"></a>
## Overlay Camera component reference

![Overlay Camera Inspector](Images/camera-inspector-overlay.png)

__Important note:__ In this version of URP, Camera Stacking is not supported when using the 2D Renderer or when using the VR Multi Pass mode. Support for these will be added in upcoming versions of URP.

When you use [Camera Stacking](camera-stacking.md), the [Base Camera](camera-types-and-render-mode.md#base-camera) of a Camera Stack determines most of the properties of the Camera Stack. Because [Overlay Cameras](camera-types-and-render-mode.md#overlay-camera) can only be used as part of a Camera Stack, you can configure only a limited number of settings on an Overlay Camera. Overlay cameras not assigned to a camera stack will skip rendering.

The Camera Inspector has the following sections when the Camera has its Render Mode set to Overlay. To read more about a section, click the corresponding link below, or scroll down on the page:

* [Projection](#overlay-projection)
* [Rendering](#overlay-rendering)
* [Environment](#overlay-environment)

<a name="overlay-projection"></a>
## Projection

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Render Mode__ |Controls which type of Camera this is. |
|__Projection__ |Toggles the camera's capability to simulate perspective. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Perspective_ |Camera will render objects with perspective intact. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Orthographic_ |Camera will render objects uniformly, with no sense of perspective. |
|__Size__ (when Orthographic is selected) |The viewport size of the Camera when set to Orthographic. |
|__FOV Axis__ (when Perspective is selected) |Field of view axist. |
|__Field of view__ (when Perspective is selected) |The width of the Camera's view angle, measured in degrees along the selected axis. |
|__Physical Camera__ |Tick this box to enable the Physical Camera properties for this camera. <br/><br/> When the Physical Camera properties are enabled, Unity calculates the **Field of View** using the properties that simulate real-world camera attributes: **Focal Length**, **Sensor Size**, and **Lens Shift**. <br/><br/>Physical Camera properties are not visible in the Inspector until you tick this box.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Focal Length_ |Set the distance, in millimeters, between the camera sensor and the camera lens.<br/><br/>Lower values result in a wider **Field of View**, and vice versa. <br/><br/>When you change this value, Unity automatically updates the **Field of View** property accordingly.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Sensor Type_ |Specify the real-world camera format you want the camera to simulate. Choose the desired format from the list. <br/><br/>When you choose a camera format, Unity sets the the **Sensor Size > X** and **Y** properties to the correct values automatically. <br/><br/>If you change the **Sensor Size** values manually, Unity automatically sets this property to **Custom**.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Sensor Size_ |Set the size, in millimeters, of the camera sensor. <br/><br/>Unity sets the **X** and **Y** values automatically when you choose the **Sensor Type**. You can enter custom values if needed.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_X_ |The width of the sensor. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Y_ |The height of the sensor. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Lens Shift_ |Shift the lens horizontally or vertically from center. Values are multiples of the sensor size; for example, a shift of 0.5 along the X axis offsets the sensor by half its horizontal size. <br/><br/>You can use lens shifts to correct distortion that occurs when the camera is at an angle to the subject (for example, converging parallel lines). <br/><br/>Shift the lens along either axis to make the camera frustum [oblique](ObliqueFrustum). |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_X_ |The horizontal sensor offset. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Y_ |The vertical sensor offset. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Gate Fit_ |Options for changing the size of the **resolution gate** (size/aspect ratio of the game view) relative to the **film gate** (size/aspect ratio of the Physical Camera sensor). <br/><br/>For further information about resolution gate and film gate, see documentation on [Physical Cameras](PhysicalCameras).|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Vertical_ |Fits the resolution gate to the height of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity crops the rendered image at the sides. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity overscans the rendered image at the sides. <br/><br/>When you choose this setting, changing the sensor width (**Sensor Size > X property**) has no effect on the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Horizontal_ |Fits the resolution gate to the width of the film gate. <br/><br/>If the sensor aspect ratio is larger than the game view aspect ratio, Unity overscans the rendered image on the top and bottom. <br/><br/>If the sensor aspect ratio is smaller than the game view aspect ratio, Unity crops the rendered image on the top and bottom. <br/><br/>When you choose this setting, changing the sensor height (**Sensor Size > Y** property) has no effect on the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Fill_ |Fits the resolution gate to either the width or height of the film gate, whichever is smaller. This crops the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Overscan_ |Fits the resolution gate to either the width or height of the film gate, whichever is larger. This overscans the rendered image.|
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_None_ |Ignores the resolution gate and uses the film gate only. This stretches the rendered image to fit the game view aspect ratio.|
|__Clipping Planes__ |Distances from the camera to start and stop rendering. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Near_ |The closest point relative to the camera that drawing will occur. |
|&#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;_Far_ |The furthest point relative to the camera that drawing will occur. |

<a name="overlay-rendering"></a>
## Rendering

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Renderer__ |Controls which renderer this Camera uses. |
|__Post Processing__ |Enables post-processing effects. |
|__Clear Depth__ | If enabled, the depth buffer from the previous Camera will be cleared at the start of this Camera's render loop. For more information, see [the documentation on clearing](cameras-advanced.md#clearing). |
|__Render Shadows__ |Enables shadow rendering. |
|__Culling Mask__ |Which Layers the Camera renders to. |
|__Occlusion Culling__ |Enables Occlusion Culling. |

<a name="overlay-environment"></a>
## Environment

| __Property__               | __Description__                                              |
| -------------------------- | ------------------------------------------------------------ |
|__Volume Mask__| Use the drop-down to set the Layer Mask that defines which Volumes affect this Camera.|
|__Volume Trigger__| Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this Camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The Camera then uses the post-processing and Scene settings for Volumes that the character enters. If you do not assign a Transform, the Camera uses its own Transform instead.|
