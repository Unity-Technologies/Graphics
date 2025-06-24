# Spatial-Temporal Post-processing

Spatial-Temporal Post-Processing (STP) uses spatial and temporal upsampling techniques to produce a high quality, anti-aliased image.

STP performance is consistent across platforms that support it and does not require platform-specific configuration.

## Requirements

STP is a software-based upscaler. STP uses compute shaders, so target devices must support [Shader Model 5.0](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/d3d11-graphics-reference-sm5) to use STP.

## STP performance

STP configures itself automatically to provide the best balance of performance and quality based on the platform your application runs on. STP performance is consistent across different target platforms.

On high-performance platforms, like PCs and consoles, STP uses higher quality image filtering logic and additional deringing logic to improve image quality when it upscales images. These techniques require additional processing power and Unity uses them on high-performance devices where the performance impact is not significant.

On mobile devices, STP uses more performant image filtering logic to provide a balance between performance and image quality. This minimizes the performance impact of STP on less powerful devices, while still delivering a high quality image.

### STP in the Rendering Debugger

There are a several debug views available for STP within the Rendering Debugger. For more information on the STP debug views, refer to [Spatial Temporal Post-processing debug views](stp-debug-views.md).

## How to use STP

To enable STP in the High Definition Render Pipeline (HDRP), do the following:

1. Select the active HDRP Asset in the Project window.

2. In the Inspector, go to **Rendering** > **Dynamic Resolution** and select **Enable**.

2. In the list **Advanced Upscalers by Priority**, click the **+** button and select **STP** to add it to the list.

3. Set **Dynamic Resolution Type** to **Hardware**.

STP remains active when **Render Scale** is set to **1.0** as it applies temporal anti-aliasing (TAA) to the final rendered output.

## Additional resources

- [Introduction to changing resolution scale](https://docs.unity3d.com/6000.0/Documentation/Manual/resolution-scale-introduction.html)

