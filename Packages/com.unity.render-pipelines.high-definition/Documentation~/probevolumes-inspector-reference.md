# Adaptive Probe Volume Inspector reference

Select an Adaptive Probe Volume and open the Inspector to view its properties.

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
        <td>HDRP sizes this Adaptive Probe Volume to include all renderers in the scene or Baking Set that have **Contribute Global Illumination** enabled in their <a href="https://docs.unity3d.com/Manual/class-MeshRenderer.html">Mesh Renderer component</a>. HDRP recalculates the volume size every time you save or generate lighting.</td>
    </tr>
    <tr>
        <td><strong>Scene</strong></td>
        <td>HDRP sizes this Adaptive Probe Volume to include all renderers in the same scene as this Adaptive Probe Volume. HDRP recalculates the volume size every time you save or generate lighting.</td>
    </tr>
    <tr>
        <td><strong>Local</strong></td>
        <td>Set the size of this Adaptive Probe Volume manually.</td>
    </tr>
    <tr>
        <td colspan="2"><strong>Size</strong></td>
        <td colspan="">Set the size of this Adaptive Probe Volume. This setting only appears when you set <strong>Mode</strong> to <strong>Local</strong>.</td>
    </tr>
    <tr>
        <td rowspan="2"><strong>Subdivision Override</strong></td>
    </tr>
    <tr>
        <td><strong>Override Probe Spacing</strong></td>
        <td>Override the Probe Spacing set in the <strong>Baking Set</strong> for this Adaptive Probe Volume. This cannot exceed the <strong>Min Probe Spacing</strong> and <strong>Max Probe Spacing</strong> values in the <a href="probevolumes-lighting-panel-reference.md">Adaptive Probe Volumes panel in the Lighting window</a>.</td>
    </tr>
    <tr>
        <td rowspan="5"><strong>Geometry Settings</strong></td>
    </tr>
    <tr>
        <td><strong>Override Renderer Filters</strong></td>
        <td>Enable filtering by Layer which GameObjects HDRP considers when it generates probe positions. Use this to exclude certain GameObjects from contributing to Adaptive Probe Volume lighting.</td>
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
        <td>Enable HDRP filling the empty space between and around Renderers with bricks. Bricks in empty spaces always use the **Max Probe Spacing** value.</td>
    </tr>
</tbody>
</table>

## Size gizmo
<a name ="size-gizmo"></a>
To resize the Adaptive Probe Volume, use one of the handles of the box gizmo in the Scene view. You can't resize an Adaptive Probe Volume by changing the Transform component of the GameObject, or using the scale gizmo.

In this screenshot, a red box indicates the box gizmo handles.

![](Images/ProbeVolume-Size-gizmo.png)<br/>
The resize handles for Adaptive Probe Volumes.

## Probe Volume limitations with Asset Bundles and Addressables
<a name ="pv-assetbundles"></a>
Internally, the Probe Volume system uses the Streaming Asset feature to store baked data. This is necessary to allow both efficient loading and streaming of data. The consequence is that Probe Volume baked data is incompatible with Asset Bundles and Addressables as it is explicitly moved inside the Streaming Asset folder upon Player build.
In order to allow the use of Asset Bundles and Addressables when necessary, a toggle is provided in the Probe Volume Graphics settings: *Disable Streaming Assets*. When enabling this option, the system will no longer use Streaming Assets internally but regular Assets that can be managed manually by the user.
Enabling this option will also disable the use of Disk Streaming and increase memory consumption in multi-scene setups.
