# Display Adaptive Probe Volumes

You can use the Rendering Debugger to check how HDRP places Light Probes in an Adaptive Probe Volume, then use Adaptive Probe Volume settings to configure the layout.

## Display Adaptive Probe Volumes

To display Adaptive Probe Volumes, open the [Rendering Debugger](rendering-debugger-window-reference.md#ProbeVolume) and select the **Probe Volume** tab.

You can do the following:

- Enable **Probe Visualization** > **Display Probes** to display the locations of Light Probes and the lighting they store.
- Enable **Subdivision Visualization** > **Display Bricks** to display the outlines of groups of Light Probes ('bricks'). Refer to [Understanding Adaptive Probe Volumes](probevolumes-concept.md#how-probe-volumes-work) for more information on bricks.
- Enable **Subdivision Visualization** > **Display Cells** to display the outlines of cells, which are groups of bricks used for [streaming](probevolumes-streaming.md).
- Enable **Subdivision Visualization** > **Debug Probe Sampling** to display how neighboring Light Probes influence a chosen position. Select a surface to display the weights HDRP uses to sample nearby Light Probes.

If the Rendering Debugger displays invalid probes when you select **Display Probes**, refer to [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md).

![The Rendering Debugger with Display Probes enabled.](Images/probevolumes-debug-displayprobes.PNG)<br/>

The Rendering Debugger with **Display Probes** enabled.

![The Rendering Debugger with Display Bricks enabled.](Images/probevolumes-debug-displayprobebricks1.PNG)

The Rendering Debugger with **Display Bricks** enabled.

![The Rendering Debugger with Display Cells enabled.](Images/probevolumes-debug-displayprobecells.PNG)

The Rendering Debugger with **Display Cells** enabled.

![The Rendering Debugger with Debug Probe Sampling enabled](Images/APVsamplingDebug.png)

The Rendering Debugger with **Debug Probe Sampling** enabled

Refer to [Rendering Debugger window reference](rendering-debugger-window-reference.md#ProbeVolume) for more information.

## Additional resources

* [Configure the size and density of an Adaptive Probe Volume](probevolumes-changedensity.md)
* [Adaptive Probe Volumes panel reference](probevolumes-lighting-panel-reference.md)
* [Probe Volumes Options Override reference](probevolumes-options-override-reference.md)
* [Probe Adjustment Volume component reference](probevolumes-adjustment-volume-component-reference.md)
