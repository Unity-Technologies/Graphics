# Adaptive Probe Volumes panel properties

This page explains the properties in the **Adaptive Probe Volumes** panel in Lighting settings. To open the panel, from the main menu select **Window** &gt; **Rendering** &gt; **Lighting** &gt; **Adaptive Probe Volumes**.

## Baking

To open Baking Set properties, either select the Baking Set asset in the Project window, or from the main menu select **Window** &gt; **Rendering** &gt; **Lighting** &gt; **Adaptive Probe Volumes** tab.

### Baking

|Property   |Description   |
|:---|:---|
|**Baking Mode**   |<ul><li>**Single Scene:** Use only the active scene to calculate the lighting data in Adaptive Probe Volumes.</li> <li>**Baking Set:** Use the scenes in this Baking Set to calculate the lighting data in Adaptive Probe Volumes.</li></ul>|
|**Current Baking Set**   |The current Baking Set asset.   |
|**Scenes in Baking Set**   |Lists the scenes in the current Baking Set:<ul><li>**Status:** Indicates whether the scene is loaded.</li><li>**Bake:** When enabled, HDRP generates lighting for this scene.</li></ul>Use + and &minus; to add add or remove a scene from the active Baking Set. Use the two-line icon to the left of each scene to drag the scene up or down in the list.  |

### Probe Placement

|Property   |Description   |
|:---|:---|
|**Probe Positions**   |<ul><li>**Recalculate:** Recalculate probe positions during baking, to accommodate changes in scene geometry. Refer to [Bake different lighting setups with Lighting Scenarios](probevolumes-bakedifferentlightingsetups.md) for more information.</li><li>**Don't Recalculate:** Don't recalculate probe positions during baking. This keeps the probe positions the same as the last successful bake, which means HDRP can blend probes in different Lighting Scenarios. Refer to [Bake different lighting setups with Lighting Scenarios](probevolumes-bakedifferentlightingsetups.md) for more information.</li></ul>  |
|**Min Probe Spacing**   |<a name="minprobespacing"></a> The minimum distance between probes, in meters. Refer to [Configure the size and density of Adaptive Probe Volumes](probevolumes-changedensity.md) for more information.  |
|**Max Probe Spacing**   |<a name="maxprobespacing"></a>The maximum distance between probes, in meters. Refer to [Configure the size and density of Adaptive Probe Volumes](probevolumes-changedensity.md) for more information.   |
|**Renderer Filter Settings**   |<ul><li>**Layer Mask:** Specify the [Layers](xref:um-layers) HDRP considers when it generates probe positions. Select a Layer to enable or disable it.</li><li>**Min Renderer Size:** The smallest <a href="https://docs.unity3d.com/ScriptReference/Renderer.html">Renderer</a> size HDRP considers when it places probes.</li></ul>  |


### Lighting Scenarios

This section appears only if you enable **Lighting Scenarios** under **Light Probe Lighting** in the [HDRP Asset](HDRP-Asset.md).

|Property   |Description   |
|:---|:---|
|**Scenarios**   |Lists the Lighting Scenarios in the Baking Set. To rename a Lighting Scenario, double-click its name. <ul><li>**Active:** Set the currently loaded Lighting Scenario, which HDRP writes to when you select **Generate Lighting**.</li><li>**Status:** Indicates the status of the active Lighting Scenario:<ul><li>**Invalid Scenario:** A warning icon appears if the active Lighting Scenario is baked but HDRP can't load it anymore, for example if another Lighting Scenario has been baked that caused changes in the probe subdivision.</li><li>**Not Baked:** An information icon appears if you haven't baked any lighting data for the active Lighting Scenario.</li><li>**Not Loaded:** An information icon appears if scenes in the Baking Set aren't currently loaded in the Hierarchy window, so HDRP can't determine the Lighting Scenario status.</li></li></ul> |

## Sky Occlusion Settings

| **Property** | **Description** |
|:-|:-|
| **Sky Occlusion** | Enable [sky occlusion](probevolumes-skyocclusion.md). |
| **Samples** | Set the number of samples Unity uses to calculate the light each probe receives from the sky. Higher values increase the accuracy of the sky occlusion data, but increasing baking time. The default value is 2048. |
| **Bounces** | Set the number of times Unity bounces light from the sky off objects when calculating the sky occlusion data. Higher values increase the accuracy of the sky occlusion data, but increase baking time. Use higher values if objects block the direct view from probes to the sky. The default value is 2. |
| **Albedo Override** | Set the brightness of the single color Unity uses to represent objects the sky light bounces off, instead of the actual color of the objects. Higher values brighten the baked sky occlusion lighting. The default value is 0.6. |
| **Sky Direction** | Enable Unity storing and using more accurate data about the directions from probes towards the sky. Refer to [Enable more accurate sky direction data](probevolumes-skyocclusion.md#enable-more-accurate-sky-direction-data) for more information. |

## Probe Invalidity Settings

|Property   |Description   |
|:---|:---|
|**Probe Dilation Settings**   |<ul><li>**Enable Dilation:** When enabled, HDRP replaces data in invalid probes with data from nearby valid probes. Enabled by default. Refer to [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md).</li><li>**Search Radius:** Determine how far from an invalid probe HDRP searches for valid neighbors. Higher values include more distant probes that might be in different lighting conditions than the invalid probe, resulting in unwanted behaviors such as light leaks.</li><li><a name="dilationvaliditythreshold"></a>**Validity Threshold:** Set the ratio of backfaces a probe samples before HDRP considers it invalid. Higher values mean HDRP is more likely to mark a probe invalid.</li><li>**Dilation Iterations:** Set the number of times Unity repeats the dilation calculation. This increases the spread of dilation effect, but increases the time HDRP needs to calculate probe lighting.</li><li>**Squared Distance Weighting:** Enable weighing the contribution of neighboring probes by squared distance, rather than linear distance. Probes that are closer to invalid probes will contribute more to the lighting data.</li></ul>    |
|**Virtual Offset Settings**   |<ul><li>**Enable Virtual Offset:** Enable HDRP moving the capture point of invalid probes into a valid area. Refer to [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md).</li><li>**Search Distance Multiplier:** Set the length of the sampling ray HDRP uses to search for valid probe positions. High values might cause unwanted results, such as probe capture points pushing through neighboring geometry.</li><li>**Geometry Bias:** Set how far HDRP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry.</li><li>**Ray Origin bias:** Set the distance between a probe's center and the point HDRP uses as the origin of each sampling ray. High values might cause unwanted results, such as rays missing nearby occluding geometry.</li><li>**Layer Mask:** Specify which layers HDRP includes in collision calculations for [Virtual Offset](probevolumes-fixissues.md).</li><li>**Refresh Virtual Offset Debug:** Re-run the virtual offset simulation to preview updated results, without affecting baked data.</li></ul>   |

### Adaptive Probe Volume Disk Usage

| Property | Description |
|:-|:-|
| **Scenario Size** | Indicates how much space on disk is used by the currently selected Lighting Scenario. |
| **Baking Set Size** | Indicates how much space on disk is used by all the baked Light Probe data for the currently selected Baking Set. This includes the data for all Lighting Scenarios, and the data shared by all Lighting Scenarios.|
