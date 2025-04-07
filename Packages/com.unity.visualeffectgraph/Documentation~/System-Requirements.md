# Requirements and compatibility

This page contains information on system requirements and compatibility for the Visual Effect Graph package.

## Unity Editor compatibility

The following table shows the compatibility of the Visual Effect Graph versions with different Unity Editor versions.

| **Package version** | **Minimum Unity version** | **Maximum Unity version** |
| ------------------- | ------------------------- | ------------------------- |
| 17.x                | Unity 6                   | Unity 6.1                 |
| 16.x                | 2023.2                    | 2023.2                    |
| 15.x                | 2023.1                    | 2023.1                    |
| 14.x                | 2022.2                    | 2022.2                    |
| 13.x                | 2022.1                    | 2022.1                    |
| 12.x                | 2021.2                    | 2021.2                    |
| 11.x                | 2021.1                    | 2021.1                    |
| 10.x                | 2020.2                    | 2020.3                    |
| 8.x / 9.x-preview   | 2020.1                    | 2020.1                    |
| 7.x                 | 2019.3                    | 2019.4                    |
| 6.x                 | 2019.2                    | 2019.2                    |

## Render pipeline compatibility

The Visual Effect Graph varies in compatibility between the High Definition Render Pipeline (HDRP) and the Universal Render Pipeline (URP). This section describes the compatibility of Visual Effect Graph versions with different render pipelines.

| **Package version** | **HDRP**   | **URP**       |
| ------------------- | ---------- | ------------- |
| 17.x                | Supported  | Supported     |
| 16.x                | Supported  | Supported     |
| 14.x                | Supported  | Supported     |
| 13.x                | Supported  | Supported     |
| 12.x                | Supported  | Supported     |
| 11.x                | Supported  | In preview    |
| 10.x                | Supported  | In preview    |
| 8.x / 9.x-preview   | Supported  | In preview    |
| 7.x                 | Supported  | In preview    |
| 6.x                 | In preview | Not supported |

The Visual Effect Graph supports the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html) (HDRP) from Unity 2018.3 and is verified for HDRP from Unity 2019.3. The Visual Effect Graph supports every platform that HDRP supports. For information on which platforms this includes, see HDRP's [system requirements](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html).

In the [Universal Render Pipeline](https://docs.unity3d.com/Manual/urp/urp-introduction.html) (URP) versions 2019.3. to 2021.1, the Visual Effect Graph supports a subset of platforms that URP supports, and only supports unlit particles. 

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
