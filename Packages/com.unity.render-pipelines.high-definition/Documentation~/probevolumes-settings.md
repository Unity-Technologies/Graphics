# Probe Volume settings and properties

This page explains the settings you can use to configure Probe Volumes.

## Probe Volume Settings

To open **Probe Volume Settings**, open the **Window** menu, select **Rendering**, then select **Probe Volume Settings**.

Probe Volume Settings affect all the Probe Volumes in a [Baking Set](probevolumes-concept.md#baking-sets).

See [Use Probe Volumes](probevolumes-use.md).

<table>
  <tr>
    <td><a name="bakingsetlists"></a><b>Property</b></td>
    <td colspan="4"><b>Description</b></td>
  </tr>
  <tr>
    <td><b>Baking Sets</b></td>
    <td colspan="4">Lists the Baking Sets in your Project. Use <b>+</b> and <b>-</b> to add or remove a Baking Set.</td>
  </tr>
  <tr>
    <td><b>Scenes</b></td>
  <td colspan="4">Lists the Scenes in the Baking Set selected.<br/>Each Scene includes a probes icon if the Scene has one or more Probe Volumes.<br/>Use <b>+</b> and <b>-</b> to add or remove a Scene from the active Baking Set.<br/>Use the three-line icon to the left of each Scene to drag the Scene up or down in the list.</td>
  </tr>
  <tr>
    <td><b>Lighting Scenarios</b></td>
 <td colspan="4">Lists the Lighting Scenarios in the Baking Set selected.<br/><b>Active Scenario</b> appears if this is the active scenario HDRP bakes lighting data into. <b>Not baked</b> appears if you haven't baked lighting data into the Lighting Scenario.<br/>To rename a Scenario, double-click its name.
 </td>
  </tr>
  <tr>
  </table>

  <br/>


<table>
    <tr>
    <td colspan="3"><b>Property</b></td>
    <td colspan="1"><b>Description</b></td>
  </tr>
    <td rowspan="4"><b>Probe Placement</b><br></td>
  </tr>
  <tr>
    <td colspan="2"><b>Freeze Placement</b></td>
    <td>Disables probe position recalculation after the first bake. Makes it possible to bake Scenarios so they're compatible for blending purposes.</td>
  </tr>
  <tr>
    <td colspan="2"><b>Max Distance Between Probes</b></td>
    <td><a name="maxprobespacing"></a>The maximum distance between probes, in meters. See <a href="probevolumes-showandadjust.md">Display and adjust Probe Volumes</a> for additional information.</td>
  </tr>
  <tr>
    <td colspan="2"><a name="minprobespacing"></a><b>Min Distance Between Probes</b></td>
    <td>The minimum distance between probes, in meters. See <a href="probevolumes-showandadjust.md">Display and adjust Probe Volumes</a> for additional information.</td>
  </tr>
  <tr>
 <td rowspan="3"><b>Renderer Filter Settings</b><a name="rendererfilter"></a></td>
  </tr>
  <tr>
    <td colspan="2"><b>Layer Mask</b></td>
     <td>Specify the <a href="https://docs.unity3d.com/Manual/Layers.html">Layers</a> HDRP considers when it generates probe positions. Select a Layer to enable or disable it.
     </td>
  </tr>
  <tr>
    <td colspan="2"><b>Min Renderer Volume Size</b></td>
     <td>The smallest <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a> size HDRP considers when it places probes.</td>
  </tr>
  <tr>
    <td rowspan="6"><b>Dilation Settings</b></td>
  </tr>
  <tr>
    <td colspan="2"><b>Enable Dilation</b><a name="dilationsettings"></a></td>
       <td>Enables HDRP giving invalid probes data from valid probes nearby. Enabled by default. See <a href="probevolumes-fixissues.md">Fix issues with Probe Volumes</a>.</td>
  </tr>
  <tr>
    <td colspan="2"><b>Dilation Distance</b></td>
       <td>Determines how far from an invalid probe HDRP searches for valid neighbors. Higher values include more distant probes. Probes that are too far away may be in different lighting conditions than the invalid probe.</td>
  </tr>
  <tr>
    <td colspan="2"><a name="dilationvaliditythreshold"></a><b>Dilation Validity Threshold</b></td>
       <td>The ratio of backfaces a probe samples before HDRP considers it invalid. Higher values mean HDRP is more likely to mark a probe invalid.</td>
  </tr>
  <tr>
    <td colspan="2"><b>Dilation Iteration Count</b></td>
       <td>The number of times Unity repeats the dilation calculation. This increases spread of dilation effect, but requires additional processing power.</td>
  </tr>
  <tr>
    <td colspan="2"><b>Squared Distance Weighting</b></td>
       <td>Enables weighing the contribution of neighbouring probes by squared distance, rather than linear distance.</td>
  </tr>
  <tr>
    <td rowspan="8"><b>Virtual Offset Settings</b><br></td>
  </tr>
  <tr>
    <td colspan="2"><b>Use Virtual Offset <a name="offset"></a></td>
    <td>Enables HDRP moving the capture point of invalid Light Probes so they're valid again. See <a href="probevolumes-fixissues.md">Fix issues with Probe Volumes</a>.</td>
  </tr>
  <tr>
    <td rowspan="6"><b>Advanced</b></td>
  </tr>
  <tr>
    <td><b>Search multiplier</b></td>
      <td>Determines the length of the sampling ray HDRP uses to search for valid probe positions. High values may cause unwanted results, such as probe capture points pushing through neighboring geometry.</td>
  </tr>
  <tr>
    <td><b>Bias Out Geometry</b></td>
      <td>Determines how far HDRP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry.</td>
  </tr>
  <tr>
    <td><b>Ray origin bias</b></td>
      <td>Distance between a probe's center and the point HDRP uses to determine the origin of that probe's sampling ray. High values may cause unwanted results, such as sampling from an area of the scene with dissimilar lighting.</td>
  </tr>
  <tr>
    <td><b>Max hits per ray</b></td>
      <td>How many times a sampling ray hits geometry before HDRP determines the position of the probe where that ray originated.</td>
  </tr>
  <tr>
    <td><b>Collision mask</b></td>
      <td>Layers HDRP includes in collision calculations for [Virtual Offset](probevolumes-fixissues.md).</td>
  </tr>
  <tr>
    <td rowspan="2"><b>Stats</b></td>
  </tr>
  <tr>
    <td colspan="4">
    <ul>
    <li>Active Scenario Size on Disk</li>
    <li>Baking Set Total Size on Disk</li></ul></td>
  </tr>
</table>

<br/>

<table>

  <tr>
    <td><b>Control</b></td>
    <td colspan="4"><b>Description</b></td>
  </tr>
  <tr>
   <td><b>Load All Scenes in Set</b></td>
    <td colspan="4">Load all Scenes in this <a href="probevolumes-concept.md#baking-sets">Baking Set</a>.</td>
  </tr>
  <tr>
    <td><b>Clear Loaded Scenes Data</b></td>
    <td colspan="4">Clear baked lighting data for all loaded Scenes.</td>
  </tr>
  <tr>
    <td><b>Generate Lighting</b></td>
    <td colspan="4">
    Bakes lighting for the active scene. For additional options, use the dropdown arrow on this button.<br/>
    These controls bake all Scene lighting, not just Probe Volumes. If you bake individual Scenes separately, then together, the new shared baked data overwrites the unique data in each of the individual Scenes.
    <ul><li><b>Bake the set</b>: Bakes lighting for all Scenes in this Baking Set.</li>
    <li><b>Bake loaded Scenes</b>: Bakes lighting for all Scenes loaded in the <b>Scene Hierarchy</b> that are part of the selected Baking Set.</li>
    <li><b>Bake active Scene</b>: Bakes the Scene selected in the <b>Scene Hierarchy</b>.</li>
    </ul>
    </td>
  </tr>
</tbody>
</table>

## Probe Volume properties
<a name="pv-inspector"></a>

Select a Probe Volume and open the Inspector to view its properties.

| **Property**                        | **Description** |
|---------------------------------|-------------|
| **Global**                          | Enable to fit this Probe Volume to all Renderers in the Project that [contribute Global Illumination](https://docs.unity3d.com/Manual/class-MeshRenderer.html). The list of Renderers updates every time you save or bake a Scene. Disable to resize this Probe Volume manually or select a different scope option, such as **Fit to Scene**.|
| **Size** | The size of the Probe Volume. |
| **Fill Empty Spaces**               | Enable to make Unity fill the empty space between Renderers with bricks at the lowest density.  |
| **Subdivision Override** |  |
| **Distance Between Probes**         | Set the maximum and minimum distance between probes. See [Display and adjust Probe Volumes](probevolumes-showandadjust.md). |
| **Geometry Settings**               | If this Probe Volume isn't **Global**, use these settings to set its size.           |

### Geometry Settings

<table>
<tr><td><b>Override Renderer Filters</b></td><td colspan="2">Enable to override the <a href="#rendererfilter">Renderer Filter configuration</a> of the Baking Set to which this Scene belongs.   </td><tr>
<tr><td>&nbsp;</td><td><b>Layer Mask</b></td><td>Layers HDRP considers when it generates probe positions.</td></tr>
<tr><td>&nbsp;</td><td><b>Min Volume Size</b> </td><td>The minimum bounding box volume of Renderers Unity considers when it generates probe positions.</td></tr>
<tr><td colspan="2"><b>Fit to all Scenes</b></td><td>Fits this Probe Volume to all loaded Scenes.</td></tr>
<tr><td colspan="2"><b>Fit to Scene</b></td><td>Fits this Probe Volume to the active Scene.</td></tr>
<tr><td colspan="2"><b>Fit to Selection</b> </td><td>Fits this Probe Volume to selected Renderers. <a href="https://docs.unity3d.com/Manual/InspectorOptions.html">Lock the Inspector</a> to make additional selections. </td></tr>

</table>

## Probe Volume Touchup Component
<a name="pv-touchup"></a>

Select a [Probe Volume Touchup Component](probevolumes-fixissues.md#add-a-probe-volume-touchup-component) and open the Inspector to view its properties.

| **Property**                                     | **Description** |
|----------------------------------------------|-------------|
| **Size**                           |  The size of the Touchup Volume. |
| **Invalidate Probes**                            | Enables making all the Light Probes in this volume invalid.          |
| **Override Dilation Validity Threshold**         | Enables overriding **Dilation Validity Threshold** in the **Probe Volume Settings**. |
| **Dilation Validity Threshold**  | The value that will override the **Dilation Validity Threshold** value in the **Probe Volume Settings**.          |

## Probe Volumes Options Override
<a name="pv-voloverride"></a>

To add a Probe Volume Options Override, do the following:

1. Add a [Volume](Volumes.html) to your Scene and make sure its area overlaps the position of the camera.
2. Select **Add Override**, then select **Lighting** > **Probe Volume Options**.

| **Property**                           | **Description** |
|------------------------------------|-------------|
| **Normal Bias**   | Enable to move the position that object pixels use to sample the Light Probes, along the pixel's surface normal. The value is in meters. |
| **View Bias**  | Enable to move the position that object pixels use to sample the Light Probes, towards the camera. The results of **View Bias** vary depending on the camera position. The value is in meters. |
| **Scale Bias with Min Probe Distance** | Scale the **Normal Bias** or **View Bias** so it's proportional to the spacing between Light Probes in a [brick](probevolumes-concept.md#brick-size-and-light-probe-density). |
| **Sampling Noise** | Enable to increase or decrease HDRP adding noise to lighting, to help [fix seams](probevolumes-fixissues.md#fix-seams). |
| **Animate Sampling Noise** | Enable to animate sampling noise when Temporal Anti-Aliasing (TAA) is enabled. This can make noise patterns less visible. |
| **Leak Reduction Mode** | Enable to choose the method Unity uses to reduce leaks. See [Fix light leaks](probevolumes-fixissues.md#fix-light-leaks).<br/>Options:<br/>&#8226; **Validity and Normal Based**: Enable to make HDRP prevent invalid Light Probes contributing to the lighting result, and give Light Probes more weight than others based on the object pixel's sampling position.<br/>&#8226; **None**: No leak reduction.
| **Min Valid Dot Product Value** | Enable to make HDRP reduce a Light Probe's influence on an object if the direction towards the Light Probe is too different to the object's surface normal direction. The value is the minimum [dot product](https://docs.unity3d.com/ScriptReference/Vector3.Dot.html) between the two directions where HDRP will reduce the Light Probe's influence. |
| **Occlusion Only Reflection Normalization** | Enable to limit Reflection Probe Normalization so it only decreases the intensity of reflections. Keep this enabled to reduce light leaks. See [Frame Settings](Frame-Settings.md#lighting). |


## Size gizmo
<a name ="size-gizmo"></a>
The resize handles you use for other GameObjects do not resize a Probe Volume. In this screenshot, a red box indicates one of the handles.

![](Images/ProbeVolume-Size-gizmo.png)<br/>
The resize handles for Probe Volumes.
