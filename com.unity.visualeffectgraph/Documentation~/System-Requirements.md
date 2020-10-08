# Requirements and compatibility

This page contains information on system requirements and compatibility for the Visual Effect Graph package.

## Unity Editor compatibility

The following table shows the compatibility of the Visual Effect Graph versions with different Unity Editor versions.

| **Package version** | **Minimum Unity version** | **Maximum Unity version** |
| ------------------- | ------------------------- | ------------------------- |
| 10.x                | 2020.2                    | 2020.2                    |
| 8.x / 9.x-preview   | 2020.1                    | 2020.1                    |
| 7.x                 | 2019.3                    | 2019.4                    |
| 6.x                 | 2019.2                    | 2019.2                    |

## Render pipeline compatibility

The Visual Effect Graph varies in compatibility between the High Definition Render Pipeline (HDRP) and the Universal Render Pipeline (URP). This section describes the compatibility of Visual Effect Graph versions with different render pipelines.

| **Package version** | **HDRP**       | **URP**       |
| ------------------- | -------------- | ------------- |
| 10.x                | Out of preview | In preview    |
| 8.x / 9.x-preview   | Out of preview | In preview    |
| 7.x                 | Out of preview | In preview    |
| 6.x                 | In preview     | Not supported |

The Visual Effect Graph supports the [High Definition Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html) (HDRP) from Unity 2018.3 and is verified for HDRP from Unity 2019.3. The Visual Effect Graph supports every platform that HDRP supports. For information on which platforms this includes, see HDRP's [system requirements](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html). 

**Note**: When you download the HDRP package from the Package Manager, Unity automatically installs the Visual Effect Graph package.

The Visual Effect Graph supports the [Universal Render Pipeline](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) (URP) from Unity 2019.3. However, it is not yet out of preview for URP, which means it only supports a subset of platforms that URP supports. It also does not support every feature that it does with HDRP, and also only supports unlit particles.

## Unity Player system requirements

The Unity Player system requirements for the Visual Effect Graph depend on which render pipeline you use. 

- The Visual Effect Graph is out of preview for HDRP, which means it supports every platform that HDRP supports. For information on which platforms this includes, see HDRP's [system requirements](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/System-Requirements.html).
- The Visual Effect Graph is not out of preview for URP, which means it only supports a subset of platforms that URP supports.

For more information on general system requirements for the Unity Player, see [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).