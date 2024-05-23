# Requirements and compatibility

This page contains information on system requirements and compatibility of this package.

## Unity Editor compatibility

The following table shows the compatibility of URP package versions with different Unity Editor versions.

| Package version | Minimum Unity version | Maximum Unity version |
| --------------- | --------------------- | --------------------- |
| 16.0.x          | 2023.2                | 2023.x                |
| 15.0.x          | 2023.1                | 2023.1                |
| 14.0.x          | 2022.2                | 2022.x                |
| 13.x.x          | 2022.1                | 2022.1                |
| 12.0.x          | 2021.2                | 2021.3                |
| 11.0.0          | 2021.1                | 2021.1                |
| 10.x            | 2020.2                | 2020.3                |
| 9.x-preview     | 2020.1                | 2020.2                |
| 8.x             | 2020.1                | 2020.1                |
| 7.x             | 2019.3                | 2019.4                |

Since the release of Unity 2021.1, graphics packages are [core Unity packages](https://docs.unity3d.com/2021.2/Documentation/Manual/pack-core.html).

For each release of Unity (alpha, beta, patch release), the main Unity installer contains the up-to-date versions of the following graphics packages: SRP Core, URP, HDRP, Shader Graph, VFX Graph. Since the release of Unity 2021.1, the Package Manager shows only the major revisions of the graphics packages (version 11.0.0 for all Unity 2021.1.x releases, version 12.0.0 for all Unity 2021.2.x releases).

You can install a different version of a graphics package from disk using the Package Manager, or by modifying the `manifest.json` file.

## Render pipeline compatibility

Projects made using URP are not compatible with the High Definition Render Pipeline (HDRP) or the Built-in Render Pipeline. Before you start development, you must decide which render pipeline to use in your Project. For information on choosing a render pipeline, refer to the [Render Pipelines](https://docs.unity3d.com/2019.3/Documentation/Manual/render-pipelines.html) section of the Unity Manual.

## Unity Player system requirements

This package does not add any extra platform-specific requirements. General system requirements for the Unity Player apply. For more information on Unity system requirements, refer to [System requirements for Unity](https://docs.unity3d.com/Manual/system-requirements.html).
