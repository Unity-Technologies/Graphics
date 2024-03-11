# Streaming Probe Volumes

You can enable Probe Volume streaming to enable Probe Volume lighting in very large worlds. Using streaming means you can bake Probe Volume data larger than available CPU or GPU memory, and load it at runtime when it's needed. At runtime, as your camera moves, the High Definition Render Pipeline (HDRP) loads only Probe Volume data from cells within the camera's view frustum.

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

## Debug streaming

The smallest section HDRP loads and uses is a cell, which is the same size as the largest [brick](probevolumes-concept.md) in a Probe Volume. You can influence the size of cells in a Probe Volume by [adjusting the density of Light Probes](probevolumes-changedensity.md)

To view the cells in a Probe Volume, or debug streaming, use the [Rendering Debugger](rendering-debugger-window-reference.md#probe-volume-panel).

![](Images/probevolumes-debug-displayprobecells.PNG)<br/>
The Rendering Debugger with **Display Cells** enabled.

# Additional resources

* [Understanding probe volumes](probevolumes-concept.md)
* [Frame Settings](frame-settings-reference.md)
