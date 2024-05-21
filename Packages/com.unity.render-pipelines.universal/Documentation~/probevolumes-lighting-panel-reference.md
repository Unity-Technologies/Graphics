# Adaptive Probe Volumes panel properties

This page explains the properties in the **Adaptive Probe Volumes** panel in Lighting settings. To open the panel, from the main menu select **Window** > **Rendering** > **Lighting** > **Adaptive Probe Volumes**.

## Baking

To open Baking Set properties, either select the Baking Set asset in the Project window, or from the main menu select **Window** > **Rendering** > **Lighting** > **Adaptive Probe Volumes** tab.

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
            <td>Use only the active scene to calculate the lighting data in Adaptive Probe Volumes.</td>
        </tr>
        <tr>
            <td><strong>Baking Set</strong></td>
            <td>Use the scenes in this Baking Set to calculate the lighting data in Adaptive Probe Volumes.</td>
        </tr>
        <tr>
            <td><b>Current Baking Set</b></td>
            <td colspan="2">The current Baking Set asset.</td>
        </tr>
        <tr>
            <td><b>Scenes in Baking Set</b></td>
            <td colspan="2">Lists the scenes in the current Baking Set.<br/><b>Status</b>: Indicates whether the scene is loaded.<br/><b>Bake</b>: When enabled, URP generates lighting for this scene.<br/>Use <b>+</b> and <b>-</b> to add or remove a scene from the active Baking Set.<br/>Use the two-line icon to the left of each scene to drag the scene up or down in the list.</td>
        </tr>
    </tbody>
</table>

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
      <td>Recalculate probe positions during baking, to accommodate changes in scene geometry. Refer to <a href="probevolumes-bakedifferentlightingsetups.md">Bake different lighting setups with Lighting Scenarios</a> for more information.</td>
    </tr>
    <tr>
      <td><strong>Don't Recalculate</strong></td>
      <td>Don't recalculate probe positions during baking. This keeps the probe positions the same as the last successful bake, which means URP can blend probes in different Lighting Scenarios. Refer to <a href="probevolumes-bakedifferentlightingsetups.md">Bake different lighting setups with Lighting Scenarios</a> for more information.</td>
    </tr>    
    <tr>
      <td rowspan="1"><strong>Min Probe Spacing</strong></td>
      <td colspan="2"><a name="minprobespacing"></a>The minimum distance between probes, in meters. Refer to <a href="probevolumes-changedensity.md">Configure the size and density of Adaptive Probe Volumes</a> for more information.</td>
    </tr>
    <tr>
      <td rowspan="1"><strong>Max Probe Spacing</strong></td>
      <td colspan="2"><a name="maxprobespacing"></a>The maximum distance between probes, in meters. Refer to <a href="probevolumes-changedensity.md">Configure the size and density of Adaptive Probe Volumes</a> for more information.</td>
    </tr>
    <tr>
      <td rowspan="3"><strong>Renderer Filter Settings</strong></td>
    </tr>
    <tr>
      <td><strong>Layer Mask</strong></td>
      <td>Specify the <a href="https://docs.unity3d.com/Manual/Layers.html">Layers</a> URP considers when it generates probe positions. Select a Layer to enable or disable it.</td>
    </tr>
    <tr>
      <td><strong>Min Renderer Size</strong></td>
      <td>The smallest <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a> size URP considers when it places probes.</td>
    </tr>
  </tbody>
</table>

### Lighting Scenarios

This section appears only if you enable **Lighting Scenarios** under **Light Probe Lighting** in the [URP Asset](universalrp-asset.md).

| **Property** ||| **Description** |
|-|-|-|-|
| **Scenarios** ||| Lists the Lighting Scenarios in the Baking Set. To rename a Lighting Scenario, double-click its name. |
|| **Active** || Set the currently loaded Lighting Scenario, which URP writes to when you select **Generate Lighting**. |
|| **Status** || Indicates the status of the active Lighting Scenario. |
||| **Invalid Scenario** | A warning icon appears if the active Lighting Scenario is baked but URP can't load it anymore, for example if another Lighting Scenario has been baked that caused changes in the probe subdivision. |
||| **Not Baked** | An information icon appears if you haven't baked any lighting data for the active Lighting Scenario.|
||| **Not Loaded** | An information icon appears if scenes in the Baking Set aren't currently loaded in the Hierarchy window, so URP can't determine the Lighting Scenario status. |

## Sky Occlusion Settings

| **Property** | **Description** |
|-|-|
| **Sky Occlusion** | Enable [sky occlusion](probevolumes-skyocclusion.md). |
| **Samples** | Set the number of samples Unity uses to calculate the light each probe receives from the sky. Higher values increase the accuracy of the sky occlusion data, but increasing baking time. The default value is 2048. |
| **Bounces** | Set the number of times Unity bounces light from the sky off objects when calculating the sky occlusion data. Higher values increase the accuracy of the sky occlusion data, but increase baking time. Use higher values if objects block the direct view from probes to the sky. The default value is 2. |
| **Albedo Override** | Set the brightness of the single color Unity uses to represent objects the sky light bounces off, instead of the actual color of the objects. Higher values brighten the baked sky occlusion lighting. The default value is 0.6. |
| **Sky Direction** | Enable Unity storing and using more accurate data about the directions from probes towards the sky. Refer to [Add dynamic color and shadows from the sky](probevolumes-skyocclusion.md#enable-more-accurate-sky-direction-data) for more information. |

## Probe Invalidity Settings

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
            <td>When enabled, URP replaces data in invalid probes with data from nearby valid probes. Enabled by default. Refer to <a href="probevolumes-fixissues.md">Fix issues with Adaptive Probe Volumes</a>.</td> 
        </tr>
        <tr>
            <td><strong>Search Radius</strong></td>
            <td>Determine how far from an invalid probe URP searches for valid neighbors. Higher values include more distant probes that might be in different lighting conditions than the invalid probe, resulting in unwanted behaviors such as light leaks.</td>
        </tr>
        <tr>
            <td><a name="dilationvaliditythreshold"></a><strong>Validity Threshold</strong></td>
            <td>Set the ratio of backfaces a probe samples before URP considers it invalid. Higher values mean URP is more likely to mark a probe invalid.</td>
        </tr>
        <tr>
            <td><strong>Dilation Iterations</strong></td>
            <td>Set the number of times Unity repeats the dilation calculation. This increases the spread of dilation effect, but increases the time URP needs to calculate probe lighting.</td>
        </tr>
        <tr>
            <td><strong>Squared Distance Weighting</strong></td>
            <td>Enable weighing the contribution of neighbouring probes by squared distance, rather than linear distance. Probes that are closer to invalid probes will contribute more to the lighting data.</td>
        </tr>
        <tr>
            <td rowspan="8"><strong>Virtual Offset Settings</strong></td>
        </tr>
        <tr>
            <td><strong>Enable Virtual Offset <a name="offset"></a></strong></td>
            <td>Enable URP moving the capture point of invalid probes into a valid area. Refer to <a href="probevolumes-fixissues.md">Fix issues with Adaptive Probe Volumes</a>.</td>
        </tr>
        <tr>
            <td><strong>Search Distance Multiplier</strong></td>
            <td>Set the length of the sampling ray URP uses to search for valid probe positions. High values might cause unwanted results, such as probe capture points pushing through neighboring geometry.</td>
        </tr>
        <tr>
            <td><strong>Geometry Bias</strong></td>
            <td>Set how far URP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry.</td>
        </tr>
        <tr>
            <td><strong>Ray Origin bias</strong></td>
            <td>Set the distance between a probe's center and the point URP uses as the origin of each sampling ray. High values might cause unwanted results, such as rays missing nearby occluding geometry.</td>
        </tr>
        <tr>
            <td><strong>Layer Mask</strong></td>
            <td>Specify which layers URP includes in collision calculations for [Virtual Offset](probevolumes-fixissues.md).</td>
        </tr>
        <tr>
            <td><strong>Refresh Virtual Offset Debug</strong></td>
            <td>Re-run the virtual offset simulation to preview updated results, without affecting baked data.</td>
        </tr>
    </tbody>
</table>

### Adaptive Probe Volume Disk Usage

| **Property** | **Description** |
|-|-|
| **Scenario Size** | Indicates how much space on disk is used by the baked Light Probe data. |
| **Baking Set Size** | Indicates how much space on disk is used by all the baked Light Probe data for the currently selected Baking Set. This includes the data for all Lighting Scenarios, and the data shared by all Lighting Scenarios.
