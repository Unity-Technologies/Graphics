# Spatial-Temporal Post-processing debug views

There are six debug views for Spatial-Temporal Post-processing (STP). To access them, open the Rendering Debugger window  and navigate to **Rendering** > **Fullscreen Debug Mode** and select **STP** from the dropdown. Unity shows the **STP Debug Mode** property where you can select one of the views.

For more information on how to access the Rendering Debugger window, refer to [How to access the Rendering Debugger](../features/rendering-debugger.md).

## Debug views

| **Clipped Input Color** | Show the HDR input color clipped between 0 and 1. |
| **Log Input Depth** | Show the input depth in logarithmic scale. |
| **Reversible Tonemapped Input Color** | Show the input color mapped to a 0-1 range with a reversible tonemapper. |
| **Shaped Absolute Input Motion** | Visualize the input motion vectors. |
| **Motion Reprojection** | Visualize the reprojected color difference across several frames. |
| **Sensitivity** | Visualize the pixel sensitivities. Green areas show where STP can't predict motion behavior. These areas are likely to render with reduced visual quality. STP struggles to predict motion in areas when occluded objects first become visible or when there is fast movement. Incorrect object motion vectors can also cause issues with motion prediction. Red areas highlight pixels that are excluded from TAA, so STP intentionally does not predict their motion. This helps avoid unnecessary blurring and ghosting, especially when rendering transparent objects. |
