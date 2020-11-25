# What's new in version 11

This page contains an overview of new features, improvements, and issues resolved in version 11 of the High Definition Render Pipeline (HDRP).

## Features

The following is a list of features Unity added to version 11.0 of the High Definition Render Pipeline. Each entry includes a summary of the feature and a link to any relevant documentation.

### Mixed Cached Shadow Maps

From HDRP 11.0 It is possible to cache only a portion of non-directional shadow maps. With this setup HDRP will render static shadow casters into the shadow map following the Light's Update Mode, but it renders dynamic shadow casters into their respective shadow maps each frame. 

This can result in significant performance improvements for projects that have lights that don't move or move not often, but need dynamic shadows being cast from them. 

For more information about the future, see the [Shadow](Shadows-in-HDRP.md) section of the documentation.

### Cubemap fields in Volume Components

Cubemap fields now accept both [RenderTexture](https://docs.unity3d.com/Manual/class-RenderTexture.html) and [CustomRenderTexture](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html) of Cubemap dimension. This change affects the `HDRI Sky` and `Physically Based Sky` components and will allow you to animate both skies.

For more information, see the [HDRI Sky](Override-HDRI-Sky.md) and [Physically Based Sky](Override-Physically-Based-Sky) sections of the documentation.

## Issues resolved

For information on issues resolved in version 11 of HDRP, see the [changelog](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@10.0/changelog/CHANGELOG.html).
