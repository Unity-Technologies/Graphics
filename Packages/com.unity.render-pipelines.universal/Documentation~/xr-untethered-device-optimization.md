---
uid: urp-xr-untethered-device-optimization
---
# Optimization techniques for untethered XR devices

This page describes the optimization techniques for URP projects that target untethered XR devices.

Most untethered XR devices use tile-based GPUs. The guidelines on this page help you use this hardware architecture more efficiently and avoid using rendering  techniques that are less efficient on those devices.

To learn more about how tiled-based GPUs work, refer to the additional resources section. 

## Use Vulkan API

Vulkan API is more stable and provides better performance compared to OpenGL ES API in URP projects targeting XR platforms.

Refer to [Graphics API support](https://docs.unity3d.com/6000.0/Documentation/Manual/GraphicsAPIs.html) for information on how to change the graphics API to Vulkan.

## Use OpenXR Plugin

Use the [OpenXR Plugin](https://docs.unity3d.com/Packages/com.unity.xr.openxr@latest?subfolder=/manual/index.html) in projects that target XR platforms.

Enable the following settings in your project:
* **Multi-view \ Single pass rendering**
* **Foveated rendering**

To configure the **Render Mode** to **Single Pass Instanced \ Multi-view**:

1. Open the **Project Settings** window.
2. Under **XR Plug-in Management**, open the **OpenXR** settings.
3. Under the **Android** tab, change the **Render Mode** to **Single Pass Instanced \ Multi-view**.

## Use render graph system

Starting with Unity 6 Preview, new URP projects use the render graph system.
Refer to [Benefits of the render graph system](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/render-graph-benefits.html) to understand the benefits of RenderGraph.

## Use Forward rendering

In URP, Deferred rendering generates several render targets for the G-buffer. Unity performs multiple [graphics memory loads](https://developer.qualcomm.com/software/snapdragon-profiler/app-notes/avoid-gmem-loads) to access those render targets, which is slow on tile-based GPUs.

Refer to [Deferred rendering implementation details](rendering/deferred-rendering-path.md#implementation-details) for more information on the implementation of the G-buffer in URP.

The **Rendering Path** settings is in the [**Rendering**](urp-universal-renderer.md#rendering) section of the [Universal Renderer Asset](urp-universal-renderer.md).

## Avoid post-processing

Avoid post-processing on untethered XR devices because of its performance impact.

URP renders post-processing in multiple render passes where the output of one pass is the input of the next one. On tile-based GPUs one of the most resource intensive tasks is performing a GMEM load. Post-processing passes often cause GMEM loads because they might load additional textures or copy the current screen color information to perform certain effects. In certain post-processing effects, for example in [bloom](post-processing-bloom.md), rendering a pixel requires sampling adjacent pixels. This can cause extra GMEM loads for accessing pixels outside a certain tile.

In URP, the post-processing pass executes a final blit even if there are no effects to execute. This requires another GMEM load because the blit operation copies the current texture in which Unity executes the post-processing pass to the final camera texture.

On XR platforms Unity performs such operations once per view which increases the performance impact.  

> [!NOTE]
> Some effects can cause motion sickness. Refer to section [Post-processing in URP for VR](integration-with-post-processing.md#post-processing-in-urp-for-vr) for a list of effects that can cause motion sickness.

To disable post-processing for a specific **Universal Renderer**:
1. Select a **Universal Renderer** asset.
2. Under **Post-processing**, ensure that the **Enabled** checkbox is cleared.

To disable post-processing for a camera:

1. Select a camera in the **Hierarchy** window.
2. In the **Inspector** window, expand the **Rendering** section.
3. Ensure that **Post Processing** is cleared.

## Avoid geometry shaders

Avoid using geometry shaders on platforms with tile-based GPUs. Some devices don't support geometry shaders. 

The generation of additional primitives and vertices breaks the tiled GPU flow because the division of primitives after the binning pass becomes invalid.

## Use MSAA for anti-aliasing

Tile-based GPUs can store more samples in the same tile. This makes Multi-sample Anti-aliasing (MSAA) efficient on mobile and untethered XR platforms. 2X MSAA value provides a good balance between visual quality and performance.

You can change the MSAA settings in the **Quality** section of the URP Asset.

For more information on MSAA, refer to [Anti-aliasing in URP](anti-aliasing.md).

## Disable depth priming

Disable depth priming on XR platforms. XR devices have two views, which increases the performance impact from performing the depth pre-pass. 

For untethered XR devices there are no benefits of performing the depth priming. You can obtain similar results using hardware optimization features, such as Low-Resolution-Z (LRZ) or Hidden Surface Removal (HSR).

For information on how to configure or disable depth priming, refer to the [Depth Priming Mode](urp-universal-renderer.md#rendering) property description.

## Disable Opaque texture and Depth texture properties

Disable the **Opaque Texture** and **Depth Texture** properties to improve performance. Enabling those options causes extra texture copy operations, which requires extra GMEM loads.

Refer to the [Rendering](universalrp-asset.md#rendering) section of the URP Asset description for more information on these options.

To identify if your current configuration is using intermediate textures, use the [Render Graph Viewer](render-graph-view.md) window.

## Disable SSAO

Screen-Space Ambient Occlusion (SSAO) might have poor performance on mobile and untethered XR devices. 

SSAO in URP requires the depth priming pass, two blur passes to reduce the noise, and an additional blit pass to clean the image. Such passes require several GMEM loads which have a high performance impact on tile-based GPUs.

To disable SSAO:

1. Select a Universal Renderer asset.
2. In the **Inspector** window, under the **Renderer Features** section, ensure that **Screen Space Ambient Occlusion** is disabled or absent.

## Disable HDR

HDR rendering has higher precision and improves graphics fidelity, but requires more bits per pixel to process. Disable HDR to reduce memory bandwidth and improve performance.

Most untethered XR devices don't support HDR rendering.

To disable HDR:
1. Select a URP Asset.
2. In the **Inspector** window, in the **Quality** section, clear the **HDR** property.

## Additional resources

- [Tile-Based Rendering](https://developer.arm.com/documentation/102662/0100/Overview) (Arm)
- [GPU Tile-Based Rendering](https://developer.qualcomm.com/sites/default/files/docs/adreno-gpu/snapdragon-game-toolkit/gdg/gpu/overview.html#tile-based-rendering) (Qualcomm Adreno)
- [Post-processing Effects on Mobile: Optimization and Alternatives](https://community.arm.com/arm-community-blogs/b/graphics-gaming-and-vr-blog/posts/post-processing-effects-on-mobile-optimization-and-alternatives) (Arm community)
- [Tiled Rendering](https://en.wikipedia.org/wiki/Tiled_rendering) (Wikipedia)
