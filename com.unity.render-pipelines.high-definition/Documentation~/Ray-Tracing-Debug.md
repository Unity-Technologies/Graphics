# Debugging ray-traced effects

The High Definition Render Pipeline (HDRP) includes the [Render Pipeline Debug window](Render-Pipeline-Debug-Window.html), which you can use to debug and understand ray-traced effect in HDRP. To debug raytraced effects:

1. Open the debug menu, select **Window > Render Pipeline > Render Pipeline Debug**.
2. Select the **Lighting** panel.
3. Use the **Fullscreen Debug Mode** drop-down menu to select which ray tracing effect to debug.

![](Images/RayTracingLightCluster1.png)

**Light Cluster [Debug Mode](Ray-Tracing-Debug.html)**: The red regions show where the number of lights per cell is equal to the maximum number of lights per cell.
## Debug modes

| **Fullscreen Debug Mode**   | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **SSAO**                    | When [Ray-Traced Ambient Occlusion](Ray-Traced-Ambient-Occlusion.html) is active, this displays the screen space buffer that holds the ambient occlusion. |
| **Screen Space Reflection** | When [Ray-Traced Reflections](Ray-Traced-Reflections.html) are active, this displays the ray-traced reflections. |
| **Contact Shadows** 		  | When [Ray-Traced Contact Shadows](Ray-Traced-Contact-Shadows.html) are active, this displays the ray-traced contact shadows. |
| **Screen Space Shadows**    | When screen space shadows are active, this displays the set of screen space shadows. If you select this option, Unity exposes the **Screen Space Shadow Index** slider that allows you to change the currently active shadows. Area lights shadows take two channels. |
| **Indirect Diffuse**        | When [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.html) is active, this displays a screen space buffer that holds the indirect diffuse lighting. |
| **Light Cluster**           | This displays the cluster using a debug view that allows you to see the regions of the Scene where light density is higher, and potentially more resource-intensive to evaluate. |