# Streaming Adaptive Probe Volumes

You can enable Adaptive Probe Volume streaming to enable Adaptive Probe Volume lighting in very large worlds. Using streaming means you can bake Adaptive Probe Volume data larger than available CPU or GPU memory, and load it at runtime when it's needed. At runtime, as your camera moves, the High Definition Render Pipeline (HDRP) loads only Adaptive Probe Volume data from cells within the camera's view frustum.

You can enable and disable streaming for different [HDRP quality levels](quality-settings.md).

## Enable streaming

To enable streaming, do the following:

1. From the main menu, select **Edit** > **Project Settings** > **Quality** > **HDRP**.
2. Select a Quality Level.
3. Expand **Lighting** > **Light Probe Lighting**.

You can now enable two types of streaming:

- Enable **Enable Disk Streaming** to stream from disk to CPU memory.
- Enable **Enable GPU Streaming** to stream from CPU memory to GPU memory. You must enable **Enable Disk Streaming** first.

You can configure streaming settings in the same window. Refer to [HDRP Asset](HDRP-Asset.md#Lighting) for more information.

## Compatibility with Asset Bundles

The underlying system used to support streaming causes limitation with regards to Asset Bundles and Addressables. Please see [this section for more information](probevolumes-inspector-reference.md#probe-volume-limitations-with-asset-bundles-and-addressables)

## Debug streaming

The smallest section HDRP loads and uses is a cell, which is the same size as the largest [brick](probevolumes-concept.md) in an Adaptive Probe Volume. You can influence the size of cells in an Adaptive Probe Volume by [adjusting the density of Light Probes](probevolumes-changedensity.md)

To view the cells in an Adaptive Probe Volume, or debug streaming, use the [Rendering Debugger](rendering-debugger-window-reference.md#probe-volume-panel).

![](Images/probevolumes-debug-displayprobecells.PNG)<br/>
The Rendering Debugger with **Display Cells** enabled.

# Additional resources

* [Understanding Adaptive Probe Volumes](probevolumes-concept.md)
* [Frame Settings](frame-settings-reference.md)
