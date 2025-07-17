# Requirements and compatibility

This page contains information on system requirements and compatibility for the Visual Effect Graph package.

## Unity Editor compatibility

Visual Effect Graph is a [core Unity package](../pack-core). For each alpha, beta or patch release of Unity, the main Unity installer contains the up-to-date version of the package.

The Package Manager window displays only the major and minor revision of the package. For example, version 17.2.0 for all Unity 6.2.x releases.

You can install a different version of a graphics package from disk using the Package Manager, or by modifying the `manifest.json` file.

## Render pipeline compatibility

Visual Effect Graph is compatible with the Universal Render Pipeline (URP) and the High Definition Render Pipeline (HDRP).

**Note**: When you download the HDRP package from the Package Manager, Unity automatically installs the Visual Effect Graph package.

**Note**: In URP, the Visual Effect Graph doesn't support [gamma color space](https://docs.unity3d.com/Manual/LinearRendering-LinearOrGammaWorkflow.html).

## Unity Player system requirements

- The Unity Player system requirements for the Visual Effect Graph depend on which render pipeline you use:
  - The Visual Effect Graph is out of preview for HDRP, which means it supports every platform that HDRP supports. For information on which platforms this includes, see HDRP's [system requirements](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html).
  - The Visual Effect Graph isn't out of preview for URP, which means it only supports some of the platforms that URP supports.
- For both render pipelines, the minimum hardware requirements are:
  - Support for compute shaders. If a platform supports compute shaders, it returns `true` for [SystemInfo.supportsComputeShaders](https://docs.unity3d.com/ScriptReference/SystemInfo-supportsComputeShaders.html).
  - Support for Shader Storage Buffer Objects (SSBOs). If a platform supports SSBOs, it returns a value greater than 0 for [SystemInfo.maxComputeBufferInputsVertex](https://docs.unity3d.com/ScriptReference/SystemInfo-maxComputeBufferInputsVertex.html).
- The Visual Effect Graph isn't out of preview for mobile platforms.
- The Visual Effect Graph does not support Open GL ES.

For more information on general system requirements for the Unity Player, see [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).
