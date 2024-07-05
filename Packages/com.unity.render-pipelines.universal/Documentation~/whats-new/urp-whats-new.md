---
uid: urp-urp-whats-new
---
# What's new in URP 17 (Unity 6 Preview)

This section contains information about new features, improvements, and issues fixed in URP 17.

For a complete list of changes made in URP 17, refer to the [Changelog](xref:urp-changelog).

## Features

This section contains the overview of the new features in this release.

### Render graph system

In this release, Unity introduces the [render graph](../render-graph.md) system. The render graph system is a framework built on top of the Scriptable Render Pipeline (SRP) API. This system improves the way you customize and maintain the render pipeline.

The render graph system reduces the amount of memory URP uses and makes memory management more efficient. The render graph system only allocates resources the frame actually uses, and you no longer need to write complicated logic to handle resource allocation and account for rare worst-case scenarios. The render graph system also generates correct synchronization points between the compute and graphics queues, which reduces frame time.

The [Render Graph Viewer](../render-graph-viewer-reference.md) lets you visualize how render passes use frame resources, and debug the rendering process.

For more information about the render graph system, refer to the [render graph](../render-graph.md) documentation.

### Alpha Processing setting in post-processing

URP 17 adds an **Alpha Processing** setting (**URP Asset** > **Post-processing** > **Alpha Processing**). If you enable this setting, URP renders the post-processing output into a render texture with an alpha channel. In previous versions, URP discarded the alpha channel by replacing alpha values with 1.

The render target requires a format with the alpha channel. The camera color buffer format must be RGBA8 for SDR (HDR off) and RGBA16F for HDR (64-bits). You can configure the format using the settings in **URP Asset** > **Quality**.

Example use cases for this feature:

* Render in-game UI, such as a head-up display. You can render multiple render textures with different post-processing configurations and compose the final output using the alpha channel.

* Render a character customization screen, where Unity renders a background interface and a 3D character with different post-processing effects and blends them using the alpha channel.

* XR applications that need to support video pass-through.

### Reduce rendering work on the CPU

URP 17 contains new features that let you speed up the rendering process by moving certain tasks to the GPU and reducing the workload on the CPU.

#### GPU Resident Drawer

URP 17 includes a new rendering system called the **GPU Resident Drawer**.

This system automatically uses the [BatchRendererGroup API](https://docs.unity3d.com/Manual/batch-renderer-group.html) to draw GameObjects with GPU instancing, which reduces the number of draw calls and frees CPU processing time.

For more information on the GPU Resident Drawer, refer to the section [Reduce rendering work on the CPU](../reduce-rendering-work-on-cpu.md).

#### GPU occlusion culling

When using GPU occlusion culling, Unity uses the GPU instead of the CPU to exclude objects from rendering when they're occluded behind other objects. Unity uses this information to speed up rendering in scenes that have a lot of occlusion.

For more information on GPU occlusion culling, refer to the section [Reduce rendering work on the CPU](../gpu-culling.md).

### Foveated rendering in the Forward+ Rendering Path

The Forward+ Rendering Path now supports foveated rendering.

### Camera history API

This release contains the [camera history API](xref:UnityEngine.Rendering.Universal.UniversalCameraHistory) which lets you access per-camera history textures and use them in custom render passes. History textures are the color and depth textures that Unity rendered for each camera in previous frames.

You can use history textures for rendering algorithms that use frame data from one or multiple previous frames.

URP implements per-camera color and depth texture history and history access for custom render passes.

### Mipmap Streaming section in the Rendering Debugger

The [Rendering Debugger](../features/rendering-debugger.md) now contains a **Mipmap Streaming** section. This section lets you inspect the texture streaming activity.


### Spatial Temporal Post-Processing (STP)

Spatial Temporal Post-Processing (STP) optimizes GPU performance and enhances visual quality by upscaling frames Unity renders at a lower resolution. STP works on desktop platforms, consoles, and mobile devices that support compute shaders.

To enable STP, in the active **URP Asset**, select **Quality** > **Upscaling Filter** > **Spatial Temporal Post-Processing (STP)**. 

## Improvements

This section contains the overview of the major improvements in this release.

### Adaptive Probe Volumes (APV)

This release contains the following improvements to [Adaptive Probe Volumes](../probevolumes.md):

* [APV Lighting Scenario Blending](../probevolumes-bakedifferentlightingsetups.md).

* [APV sky occlusion support](../probevolumes-skyocclusion.md).

* [APV disk streaming](../probevolumes-streaming.md).

### Volume framework enhancements

This release comes with CPU performance optimizations of the volume framework on all platforms, especially mobile platforms. You can now set global volume default values and override them in quality settings.

### 8192 shadow texture resolution

The **Shadow Resolution** property now contains the `8192` option for the Main Light and Additional Lights.

### Use the URP Config package to change render pipeline settings

The [URP Config package](../URP-Config-Package.md) lets you change certain render pipeline settings that are not available in the Editor interface.

For example, you can [change the maximum number of visible lights](../rendering/forward-plus-rendering-path.md#change-the-maximum-number-of-visible-lights).

## Issues resolved

For a complete list of issues resolved in URP 17, refer to the [Changelog](xref:urp-changelog).

## Known issues

For information on the known issues in URP 17, refer to the section [Known issues](../known-issues.md).
