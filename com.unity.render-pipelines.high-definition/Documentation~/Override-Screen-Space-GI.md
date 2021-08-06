# Screen Space Global Illumination

The **Screen Space Global Illumination** (SSGI) override is a High Definition Render Pipeline (HDRP) feature that uses the depth and color buffer of the screen to calculate diffuse light bounces.

HDRP implements [ray-traced global illumination](Ray-Traced-Global-Illumination.md) (RTGI) on top of this override. This means that the properties visible in the Inspector change depending on whether or not you enable ray tracing.

SSGI and RTGI completely replace all [lightmap](https://docs.unity3d.com/Manual/Lightmapping.html) and [Light Probe](https://docs.unity3d.com/Manual/LightProbes.html) data. If you enable this override and the Volume affects the Camera, Light Probes, and the ambient probe, stop contributing to lighting for GameObjects.

![](Images/HDRPFeatures-SSGI.png)

## Enabling Screen Space Global Illumination
[!include[](Snippets/Volume-Override-Enable-Override.md)]

For this feature:
The property to enable in your HDRP Asset is: **Lighting > Screen Space Global Illumination**.
The property to enable in your Frame Settings is: **Lighting > Screen Space Global Illumination**.

## Using Screen Space Global Illumination

HDRP uses the [Volume](Volumes.md) framework to calculate SSGI, so to enable and modify SSGI properties, you must add a **Screen Space Global Illumination** override to a [Volume](Volumes.md) in your Scene. To add **Screen Space Global Illumination** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, navigate to **Add Override > Lighting** and click **Screen Space Global Illumination**.
   HDRP now calculates SSGI for any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

[!include[](snippets/tracing-modes.md)]

## Properties

[!include[](Snippets/Volume-Override-Enable-Properties.md)]

### Screen-space

![](Images/Override-ScreenSpaceGlobalIllumination1.png)

| **Property**        | **Description**                                              |
| ------------------- | ------------------------------------------------------------ |
| **Enable**          | Indicates whether HDRP processes SSGI for Cameras in the influence of this effect's Volume. |
| **Tracing**         | Specifies the method HDRP uses to calculate global illumination. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see [tracing modes](#tracing-modes). The options are:<br/>&#8226; **Ray Marching**: Uses a screen-space ray marching solution to calculate global illumination. For the list of properties this option exposes, see [Screen-space](#screen-space).<br/>&#8226; **Ray Tracing**: Uses ray tracing to calculate global illumination. For information on ray-traced global illumination, see [ray-traced global illumination](Ray-Traced-Global-Illumination.md). For the list of properties this option exposes, see [Ray-traced](#ray-traced).<br/>&#8226; **Mixed**: Uses a combination of ray tracing and ray marching to calculate global illumination. For the list of properties this option exposes, see [Ray-traced](#ray-traced). |
| **Quality**         | Specifies the overall quality of the effect. The higher the quality, the more resource-intensive the effect is to process. |
| **Max Ray Steps**   | The number of ray steps to use to calculate SSGI. If you set this to a higher value, the quality of the effect improves, however it is more resource intensive to process. |
| **Denoise**                    | Enable this to enable the spatio-temporal filter that HDRP uses to remove noise from the Ray-Traced global illumination. |
| - **Half Resolution Denoiser** | Enable this feature to evaluate the spatio-temporal filter in half resolution. This decreases the resource intensity of denoising but reduces quality. |
| - **Denoiser Radius**          | Set the radius of the spatio-temporal filter.                |
| - **Second Denoiser Pass**     | Enable this feature to process a second denoiser pass. This helps to remove noise from the effect. |
| **Depth Tolerance** | Use the slider to control the tolerance when comparing the depth of the GameObjects on screen and the depth buffer. Because the SSR algorithm can not distinguish thin GameObjects from thick ones, this property helps trace rays behind GameObjects. The algorithm applies this property to every GameObject uniformly. |

### Ray-traced

![](Images/Override-ScreenSpaceGlobalIllumination2.png)

| Property                       | Description                                                  |
| ------------------------------ | ------------------------------------------------------------ |
| **Tracing**                    | Specifies the method HDRP uses to calculate global illumination. Depending on the option you select, the properties visible in the Inspector change. For more information on what the options do, see [tracing modes](#tracing-modes). The options are:<br/>&#8226; **Ray Marching**: Uses a screen-space ray marching solution to calculate global illumination. For the list of properties this option exposes, see [Screen-space](#screen-space).<br/>&#8226; **Ray Tracing**: Uses ray tracing to calculate global illumination. For information on ray-traced global illumination, see [ray-traced global illumination](Ray-Traced-Global-Illumination.md). For the list of properties this option exposes, see [Ray-traced](#ray-traced).<br/>&#8226; **Mixed**: Uses a combination of ray tracing and ray marching to calculate global illumination. For the list of properties this option exposes, see [Ray-traced](#ray-traced). |
| **LayerMask**                  | Defines the layers that HDRP processes this ray-traced effect for. |
| **Mode**                       | Defines if HDRP should evaluate the effect in **Performance** or **Quality** mode.<br/>This property only appears if you select set **Supported Ray Tracing Mode** in your HDRP Asset to **Both**. |
| **Quality**                    | Specifies the preset HDRP uses to populate the values of the following nested properties. The options are:<br/>&#8226; **Low**: A preset that emphasizes performance over quality.<br/>&#8226; **Medium**: A preset that balances performance and quality.<br/>&#8226; **High**: A preset that emphasizes quality over performance.<br/>&#8226; **Custom**: Allows you to override each property individually.<br/>This property only appears if you set **Mode** to **Performance**. |
| **Max Ray Length**             | Controls the maximal length of rays. The higher this value is, the more resource-intensive ray traced global illumination is. |
| **Clamp Value**                | Set a value to control the threshold that HDRP uses to clamp the pre-exposed value. This reduces the range of values and makes the global illumination more stable to denoise, but reduces quality. |
| **Full Resolution**            | Enable this feature to increase the ray budget to one ray per pixel, per frame. Disable this feature to decrease the ray budget to one ray per four pixels, per frame.<br/>This property only appears if you set **Mode** to **Performance**. |
| **Sample Count**               | Controls the number of rays per pixel per frame. Increasing this value increases execution time linearly.<br/>This property only appears if you set **Mode** to **Quality**. |
| **Bounce Count**               | Controls the number of bounces that global illumination rays can do. Increasing this value increases execution time exponentially.<br/>This property only appears if you set **Mode** to **Quality**. |
| **Max Mixed Ray Steps**        | Sets the maximum number of iterations that the algorithm can execute before it stops trying to find an intersection with a Mesh. For example, if you set the number of iterations to 1000 and the algorithm only needs 10 to find an intersection, the algorithm terminates after 10 iterations. If you set this value too low, the algorithm may terminate too early and abruptly stop global illumination. This property only appears if you set **Tracing** to **Mixed**. |
| **Denoise**                    | Enable this to enable the spatio-temporal filter that HDRP uses to remove noise from the Ray-Traced global illumination. |
| - **Half Resolution Denoiser** | Enable this feature to evaluate the spatio-temporal filter in half resolution. This decreases the resource intensity of denoising but reduces quality. |
| - **Denoiser Radius**          | Set the radius of the spatio-temporal filter.                |
| - **Second Denoiser Pass**     | Enable this feature to process a second denoiser pass. This helps to remove noise from the effect. |

## Limitations

### Screen-space global illumination

* When rendering [Reflection Probes](Reflection-Probe.md) screen space global illumination is not supported.

### Ray-traced global illumination

* Currently, ray tracing in HDRP does not support [decals](decal.md). This means that ray-traced global illumination does not affect decals in your Scene.

  ### Mixed global illumination

* In Mixed tracing mode, emissive decals do not contribute to global illumination because HDRP renders them later in the render pipeline. For the same reason, emissive Materials that use Force Forward Emissive also do not contribute to global illumination.
