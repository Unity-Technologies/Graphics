# Ambient Occlusion

The Ambient Occlusion post-processing effect darkens creases, holes, intersections and surfaces that are close to each other. In the real world, such areas tend to block out or occlude ambient light, so they appear darker.

URP implements the real-time Screen Space Ambient Occlusion (SSAO) post-processing effect as a Renderer Feature.

> **NOTE**: The SSAO effect is a Renderer Feature and works independently from the post-processing effects in URP. This effect does not depend on or interact with Volumes.

The following images show a scene with the Ambient Occlusion effect turned off and on.

![Scene with Ambient Occlusion effect turned off.](Images/post-proc/ssao/scene-ssao-off.png)

![Scene with Ambient Occlusion effect turned on.](Images/post-proc/ssao/scene-ssao-on.png)

## Adding the SSAO effect to a Renderer

URP implements the Ambient Occlusion post-processing effect as a Renderer Feature.

To use the Ambient Occlusion post-processing effect in your project:

1. In the __Project__ window, select the Renderer that the URP asset is using.

    ![Select the Renderer.](Images/post-proc/ssao/ssao-select-renderer.png)

    The Inspector window shows the the Renderer properties.

    ![Inspector window shows the Renderer properties.](Images\post-proc\ssao\ssao-inspector-no-rend-features.png)

2. In the Inspector window, select __Add Renderer Feature__. In the list, select __Screen Space Ambient Occlusion__.

    ![Select __Add Renderer Feature__, then select __Screen Space Ambient Occlusion__](Images/post-proc/ssao/ssao-select-renderer-feature.png)

    Unity adds the SSAO Renderer Feature to the Renderer.

    ![SSAO Renderer Feature.](Images/post-proc/ssao/ssao-renderer-feature-created.png)

Now Cameras that use the Renderer with the SSAO Renderer Feature have the Ambient Occlusion effect.

## Properties


