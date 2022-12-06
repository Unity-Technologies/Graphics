# Debugging ray-traced effects

The High Definition Render Pipeline (HDRP) includes the [Rendering Debugger window](Render-Pipeline-Debug-Window.md), which you can use to debug and understand ray-traced effect in HDRP. To debug raytraced effects:

1. Open the debug menu, select **Window > Analysis > Rendering Debugger**.
2. Select the **Lighting** panel.
3. Use the **Fullscreen Debug Mode** drop-down menu to select which ray tracing effect to debug.

![](Images/RayTracingLightCluster1.png)

**Light Cluster [Debug Mode](Ray-Tracing-Debug.md)**: The red regions show where the number of lights per cell is equal to the maximum number of lights per cell.

![](Images/RayTracingDebugRTAS.png)

**Ray Tracing Acceleration Structure [Debug Mode](Ray-Tracing-Debug.md)**: This debug mode displays the GameObjects HDRP uses to compute specific ray traced effects.

## Debug modes

| **Fullscreen Debug Mode**   | **Description**                                              |
| --------------------------- | ------------------------------------------------------------ |
| **Screen Space Ambient Occlusion** | When [Ray-Traced Ambient Occlusion](Ray-Traced-Ambient-Occlusion.md) is active, this displays the screen space buffer that holds the ambient occlusion. |
| **Screen Space Reflection** | When [Ray-Traced Reflections](Ray-Traced-Reflections.md) are active, this displays the ray-traced reflections. |
| **Transparent Screen Space Reflection** | When [Ray-Traced Reflections](Ray-Traced-Reflections.md) are active, this displays the ray-traced reflections on transparent objects. |
| **Contact Shadows**         | When [Ray-Traced Contact Shadows](Ray-Traced-Contact-Shadows.md) are active, this displays the ray-traced contact shadows. |
| **Screen Space Shadows**    | When screen space shadows are active, this displays the set of screen space shadows. If you select this option, Unity exposes the **Screen Space Shadow Index** slider that allows you to change the currently active shadows. Area lights shadows take two channels. |
| **Screen Space Global Illumination**  | When [Ray-Traced Global Illumination](Ray-Traced-Global-Illumination.md) is active, this displays a screen space buffer that holds the indirect diffuse lighting. |
| **Recursive Ray-Tracing**             | When [Recursive Ray Tracing](Ray-Tracing-Recursive-Rendering.md) is active, this displays the pixels that have been evaluated using the effect. |
| **Ray-Traced Subsurface Scattering**  | When [Ray-Traced Subsurface Scattering](Ray-Traced-Subsurface-Scattering.md) is active, this displays the subsurface lighting value for the pixels that have been evaluated using the technique. |
| **Light Cluster**           | This displays the cluster using a debug view that allows you to see the regions of the Scene where light density is higher, and potentially more resource-intensive to evaluate. |
| **Ray Tracing Acceleration Structure**           | This mode displays the GameObjects included in the ray tracing acceleration structure for the following effects:<br>• Shadows<br>• Ambient Occlusion<br>• Global Illumination<br>• Reflections<br>• Recursive Rendering<br>• Path Tracer<br><br>HDRP only builds the acceleration structure when you activate the effect you select in this mode, otherwise the debug view is black.<br>This mode has the following visualization options:<br>• InstanceID: Assigns a color randomly based on the GameObject's InstanceID.<br>• PrimitiveID: Assigns a color randomly based on the GameObject's PrimitiveID.|
