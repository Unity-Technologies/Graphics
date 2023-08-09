# Requirements and compatibility

This page contains information on system requirements and compatibility of the High Definition Render Pipeline (HDRP) package.

## Unity Editor compatibility

The following table shows the compatibility of the High Definition Render Pipeline (HDRP) versions with different Unity Editor versions.

| **Package version** | **Minimum Unity version** | **Maximum Unity version** |
| --------------- | --------------------- | --------------------- |
| 16.0.x          | 2023.2                | 2023.x                |
| 15.0.x          | 2023.1                | 2023.1                |
| 14.0.x          | 2022.2                | 2022.x                |
| 13.x.x          | 2022.1                | 2022.1                |
| 12.0.x          | 2021.2                | 2021.3                |
| 11.x            | 2021.1                | 2021.1                |
| 10.x            | 2020.2                | 2020.3                |
| 9.x-preview     | 2020.1                | 2020.1                |
| 8.x             | 2020.1                | 2020.1                |
| 7.x             | 2019.3                | 2019.4                |
| 6.x             | 2019.2                | 2019.2                |

## Render pipeline compatibility

Projects made using HDRP aren't compatible with the Universal Render Pipeline (URP) or the Built-in Render Pipeline. Before you start development, you must decide which render pipeline to use in your Project. For information on choosing a render pipeline, see the [Render Pipelines](https://docs.unity3d.com/2019.3/Documentation/Manual/render-pipelines.html) section of the Unity Manual.

## Unity Player system requirements

This section describes the HDRP package’s target platform requirements. For platforms or use cases not covered in this section, general system requirements for the Unity Player apply.

For more information, see [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).

HRDP is only compatible with the following platforms:

- Windows and Windows Store, with DirectX 11 or DirectX 12 and Shader Model 5.0
- Google
  - Stadia
- Sony
  - PlayStation 4
  - PlayStation 5
- Microsoft
  - Xbox One
  - Xbox Series X and Xbox Series S
- MacOS (minimum version 10.13) using Metal graphics
- Linux and Windows platforms with Vulkan

**Note**: HDRP only works on these platforms if the device you use supports Compute Shaders. HDRP doesn't support OpenGL or OpenGL ES devices. On Linux, Vulkan might not be installed by default. In that case you need to install it manually to run HDRP.

### Ray tracing

To use ray tracing in HDRP, there are hardware requirements you must meet. For information on these requirements, see [Getting started with ray tracing](Ray-Tracing-Getting-Started.md#HardwareRequirements).
