# Screen Space Reflection

The **Screen Space Reflection** (SSR) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate reflections. For information about how SSR works in HDRP, see the [reflection documentation](Reflection-in-HDRP.md#ScreenSpaceReflection).

HDRP implements [ray-traced reflection](Ray-Traced-Reflections.md) on top of this override. This means that the properties visible in the Inspector change depending on whether or not you enable ray tracing.

## Enabling Screen Space Reflection

[!include[](snippets/Volume-Override-Enable-Override.md)]

To enable Screen Space Reflection:

1. In your HDRP Asset Inspector window, go to **Lighting** > **Reflections** and enable **Screen Space Reflection**

2. Enable Screen Space Reflection in Frame Settings

   * If you want to enable Screen Space Reflection for all cameras, go to **Edit** > **Project Settings** > **Graphics** > **HDRP Global Settings** > **Frame Settings (Default Values)** > **Lighting** and enable **Screen Space Reflection**
   * If you want to enable Screen Space Reflection for specific cameras, in the Inspector window of each camera:
      1. Go to **Rendering** and enable **Custom Frame Settings**
      2. Go to **Frame Settings Overrides** > **Lighting** and enable **Screen Space Reflection**

## Using Screen Space Reflection

HDRP uses the [Volume](Volumes.md) framework to calculate SSR, so to enable and modify SSR properties, you must add a **Screen Space Reflection** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Reflection** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Reflection**.
   HDRP now calculates SSR for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

[!include[](snippets/tracing-modes.md)]

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

### Screen-space

![](Images/Override-ScreenSpaceReflection1.png)

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Enable (Opaque)**           | Indicates whether HDRP processes SSR on opaque objects for Cameras in the influence of this effect's Volume. |
| **Enable (Transparent)**      | Indicates whether HDRP processes SSR on transparent objects for Cameras in the influence of this effect's Volume. |
| **Tracing**                   | Specifies the method HDRP uses to calculate reflections. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see [tracing modes](#tracing-modes). The options are:<br/>&#8226; **Ray Marching**: Uses a screen-space ray marching solution to calculate reflections. For the list of properties this option exposes, see [Screen-space](#screen-space).<br/>&#8226; **Ray Tracing**: Uses ray tracing to calculate reflections. For information on ray-traced reflections, see [ray-traced reflection](Ray-Traced-Reflections.md). For the list of properties this option exposes, see [Ray-traced](#ray-traced).<br/>&#8226; **Mixed**: Uses a combination of ray tracing and ray marching to calculate reflections. For the list of properties this option exposes, see [Ray-traced](#ray-traced). |
| **Algorithm**                 | Specifies the algorithm to use for the screen-space reflection effect. The options are:<br/>&#8226; **Approximation**: Approximates screen-space reflection to quickly calculate a result. This solution is less precise than **PBR Accumulation**, particularly for rough surfaces, but is less resource intensive.<br/>&#8226; **PBR Accumulation**: Accumulates multiple frames to calculate a more accurate result. You can control the amount of accumulation using **Accumulation Factor**. This solution might produce more ghosting than **Approximation**, due to the accumulation, and is also more resources intensive. HDRP does not apply this algorithm to transparent material and instead always uses **Approximation**. |
| **Minimum Smoothness**        | Use the slider to set the minimum amount of surface smoothness at which HDRP performs SSR tracing. Lower values result in HDRP performing SSR tracing for less smooth GameObjects. If the smoothness value of the pixel is lower than this value, HDRP falls back to the next available reflection method in the [reflection hierarchy](Reflection-in-HDRP.md#ReflectionHierarchy). |
| **Smoothness Fade Start**     | Use the slider to set the smoothness value at which SSR reflections begin to fade out. Lower values result in HDRP fading out SSR reflections for less smooth GameObjects. The fade is in the range [Minimum Smoothness, Smoothness Fade Start]. |
| **Reflect Sky**               | Indicates whether HDRP should use SSR to handle sky reflection. If you disable this property, pixels that reflect the sky use the next level of the [reflection hierarchy](Reflection-in-HDRP.md#ReflectionHierarchy).<br />**Note**: SSR uses the depth buffer to calculate reflection and HDRP does not add transparent GameObjects to the depth buffer. If you enable this property, transparent GameObject that appear over the sky in the color buffer can cause visual artifacts and incorrect looking reflection. This is a common limitation for SSR techniques. |
| **Screen Edge Fade Distance** | Use the slider to control the distance at which HDRP fades out screen space reflections when the destination of the ray is near the boundaries of the screen. Increase this value to increase the distance from the screen edge at which HDRP fades out screen space reflections for a ray destination. |
| **Object Thickness**          | Use the slider to control the thickness of the GameObjects on screen. Because the SSR algorithm can not distinguish thin GameObjects from thick ones, this property helps trace rays behind GameObjects. The algorithm applies this property to every GameObject uniformly. |
| **Quality**                   | Specifies the quality level to use for this effect. Each quality level applies different preset values. Unity also stops you from editing the properties that the preset overrides. If you want to set your own values for every property, select **Custom**. |
| - **Max Ray Steps**           | Sets the maximum number of iterations that the algorithm can execute before it stops trying to find an intersection with a Mesh. For example, if you set the number of iterations to 1000 and the algorithm only needs 10 to find an intersection, the algorithm terminates after 10 iterations. If you set this value too low, the algorithm may terminate too early and abruptly stop reflections. |
| **Accumulation Factor**       | The speed of the accumulation convergence. 0 means no accumulation. 1 means accumulation is very slow which is useful for fixed images. The more accumulation, the more accurate the result but the more ghosting occurs when moving. When using accumulation, it is important to find a balance between convergence quality and the ghosting artifact. Also note that rougher reflective surfaces require more accumulation to produce a converged image without noise.<br/>This property only appears if you set **Algorithm** to **PBR Accumulation**. |

### Ray-traced

![](Images/Override-ScreenSpaceReflection2.png)

| Property                      | Description                                                  |
| ----------------------------- | ------------------------------------------------------------ |
| **Tracing**                   | Specifies the method HDRP uses to calculate reflections. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see [tracing modes](#tracing-modes). The options are:<br/>&#8226; **Ray Marching**: Uses a screen-space ray marching solution to calculate reflections. For the list of properties this option exposes, see [Screen-space](#screen-space).<br/>&#8226; **Ray Tracing**: Uses ray tracing to calculate reflections. For information on ray-traced reflections, see [ray-traced reflection](Ray-Traced-Reflections.md). For the list of properties this option exposes, see [Ray-traced](#ray-traced).<br/>&#8226; **Mixed**: Uses a combination of ray tracing and ray marching to calculate reflections. For the list of properties this option exposes, see [Ray-traced](#ray-traced). |
| **Ray Miss**                  | Determines what HDRP does when ray-traced reflections (RTR) doesn't find an intersection. Choose from one of the following options: <br/>&#8226;**Reflection probes**: HDRP uses reflection probes in your scene to calculate the last RTR bounce.<br/>&#8226;**Sky**: HDRP uses the sky defined by the current [Volume](Volumes.md) settings to calculate the last RTR bounce.<br/>&#8226;**Both** : HDRP uses both reflection probes and the  the sky defined by the current [Volume](Volumes.md) settings to calculate the last RTR bounce.<br/>&#8226;**Nothing**: HDRP does not calculate indirect lighting when RTR doesn't find an intersection.<br/><br/>This property is set to **Both** by default|
| **Last Bounce**               | Determines what HDRP does when ray-traced reflections (RTR) lights the last bounce. Choose from one of the following options: <br/>&#8226;**Reflection probes**: HDRP uses reflection probes in your scene to calculate the last RTR bounce.<br/>&#8226;**Sky**: HDRP uses the sky defined by the current [Volume](Volumes.md) settings to calculate the last RTR bounce.<br/>&#8226;**Both**:  HDRP uses both reflection probes and the sky defined by the current [Volume](Volumes.md) settings to calculate the last RTR bounce.<br/>&#8226;**Nothing**: HDRP does not calculate indirect lighting when it evaluates the last bounce.<br/><br/>This property is set to **Both** by default. |
| **LayerMask**                 | Defines the layers that HDRP processes this ray-traced effect for. |
| **Mode**                      | Defines if HDRP should evaluate the effect in **Performance** or **Quality** mode.<br/>This property only appears if you select set **Supported Ray Tracing Mode** in your HDRP Asset to **Both**. |
| **Quality**                   | Specifies the preset HDRP uses to populate the values of the following nested properties. The options are:<br/>&#8226; **Low**: A preset that emphasizes performance over quality.<br/>&#8226; **Medium**: A preset that balances performance and quality.<br/>&#8226; **High**: A preset that emphasizes quality over performance.<br/>&#8226; **Custom**: Allows you to override each property individually.<br/>This property only appears if you set **Mode** to **Performance**. |
| **Minimum Smoothness**        | See **Minimum Smoothness** in [Screen-space](#screen-space). |
| **Smoothness Fade Start**     | See **Smoothness Fade Start** in [Screen-space](#screen-space). |
| **Max Ray Length**            | Controls the maximum length of reflection rays in meters. The higher this value is, the more resource-intensive ray-traced reflection is if a ray doesn't find an intersection. |
| **Clamp Value**               | Controls the threshold that HDRP uses to clamp the pre-exposed value. This reduces the range of values and makes the reflections more stable to denoise, but reduces quality. |
| **Full Resolution**           | Enable this feature to increase the ray budget to one ray per pixel, per frame. Disable this feature to decrease the ray budget to one ray per four pixels, per frame.<br/>This property only appears if you set **Mode** to **Performance**. |
| **Sample Count**              | Controls the number of rays per pixel per frame. Increasing this value increases execution time linearly.<br/>This property only appears if you set **Mode** to **Quality**. |
| **Bounce Count**              | Controls the number of bounces that reflection rays can do. Increasing this value increases execution time exponentially.<br/>This property only appears if you set **Mode** to **Quality**. |
| **Max Mixed Ray Steps**       | Sets the maximum number of iterations that the algorithm can execute before it stops trying to find an intersection with a Mesh. For example, if you set the number of iterations to 1000 and the algorithm only needs 10 to find an intersection, the algorithm terminates after 10 iterations. If you set this value too low, the algorithm may terminate too early and abruptly stop reflections. This property only appears if you set **Tracing** to **Mixed**. |
| **Denoise**                   | Enables the spatio-temporal filter that HDRP uses to remove noise from the reflections. |
| - **Denoiser Radius**         | Controls the radius of the spatio-temporal filter. Increasing this value results in a more blurry result and a higher execution time. |
| - **Affects Smooth Surfaces** | Indicates whether the denoiser affects perfectly smooth surfaces (surfaces with a **Smoothness** of 1.0) or not. |

## Limitations

### Screen-space reflection

To calculate SSR, HDRP reads a color buffer with a blurred mipmap generated during the previous frame. The color buffer only includes transparent GameObjects that use the **BeforeRefraction** [Rendering Pass](Surface-Type.md).

If a transparent material has **Receive SSR Transparent** enabled, HDRP always uses the **Approximation** algorithm to calculate SSR, even you select **PBR Accumulation**.

When a transparent material has rendering pass set to **Low Resolution**, then **Receive SSR Transparent** can't be selected.

### Ray-traced reflection

Currently, ray tracing in HDRP does not support [decals](decal.md). This means that, when you use ray-traced reflection, decals do not appear in reflective surfaces.

If a transparent material has **Receive SSR Transparent** enabled, HDRP will evaluate the reflections as smooth.
