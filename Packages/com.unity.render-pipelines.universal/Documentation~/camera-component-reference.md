# Camera component reference

In the Universal Render Pipeline (URP), Unity exposes different properties of the Camera component in the Inspector depending on the camera type. To change the type of the camera, select a [Render Type](camera-types-and-render-type.md).

Base cameras expose the following properties:

* [Projection](#Projection)
* [Physical Camera](#PhysicalCamera)
* [Rendering](#Rendering)
* [Stack](#Stack)
* [Environment](#Environment)
* [Output](#Output)

Overlay cameras expose the following properties:

* [Projection](#Projection)
* [Physical Camera](#PhysicalCamera)
* [Rendering](#Rendering)
* [Environment](#Environment)

<a name="Projection"></a>

## Projection

| **Property** | **Description** |
| ------------------------ | :-------------------------------------------------------------------- |
| **Projection** | Control how the camera simulates perspective. |
| &#160;&#160;&#160;&#160;**Perspective** | Render objects with perspective intact. |
| &#160;&#160;&#160;&#160;**Orthographic** | Render objects uniformly, with no sense of perspective. |
| **Field of View Axis** | Set the axis Unity measures the camera's field of view along.<br/><br/>Available options:<ul><li>**Vertical**</li><li>**Horizontal**</li></ul>This property is only visible when **Projection** is set to **Perspective**. |
| **Field of View** | Set the width of the camera's view angle, measured in degrees along the selected axis.<br/><br/>This property is only visible when **Projection** is set to **Perspective**. |
| **Size** | Set the viewport size of the camera.<br/><br/>This property is only visible when **Projection** is set to **Orthographic**. |
| **Clipping Planes** | Set the distances from the camera where rendering starts and stops. |
| &#160;&#160;&#160;&#160;**Near** | The closest point relative to the camera where drawing occurs. |
| &#160;&#160;&#160;&#160;**Far** | The furthest point relative to the camera where drawing occurs. |
| **Physical Camera** | Displays additional properties for the camera in the Inspector to simulate a physical camera. A physical camera calculates the Field of View with properties simulating real-world camera attributes: **Focal Length**, **Sensor Size**, and **Shift**.<br/><br/>The **Physical Camera** property is only available when **Projection** is set to **Perspective**. |

<a name="PhysicalCamera"></a>

### Physical Camera

The **Physical Camera** property adds additional properties to the camera to simulate a real-world camera. For more information, refer to the [Physical Camera reference](cameras/physical-camera-reference.md).

<a name="Rendering"></a>

## Rendering

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Renderer** | Select which renderer this camera uses. |
| **Post Processing** | Enable post-processing effects. |
| **Anti-Aliasing** | Select the method that this camera uses for post-process anti-aliasing. A camera can still use Multisample Anti-aliasing (MSAA), which is a hardware feature, at the same time as post-process anti-aliasing unless you use Temporal Anti-aliasing.<br/><br/>The following Anti-aliasing options are available:<ul><li>**None**: This camera can process MSAA but does not process any post-process anti-aliasing.</li><li>**Fast Approximate Anti-aliasing (FXAA)**: Performs a full screen pass which smooths edges on a per-pixel level.</li><li>**Subpixel Morphological Anti-aliasing (SMAA)**: Finds edge patterns in the image and blends the pixels on these edges according to those patterns.</li><li>**Temporal Anti-aliasing (TAA)**: Uses previous frames accumulated into a color history buffer to smooth edges over the course of multiple frames.</li></ul>For more information, refer to [Anti-aliasing in the Universal Render Pipeline](anti-aliasing.md).<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Quality (SMAA)** | Select the quality of SMAA. The difference in resource intensity is fairly small between **Low** and **High**.<br/><br/>Available options:<ul><li>**Low**</li><li>**Medium**</li><li>**High**</li></ul>This property only appears when you select **Subpixel Morphological Anti-aliasing (SMAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Quality (TAA)** | Select the quality of TAA.<br/><br/>Available options:<ul><li>**Very Low**</li><li>**Low**</li><li>**Medium**</li><li>**High**</li><li>**Very High**</li></ul>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Contrast Adaptive Sharpening** | Enable high quality post sharpening to reduce TAA blur.<br/><br/>This setting is overridden when you enable either [AMD FidelityFX Super Resolution (FSR) or Scalable Temporal Post-Processing (STP)](universalrp-asset.md#quality) upscaling in the URP Asset as they both handle sharpening as part of the upscaling process. <br/><br/>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Base Blend Factor** | Set how much the history buffer blends with the current frame result. Higher values mean more history contribution, which improves the anti-aliasing, but also increases the chance of ghosting.<br/><br/>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Jitter Scale** | Set the scale of the jitter applied when TAA is enabled. A lower value reduces visible flickering and jittering, but also reduces the effectiveness of the anti-aliasing.<br/><br/>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Mip Bias** | Set how much texture mipmap selection is biased when rendering.<br/><br/>A positive bias makes a texture appear more blurry, while a negative bias sharpens the texture. However, a lower value also has a negative impact on performance.<br/><br/>**Note**: Requires mipmaps in textures.<br/><br/>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Variance Clamp Scale** | Set the size of the color volume Unity uses to find nearby pixels when the color history is incorrect or unavailable. The clamp limits how much a pixel's color can vary from the color of the surrounding pixels.<br/><br/>Lower values can reduce ghosting, but produce more flickering. Higher values reduce flickering, but are prone to blur and ghosting.<br/><br/>This property only appears when you select **Temporal Anti-aliasing (TAA)** from the **Anti-aliasing** drop-down and enable **Show Additional Properties** in the Inspector. |
| **Stop NaNs** | Replaces Not a Number (NaN) values with a black pixel for the camera. This stops certain effects from breaking, but is a resource-intensive process which causes a negative performance impact. Only enable this feature if you experience NaN issues you can't fix.<br/><br/>The Stop NaNs pass executes at the start of the post-processing passes. You must enable **Post Processing** for the camera to use **Stop NaNs**.<br/><br/>Only available when **Render Type** is set to **Base**. |
| **Dithering** | Enable to apply 8-bit dithering to the final render to help reduce banding on wide gradients and low light areas.<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| **Clear Depth** | Enable to clear depth from previous camera on rendering.<br/><br/>This property is only visible when **Render Type** is set to **Overlay**. |
| **Render Shadows** | Enable shadow rendering. |
| **Priority** | A camera with a higher priority is drawn on top of a camera with a lower priority. Priority has a range from -100 to 100.<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| **Opaque Texture** | Control whether the camera creates a CameraOpaqueTexture, which is a copy of the rendered view.<br/><br/>Available options:<ul><li>**Off**: Camera does not create a CameraOpaqueTexture.</li><li>**On**: Camera creates a CameraOpaqueTexture.</li><li>**Use Pipeline Settings**: The Render Pipeline Asset determines the value of this setting.</li></ul>This property is only visible when **Render Type** is set to **Base**. |
| **Depth Texture** | Control whether the camera creates `_CameraDepthTexture`, which is a copy of the rendered depth values.<br/><br/>Available options:<ul><li>**Off**: Camera does not create a CameraDepthTexture.</li><li>**On**: Camera creates a CameraDepthTexture.</li><li>**Use Pipeline Settings**: The Render Pipeline Asset determines the value of this setting.</li></ul>**Note**: `_CameraDepthTexture` is set between the `AfterRenderingSkybox` and `BeforeRenderingTransparents` events, or at the `BeforeRenderingOpaques` event if you use a depth prepass. For more information on the order of events in the rendering loop, refer to [Injection points](customize/custom-pass-injection-points.md). |
| **Culling Mask** | Select which Layers the camera renders to. |
| **Occlusion Culling** | Enable Occlusion Culling. |

<a name="Stack"></a>

## Stack

> [!NOTE]
> This section is only available if **Render Type** is set to **Base**

A camera stack allows to composite results of several cameras together. The camera stack consists of a Base camera and any number of additional Overlay cameras.

You can use the stack property add Overlay cameras to the stack and they will render in the order as defined in the stack. For more information on configuring and using camera stacks, refer to [Set up a camera stack](camera-stacking.md).

<a name="Environment"></a>

## Environment

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Background Type** | Control how to initialize the color buffer at the start of this camera's render loop. For more information, refer to [the documentation on clearing](cameras-advanced.md#clearing).<br/><br/>This property is only visible when **Render Type** is set to **Base**. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Skybox** | Initializes the color buffer by clearing to a Skybox. Defaults to a background color if no Skybox is found. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Solid Color** | Initializes the color buffer by clearing to a given color.<br/>If you select this property, Unity shows the following extra property:<br/>**Background**: The camera clears its color buffer to this color before rendering. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Uninitialized** | Does not initialize the color buffer. This means that the load action for that specific RenderTarget will be `DontCare` instead of `Load` or `Clear`. `DontCare` specifies that the previous contents of the RenderTarget don't need to be preserved.<br/><br/>Only use this option in order to optimize performance in situations where your camera or Camera Stack will draw to every pixel in the color buffer, otherwise the behaviour of pixels the camera doesn't draw is undefined.<br/><br/>**Note**: The results might look different between Editor and Player, as the Editor doesn't run on Tile-Based Deferred Rendering (TBDR) GPUs (found in mobile devices). If you use this option on TBDR GPUs, it causes uninitialized tile memory and the content is undefined. |
| **Volumes** | The settings in this section define how Volumes affect this camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Update**&#160;**Mode** | Select how Unity updates Volumes.<br/><br/>Available options:<ul><li>**Every Frame**: Update Volumes with every frame Unity renders.</li><li>**Via Scripting**: Only update volumes when triggered by a script.</li><li>**Use Pipeline Settings**: Use the default setting for the Render Pipeline.</li></ul> |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Volume**&#160;**Mask** | Use the drop-down to set the Layer Mask that defines which Volumes affect this camera. |
| &#160;&#160;&#160;&#160;&#160;&#160;&#160;&#160;**Volume**&#160;**Trigger** | Assign a Transform that the [Volume](Volumes.md) system uses to handle the position of this camera. For example, if your application uses a third person view of a character, set this property to the character's Transform. The camera then uses the post-processing and scene settings for Volumes that the character enters. If you do not assign a Transform, the camera uses its own Transform instead. |

<a name="Output"></a>

## Output

This section is only available if you set the **Render Type** to **Base**

> [!NOTE]
> When a camera's **Render Type** is set to **Base** and its **Render Target** is set to **Texture**, Unity does not show the following properties in the Inspector for the camera:
>
> * **Target Display**
> * **HDR rendering**
> * **MSAA**
> * **Allow Dynamic Resolution**
>
> This is because the Render Texture determines these properties. You can change them in the Render Texture Asset.

| **Property**               | **Description**                                              |
| -------------------------- | ------------------------------------------------------------ |
| **Output Texture** | Render this camera's output to a RenderTexture if this field is assigned, otherwise render to the screen. |
| **Target Display** | Select which external device to render to. |
| **Target Eye** | Select the target eye for this camera.<br/><br/>Available options:<ul><li>**Both**: Allows XR rendering from the selected camera.</li><li>**None**: Disables XR rendering for the selected camera.</li></ul> |
| **Viewport Rect** | Four values that indicate where on the screen this camera view is drawn. Measured in Viewport Coordinates (values 0-1). |
| &#160;&#160;&#160;&#160;**X** | The beginning horizontal position Unity uses to draw the camera view. |
| &#160;&#160;&#160;&#160;**Y** | The beginning vertical position Unity uses to draw the camera view. |
| &#160;&#160;&#160;&#160;**W** | Width of the camera output on the screen. |
| &#160;&#160;&#160;&#160;**H** | Height of the camera output on the screen. |
| **HDR Rendering** | Enable High Dynamic Range rendering for this camera. |
| **MSAA** | Enable [Multisample Anti-aliasing](anti-aliasing.md#msaa) for this camera. |
| **Allow Dynamic Resolution** | Enable Dynamic Resolution rendering for this camera. |
