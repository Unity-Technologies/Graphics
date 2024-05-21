# Probe Adjustment Volume component reference

Select a [Probe Adjustment Volume Component](probevolumes-fixissues.md#add-a-probe-adjustment-volume-component) and open the Inspector to view its properties.

Refer to the following for more information about using the Probe Adjustment Volume component:

- [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md)

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
            <td>Set the shape of the Adjustment Volume to either <strong>Box</strong> or <strong>Sphere</strong>.</td>
        </tr>
        <tr>
            <td><strong>Size</strong></td>
            <td>Set the size of the Adjustment Volume. This property only appears if you set <strong>Shape</strong> to <strong>Box</strong>. </td>
        </tr>
        <tr>
            <td><strong>Radius</strong></td>
            <td>Set the radius of the Adjustment Volume. This property only appears if you set <strong>Shape</strong> to <strong>Sphere</strong>.</td>
        </tr>
        <tr>
            <td><strong>Mode</strong></td>
            <td colspan="2">
                <p>Select how to override probes inside the Adjustment Volume.</p>
                <ul>
                    <li><strong>Invalidate Probes:</strong> Mark selected probes as invalid. Refer to <a href="probevolumes-fixissues.md#how-light-probe-validity-works">How light probe validity works</a> for more information.</li>
                    <li><strong>Override Validity Threshold:</strong> Override the threshold HDRP uses to determine whether Light Probes are marked as invalid. Refer to <a href="probevolumes-fixissues.md#adjust-dilation">Adjust Dilation</a> for more information.</li>
                    <li><strong>Apply Virtual Offset:</strong> Change the position Light Probes use when sampling the lighting in the scene during baking. Refer to <a href="probevolumes-fixissues.md#adjust-virtual-offset">Adjust Virtual Offset</a> for more information.</li>
                    <li><strong>Override Virtual Offset Settings:</strong> Override the biases HDRP uses during baking to determine when Light Probes use Virtual Offset, and calculate sampling positions. Refer to <a href="probevolumes-fixissues.md#adjust-virtual-offset">Adjust Virtual Offset</a> for more information</li>
                    <li><strong>Intensity Scale:</strong> Override the intensity of probes to brighten or darken affected areas.</li>
                    <li><strong>Override Sky Direction</strong> Override the directions Unity uses to sample the ambient probe, if you enable <a href="probevolumes-skyocclusion.md">sky occlusion</a>.</li>
                    <li><strong>Override Sample Count:</strong> Override the number of samples Unity uses for Adaptive Probe Volumes.</li>        
                </ul>
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
            <td colspan="2"><p>Override the distance between a probe's center and the point HDRP uses to determine the origin of that probe's sampling ray. This can be used to push rays beyond nearby geometry if the geometry causes issues. This option appears only if you set <b>Mode</b> to <b>Override Virtual Offset Settings</b>.</p>   
            </td>
        </tr>
        <tr>
            <td><strong>Intensity Scale</strong></td>
            <td colspan="2">
                <p>Change the brightness of all probes covered by the Probe Volumes Adjustment Volume component. Use this sparingly, because changing the intensity of probe data can lead to inconsistencies in the lighting. This option only appears if you set <strong>Mode</strong> to <strong>Intensity Scale</strong>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Preview Probe Adjustments</strong></td>
            <td colspan="2">
                <p>Preview the effect of the adjustments in the Scene view and the <a href="rendering-debugger-window-reference.md">Rendering Debugger</a>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Bake Probe Volumes</strong></td>
            <td colspan="2">
                <p>Bake the Adaptive Probe Volumes with the adjustments.</p>
            </td>
        </tr>                
    </tbody>
</table>

## Override Sample Count properties

These properties are visible only when you set **Mode** to **Override Sample Count**.

### Probes

| Property | Description |
|-|-|
| **Direct Sample Count** | Set the number of samples Unity uses to calculate direct lighting. |
| **Indirect Sample Count** | Set the number of samples Unity uses to calculate indirect lighting. |
| **Sample Count Multiplier** | Set a value to multiply **Direct Sample Count** and **Indirect Sample Count** by. |
| **Max Bounces** | Set the maximum number of times Unity bounces light off objects when it calculates indirect lighting. |

### Sky Occlusion

These properties only have an effect if you enable sky occlusion. Refer to [Update light from the sky at runtime with sky occlusion](probevolumes-skyocclusion.md) for more information.

| Property | Description |
|-|-|
| **Sample Count** | Set the number of samples Unity uses to calculate a sky occlusion value for each probe. |
| **Max Bounces** | Set the maximum number of times Unity bounces light off objects when it calculates a sky occlusion value. |
