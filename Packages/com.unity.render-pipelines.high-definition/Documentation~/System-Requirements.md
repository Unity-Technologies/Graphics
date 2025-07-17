# System requirements and compatibility

This page contains information on system requirements and compatibility of the High Definition Render Pipeline (HDRP) package.

## Unity Editor compatibility

HDRP is a [core Unity package](../pack-core). For each alpha, beta or patch release of Unity, the main Unity installer contains the up-to-date version of the package.

The Package Manager window displays only the major and minor revision of the package. For example, version 17.2.0 for all Unity 6.2.x releases.

You can install a different version of a graphics package from disk using the Package Manager, or by modifying the `manifest.json` file.

## Render pipeline compatibility

Projects made using HDRP aren't compatible with the Universal Render Pipeline (URP) or the Built-in Render Pipeline. Before you start development, you must decide which render pipeline to use in your Project. For information on choosing a render pipeline, refer to the [Render Pipelines](https://docs.unity3d.com/Manual/render-pipelines.html) section of the Unity Manual.

## Unity Player system requirements

This section describes the HDRP package’s target platform requirements. For platforms or use cases not covered in this section, general system requirements for the Unity Player apply.

For more information, refer to [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).

HDRP is compatible with the following platforms:

- Windows and Windows Store, with DirectX 11 or DirectX 12 and Shader Model 5.0
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

To use ray tracing in HDRP, there are hardware requirements you must meet. For information on these requirements, refer to [Getting started with ray tracing](Ray-Tracing-Getting-Started.md).
