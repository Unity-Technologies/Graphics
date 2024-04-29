# Streaming Adaptive Probe Volumes

You can enable Adaptive Probe Volume streaming to enable Adaptive Probe Volume lighting in very large worlds. Using streaming means you can bake Adaptive Probe Volume data larger than available CPU or GPU memory, and load it at runtime when it's needed. At runtime, as your camera moves, the Universal Render Pipeline (URP) loads only Adaptive Probe Volume data from cells within the camera's view frustum.

You can enable and disable streaming for different [URP quality levels](birp-onboarding/quality-settings-location.md).

## Enable streaming

To enable streaming, do the following:

1. From the main menu, select **Edit** > **Project Settings** > **Quality**.
2. Select a Quality Level.
3. Double-click the **Render Pipeline Asset** to open it in the Inspector.
4. Expand **Lighting**.

You can now enable two types of streaming:

- Enable **Enable Disk Streaming** to stream from disk to CPU memory.
- Enable **Enable GPU Streaming** to stream from CPU memory to GPU memory. You must enable **Enable Disk Streaming** first.

You can configure streaming settings in the same window. Refer to [URP Asset](universalrp-asset.md) for more information.

## Debug streaming

The smallest section URP loads and uses is a cell, which is the same size as the largest [brick](probevolumes-concept.md) in an Adaptive Probe Volume. You can influence the size of cells in an Adaptive Probe Volume by [adjusting the density of Light Probes](probevolumes-changedensity.md)

To view the cells in an Adaptive Probe Volume, or debug streaming, use the [Rendering Debugger](features/rendering-debugger.md).

![](Images/probe-volumes/probevolumes-debug-displayprobecells.PNG)<br/>
The Rendering Debugger with **Display Cells** enabled.

# Additional resources

* [Understanding Adaptive Probe Volumes](probevolumes-concept.md)
