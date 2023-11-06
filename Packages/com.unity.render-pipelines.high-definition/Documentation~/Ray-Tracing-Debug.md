# Debug ray-traced effects

The High Definition Render Pipeline (HDRP) includes the [Rendering Debugger window](use-the-rendering-debugger.md), which you can use to debug and understand ray-traced effect in HDRP. To debug raytraced effects:

1. Open the debug menu, select **Window > Analysis > Rendering Debugger**.
2. Select the **Lighting** panel.
3. Use the **Fullscreen Debug Mode** drop-down menu to select which ray tracing effect to debug.

![](Images/RayTracingLightCluster1.png)

**Light Cluster [Debug Mode](Ray-Tracing-Debug.md#debug-modes)**: The color shows the number of lights in each cell of the cluster.

![](Images/RayTracingDebugRTAS.png)

**Ray Tracing Acceleration Structure [Debug Mode](Ray-Tracing-Debug.md#debug-modes)**: This debug mode displays the GameObjects HDRP uses to compute specific ray traced effects.

## Debug modes

<table>
<thead>
<tr>
<th><strong>Fullscreen Debug Mode</strong></th>
<th colspan="2"><strong>Description</strong></th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Screen Space Ambient Occlusion</strong></td>
<td colspan="2">When <a href="Ray-Traced-Ambient-Occlusion.md">Ray-Traced Ambient Occlusion</a> is active, this displays the screen space buffer that holds the ambient occlusion.</td>
</tr>
<tr>
<td><strong>Screen Space Reflection</strong></td>
<td colspan="2">When <a href="Ray-Traced-Reflections.md">Ray-Traced Reflections</a> are active, this displays the ray-traced reflections.</td>
</tr>
<tr>
<td><strong>Transparent Screen Space Reflection</strong></td>
<td colspan="2">When <a href="Ray-Traced-Reflections.md">Ray-Traced Reflections</a> are active, this displays the ray-traced reflections on transparent objects.</td>
</tr>
<tr>
<td><strong>Contact Shadows</strong></td>
<td colspan="2">When <a href="Ray-Traced-Contact-Shadows.md">Ray-Traced Contact Shadows</a> are active, this displays the ray-traced contact shadows.</td>
</tr>
<tr>
<td><strong>Screen Space Shadows</strong></td>
<td colspan="2">When screen space shadows are active, this displays the set of screen space shadows. If you select this option, Unity exposes the <strong>Screen Space Shadow Index</strong> slider that allows you to change the currently active shadows. Area lights shadows take two channels.</td>
</tr>
<tr>
<td><strong>Screen Space Global Illumination</strong></td>
<td colspan="2">When <a href="Ray-Traced-Global-Illumination.md">Ray-Traced Global Illumination</a> is active, this displays a screen space buffer that holds the indirect diffuse lighting.</td>
</tr>
<tr>
<td><strong>Recursive Ray-Tracing</strong></td>
<td colspan="2">When <a href="Ray-Tracing-Recursive-Rendering.md">Recursive Ray Tracing</a> is active, this displays the pixels that have been evaluated using the effect.</td>
</tr>
<tr>
<td><strong>Ray-Traced Subsurface Scattering</strong></td>
<td colspan="2">When <a href="Ray-Traced-Subsurface-Scattering.md">Ray-Traced Subsurface Scattering</a> is active, this displays the subsurface lighting value for the pixels that have been evaluated using the technique.</td>
</tr>
<tr>
<td rowspan="2"><strong>Light Cluster</strong></td>
<td colspan="2">This displays the light cluster with the color that varies by the number of the lights in each cell. The red color indicates that the number of lights is over 12 or equal to the maximum number of lights per cell.</td>
</tr>
<tr>
<td><strong>Light Category</strong></td>
<td>Use the drop-down to visualize the number of the lights in the selected light catetgory.</td>
</tr>
<tr>
<td><strong>Ray Tracing Acceleration Structure</strong></td>
<td colspan="2">This mode displays the GameObjects included in the ray tracing acceleration structure for the following effects:<ul><li>Shadows<li>Ambient Occlusion<li>Global Illumination<li>Reflections<li>Recursive Rendering<li>Path Tracer</ul>HDRP only builds the acceleration structure when you activate the effect you select in this mode, otherwise the debug view is black.<br>This mode has the following visualization options:<ul><li>InstanceID: Assigns a color randomly based on the GameObject's InstanceID.<li>PrimitiveID: Assigns a color randomly based on the GameObject's PrimitiveID.</ul></td>
</tr>
</tbody>
</table>
