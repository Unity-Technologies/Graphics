# Scalability Manual

Algorithm can be more or less resource consuming and this resource consumption is driven by parameters.
Also, the HDRP allocates upfront resources to for better memory management, and this upfront allocation is also driven by parameters.

The [`HDRenderPipelineAsset`] is the place where those settings are defined. These settings impacts performance of the GPU and the graphics memory, but it also drives the capabilities enabled of the HDRP.

Scalability means that you can provide a set of settings that fits a targetted hardware to keep a proper balance between performance and rendering quality.

## Using the HDRenderPipelineAsset and the quality settings in Unity

### The _default_ [`HDRenderPipelineAsset`]

In order to use HDRP, you needs to have an [`HDRenderPipelineAsset`] assigned as the graphics' settings render pipeline. (In Unity, this is located at 'Project Settings > Graphics > Render Pipeline').

This [`HDRenderPipelineAsset`] is called the _default_ hdrp asset. This asset will contains all default values for the hdrp's settings.

### Overriding settings for a quality level

To overrides the settings for a specific hardware, you will need to create an additional [`HDRenderPipelineAsset`] with those override.

Usually, we define a settings for an hardware and a tier. For instance, we can have an asset for:
- PS4
- PS4 Pro
- PC Low
- PC Medium
- PC High

![Quality Settings Panel](images/QualitySettingsPanel.png)
_A Quality Settings Panel with several quality level with an associated HDRP Asset_

Once you have your assets, create your desired quality levels in the Quality Settings Panel ('Project Settings > Quality').
When you select a quality level, you can assign an [`HDRenderPipelineAsset`] for this level.

The asset used in the current quality level is called the _current_ asset.

_Note: You can use the same [`HDRenderPipelineAsset`] for multiple quality levels._
_Note: If the render pieline asset field is empty, then the default asset will be used as the current asset._

### Editing the values of quality levels

To edit the values, you can use the HDRP's quality settings panel ('Project Settings > Quality > HDRP').

You will find on top all assigned [`HDRenderPipelineAsset`]s, including the default one.
To edit the value of one asset, click on it and the inspector will appear.

![HDRP Quality Settings Panel](images/HDRPQualitySettingsPanel.png)
_The HDRP quality settings panel let you edit values for specific HDRP assets_

## Using the current quality settings parameters

The settings defined in both the default HDRP asset and the one in the quality settings are used during the rendering.

### Predefined values from the current quality settings

In a single rendering, some elements can required more or less resources.
For instance, a spotlight in the foreground may have a greater shadow resolution than a background light.

For this kind of settings you can have two options: either use a custom value that will be always used, or use a predefined value in the current HDRP asset.

If we consider the above example, the shadow resolution of a light can be either:
- a _custom_ value edited in the inspector
- one of the predefined value in the current quality asset (one of _low_, _medium_, _high_, _ultra_)

![Shadow Resolution Scalability](images/ShadowResolutionScalability.png)
_Here the shadow resolution of the directional light uses the **medium** predefined value in the quality settings._

### Material Quality Node in Shader Graph

The Material Quality Node can be used in Shader Graphs to decide which code to execute for a specific quality.

The material quality level used is defined in the [`HDRenderPipelineAsset`] in the 'Material' section.

It is important to node that for a given quality level, all materials will have the same Material Quality Level applied. If you need to have different shader quality in a single rendering (like a Shader LOD system), we suggest you to author a dedicated shader and use different shader with their appropriate complexity for this rendering.

### Raytracing Node in Shader Graph

For Shader Graphs that use raytracing, you can define the Raytracing Node to provide a fast and a slow implementation.

The fast implementation will be used to increase the performance of raytraced rendering features where accuracy is less important.

The raytracing node will be activated depending on the settings defined in the current [`HDRenderPipelineAsset`].

[`HDRenderPipelineAsset`]: ../api/UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset.html
[Material Quality Node]: Material-Quality-Node
[Raytracing Node]: Raytracing-Node