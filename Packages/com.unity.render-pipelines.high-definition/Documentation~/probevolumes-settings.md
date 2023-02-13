# Probe Volume settings and properties

This page explains the settings you can use to configure Probe Volumes.

<a name="pv-tab"></a>
## Baking Set properties

To open Baking Set properties, either select the Baking Set asset in the Project window, or go to **Window > Rendering > Lighting** and select the **Probe Volumes** tab.

### Baking

<table>
    <thead>
        <tr>
            <th><strong>Property</strong></th>
            <th colspan="2"><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td rowspan="3"><strong>Baking Mode</strong></td>
        </tr>
        <tr>
            <td><strong>Single Scene</strong></td>
            <td>Bake only the active scene.</td>
        </tr>
        <tr>
            <td><strong>Baking Sets (Advanced)</strong></td>
            <td>Bake all scenes that are part of the Baking Set</td>
        </tr>
        <tr>
            <td><b>Baking Set</b></td>
            <td colspan="2">Indicates the active Baking Set.</td>
        </tr>
        <tr>
            <td><b>Scenes</b></td>
            <td colspan="2">Lists the Scenes in the active Baking Set.<br/><b>Scene</b>: Indicates whether the scene is loaded.<br/><b>Bake</b>: When enabled, HDRP generates lighting for this scene.<br/>Use <b>+</b> and <b>-</b> to add or remove a Scene from the active Baking Set.<br/>Use the three-line icon to the left of each Scene to drag the Scene up or down in the list.</td>
        </tr>
    </tbody>
</table>

### Lighting Scenarios

This section appears only if you enable **Lighting Scenarios** under **Light Probe Lighting** in the [HDRP Asset](HDRP-Asset.md).

| **Property** ||| **Description** |
|-|-|-|-|
| **Scenarios** ||| Lists the Lighting Scenarios in the Baking Set. To rename a Lighting Scenario, double-click its name. |
|| **Active** || Set the currently loaded Lighting Scenario, which HDRP writes to when you select **Generate Lighting**. |
|| **Status** || Indicates the status of the active Lighting Scenario. |
||| **Invalid Scenario** | A warning icon appears if the active Lighting Scenario is baked but HDRP can't load it anymore, for example if another Lighting Scenario has been baked that caused changes in the probe subdivision. |
||| **Not Baked** | An information icon appears if you haven't baked any lighting data for the active Lighting Scenario.|
||| **Not Loaded** | An information icon appears if scenes in the Baking Set aren't currently loaded in the Hierarchy window, so HDRP can't determine the Lighting Scenario status. |

### Probe Placement

<table>
  <thead>
    <tr>
      <th colspan="1"><strong>Property</strong></th>
      <th colspan="2"><strong>Description</strong></th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td rowspan="3"><strong>Probe Positions</strong></td>
    </tr>
    <tr>
      <td><strong>Recalculate</strong></td>
      <td>Recalculate probe positions during baking, so you can bake multiple Lighting Scenarios that would produce different brick layouts due to differences in scene geometry.</td>
    </tr>
    <tr>
      <td><strong>Don't Recalculate</strong></td>
      <td>Don't recalculate probe positions during baking.</td>
    </tr>
    <tr>
      <td rowspan="1"><strong>Min Probe Spacing</strong></td>
      <td colspan="2"><a name="minprobespacing"></a>The minimum distance between probes, in meters. See <a href="probevolumes-showandadjust.md">Display and adjust Probe Volumes</a> for additional information.</td>
    </tr>
    <tr>
      <td rowspan="1"><strong>Max Probe Spacing</strong></td>
      <td colspan="2"><a name="maxprobespacing"></a>The maximum distance between probes, in meters. See <a href="probevolumes-showandadjust.md">Display and adjust Probe Volumes</a> for additional information.</td>
    </tr>
    <tr>
      <td rowspan="3"><strong>Renderer Filter Settings</strong></td>
    </tr>
    <tr>
      <td><strong>Layer Mask</strong></td>
      <td>Specify the <a href="https://docs.unity3d.com/Manual/Layers.html">Layers</a> HDRP considers when it generates probe positions. Select a Layer to enable or disable it.</td>
    </tr>
    <tr>
      <td><strong>Min Renderer Size</strong></td>
      <td>The smallest <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a> size HDRP considers when it places probes.</td>
    </tr>
  </tbody>
</table>

### Probe Invalidity Settings

<table>
    <thead>
        <tr>
            <th colspan="1"><strong>Property</strong></th>
            <th colspan="2"><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td rowspan="6"><strong>Probe Dilation Settings</strong></td>
        </tr>
        <tr>
            <td><strong>Enable Dilation</strong><a name="dilationsettings"></a></td>
            <td>Enable HDRP replacing data in invalid probes with data from valid probes nearby. Enabled by default. See <a href="probevolumes-fixissues.md">Fix issues with Probe Volumes</a>.</td>
        </tr>
        <tr>
            <td><strong>Search Radius</strong></td>
            <td>Determine how far from an invalid probe HDRP searches for valid neighbors. Higher values include more distant probes that may be in different lighting conditions than the invalid probe, resulting in unwanted behaviors.</td>
        </tr>
        <tr>
            <td><a name="dilationvaliditythreshold"></a><strong>Validity Threshold</strong></td>
            <td>Set the ratio of backfaces a probe samples before HDRP considers it invalid. Higher values mean HDRP is more likely to mark a probe invalid.</td>
        </tr>
        <tr>
            <td><strong>Dilation Iterations</strong></td>
            <td>Set the number of times Unity repeats the dilation calculation. This increases spread of dilation effect, but requires additional processing power.</td>
        </tr>
        <tr>
            <td><strong>Squared Distance Weighting</strong></td>
            <td>Enable weighing the contribution of neighbouring probes by squared distance, rather than linear distance.</td>
        </tr>
        <tr>
            <td rowspan="8"><strong>Virtual Offset Settings</strong></td>
        </tr>
        <tr>
            <td><strong>Enable Virtual Offset <a name="offset"></a></strong></td>
            <td>Enable HDRP moving the capture point of invalid probes so they're valid again. See <a href="probevolumes-fixissues.md">Fix issues with Probe Volumes</a>.</td>
        </tr>
        <tr>
            <td><strong>Search Distance Multiplier</strong></td>
            <td>Set the length of the sampling ray HDRP uses to search for valid probe positions. High values may cause unwanted results, such as probe capture points pushing through neighboring geometry.</td>
        </tr>
        <tr>
            <td><strong>Geometry Bias</strong></td>
            <td>Set how far HDRP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry.</td>
        </tr>
        <tr>
            <td><strong>Ray Origin bias</strong></td>
            <td>Set the distance between a probe's center and the point HDRP uses to determine the origin of that probe's sampling ray. High values may cause unwanted results, such as sampling from an area of the scene with dissimilar lighting.</td>
        </tr>
        <tr>
            <td><strong>Max Ray Hits</strong></td>
            <td>Set how many times a sampling ray hits geometry before HDRP determines the position of the probe where that ray originated.</td>
        </tr>
        <tr>
            <td><strong>Layer Mask</strong></td>
            <td>Specify which layers HDRP includes in collision calculations for [Virtual Offset](probevolumes-fixissues.html).</td>
        </tr>
        <tr>
            <td><strong>Refresh Virtual Offset Debug</strong></td>
            <td>Re-run the virtual offset simulation; it will be applied only for debug visualization sake and not affect baked data.</td>
        </tr>
    </tbody>
</table>

### Probe Volume Disk Usage

| **Property** | **Description** |
|-|-|
| **Scenario Size** | Indicates how much space on disk is used by the currently selected Lighting Scenario. |
| **Baking Set Size** | Indicates how much space on disk is used by all the baked data for the currently selected Baking Set. This includes the data for all Lighting Scenarios, and the data shared by all Lighting Scenarios.

## Probe Volume Properties
<a name="pv-inspector"></a>

Select a Probe Volume and open the Inspector to view its properties.

<table>
<thead>
    <tr>
    <th><strong>Property</strong></th>
    <th colspan="2"><strong>Description</strong></th>
    </tr>
</thead>
<tbody>
    <tr>
    <tr>
        <td rowspan="4"><strong>Mode</strong></td>
    </tr>
    <tr>
        <td><strong>Global</strong></td>
        <td>HDRP sizes this Probe Volume to include all Renderers in your project that <a href="https://docs.unity3d.com/Manual/class-MeshRenderer.html">contribute Global Illumination.</a> HDRP recalculates the volume size every time you save or generate lighting.</td>
    </tr>
    <tr>
        <td><strong>Scene</strong></td>
        <td>HDRP sizes this Probe Volume to include all Renderers in the same scene as this Probe Volume. HDRP recalculates the volume size every time you save or generate lighting.</td>
    </tr>
    <tr>
        <td><strong>Local</strong></td>
        <td>Set the size of this Probe Volume manually.</td>
    </tr>
    <tr>
        <td colspan="2"><strong>Size</strong></td>
        <td colspan="">Set the size of this Probe Volume. This setting only appears when <strong>Mode</strong> is set to <strong>Local</strong>.</td>
    </tr>
    <tr>
        <td rowspan="2"><strong>Subdivision Override</strong></td>
    </tr>
    <tr>
        <td><strong>Override Probe Spacing</strong></td>
        <td>Override the Probe Spacing set in the <strong>Baking Set</strong> for this Probe Volume.</td>
    </tr>
    <tr>
        <td rowspan="5"><strong>Geometry Settings</strong></td>
    </tr>
    <tr>
        <td><strong>Override Renderer Filters</strong></td>
        <td>Enable filtering by Layer which GameObjects HDRP considers when it generates probe positions.</td>
    </tr>
    <tr>
        <td><strong>Layer Mask</strong></td>
        <td>Filter by Layer which GameObjects HDRP considers when it generates probe positions.</td>
    </tr>
    <tr>
        <td><strong>Min Renderer Size</strong></td>
        <td>The smallest <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a> size HDRP considers when it generates probe positions.</td>
    </tr>
    <tr>
        <td><strong>Fill Empty Spaces</strong></td>
        <td>Enable HDRP filling the empty space between Renderers with bricks that have the largest distance between probes.</td>
    </tr>
</tbody>
</table>

## Probe Adjustment Volume
<a name="pv-adjustment"></a>

Select a [Probe Adjustment Volume Component](probevolumes-fixissues.md#add-a-probe-adjustment-volume-component) and open the Inspector to view its properties.

<table>
    <thead>
        <tr>
            <th><strong>Property</strong></th>
            <th colspan="2"><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td rowspan="4"><strong>Influence Volume</strong></td>
        </tr>
        <tr>
            <td><strong>Shape</strong></td>
            <td>Set the shape of the Adjustment Volume to either **Box** or **Sphere**.</td>
        </tr>
        <tr>
            <td><strong>Size</strong></td>
            <td>Set the size of the Adjustment Volume. This property only appears if you set **Shape** to **Box**. </td>
        </tr>
        <tr>
            <td><strong>Radius</strong></td>
            <td>Set the radius of the Adjustment Volume. This property only appears if you set **Shape** to **Sphere**.</td>
        </tr>
        <tr>
            <td><strong>Mode</strong></td>
            <td colspan="2">
                <p>Select how to override probes inside the Adjustment Volume.</p>
                <ul>
                    <li><strong>Invalidate Probes:</strong> Mark selected probes as invalid.</li>
                    <li><strong>Override Validity Threshold:</strong> Override **Dilation Validity Threshold**.</li>
                    <li><strong>Apply Virtual Offset:</strong> Manually apply a Virtual Offset on selected probes.</li>
                    <li><strong>Override Virtual Offset Settings:</strong> Override Virtual Offset biases.</li>
                </ul>
            </td>
        </tr>
        <tr>
            <td><strong>Intensity Scale</strong></td>
            <td colspan="2">
                <p>Set the scale HDRP applies to all probes in the Adjustment Volume. Use this sparingly, because changing the intensity of probe data can lead to inconsistencies in the lighting. This option only appears if you set <b>Mode</b> to <b>Invalidate Probes</b>, and you enable <b>Additional Properties</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Dilation Validity Threshold</strong></td>
            <td colspan="2">
                <p>Override the ratio of backfaces a probe samples before HDRP considers it invalid. This option only appears if you set <b>Mode</b> to <b>Override Validity Threshold</b>, and you enable <b>Additional Properties</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Virtual Offset Rotation</strong></td>
            <td colspan="2">
                <p>Set the rotation angle for the Virtual Offset vector on all probes in the Adjustment Volume. This option only appears if you set <b>Mode</b> to <b>Apply Virtual Offset</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Virtual Offset Distance</strong></td>
            <td colspan="2">
                <p>Set how far HDRP pushes probes along the Virtual Offset Rotation vector. This option only appears if you set <b>Mode</b> to <b>Apply Virtual Offset</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Geometry Bias</strong></td>
            <td colspan="2">
                <p>Sets how far HDRP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry. This option only appears if you set <b>Mode</b> to <b>Override Virtual Offset Settings</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Ray Origin Bias</strong></td>
            <td colspan="2"><p>Override the distance between a probe's center and the point HDRP uses to determine the origin of that probe's sampling ray. This option only appears if you set <b>Mode</b> to <b>Override Virtual Offset Settings</b>.</p>
            </td>
        </tr>
    </tbody>
</table>

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
To resize the Probe Volume, use one of the handles of the box gizmo in the Scene View. You can't resize a Probe Volume by rescaling the GameObject or using the scale gizmo.

In this screenshot, a red box indicates the box gizmo handles.

![](Images/ProbeVolume-Size-gizmo.png)<br/>
The resize handles for Probe Volumes.
