# Probe Adjustment Volume component reference

Select a [Probe Adjustment Volume Component](probevolumes-fixissues.md#add-a-probe-adjustment-volume-component) and open the Inspector to view its properties.

Refer to the following for more information about using the Probe Adjustment Volume component:

- [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md)
- [Configure the size and density of Adaptive Probe Volumes](probevolumes-changedensity.md)

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
                    <li><strong>Invalidate Probes</strong> Mark selected probes as invalid. Refer to <a href="probevolumes-fixissues.md#how-light-probe-validity-works">How light probe validity works</a> for more information.</li>
                    <li><strong>Override Validity Threshold</strong> Override the threshold URP uses to determine whether Light Probes are marked as invalid. Refer to <a href="probevolumes-fixissues.md#adjust-dilation">Adjust Dilation</a> for more information.</li>
                    <li><strong>Apply Virtual Offset</strong> Change the position Light Probes use when sampling the lighting in the scene during baking. Refer to <a href="probevolumes-fixissues.md#adjust-virtual-offset">Adjust Virtual Offset</a> for more information.</li>
                    <li><strong>Override Virtual Offset Settings</strong> Override the biases URP uses during baking to determine when Light Probes use Virtual Offset, and calculate sampling positions. Refer to <a href="probevolumes-fixissues.md#adjust-virtual-offset">Adjust Virtual Offset</a> for more information</li>
                    <li><strong>Intensity Scale</strong> Override the intensity of probes to brighten or darken affected areas.</li>
                    <li><strong>Override Sky Direction</strong> Override the directions Unity uses to sample the ambient probe, if you enable <a href="probevolumes-skyocclusion.md">sky occlusion</a>.</li>
                    <li><strong>Override Sample Count:</strong> Override the number of samples Unity uses for Adaptive Probe Volumes.</li>                      
                </ul>
            </td>
        </tr>
    </tbody>
</table>

### Mode settings

<table>
    <thead>
        <tr>
            <th><strong>Property</strong></th>
            <th colspan="2"><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td><strong>Dilation Validity Threshold</strong></td>
            <td colspan="2">
                <p>Override the ratio of backfaces a probe samples before URP considers it invalid. This option only appears if you set <b>Mode</b> to <b>Override Validity Threshold</b>, and you enable <b>Additional Properties</b>.</p>
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
                <p>Set how far URP pushes probes along the Virtual Offset Rotation vector. This option only appears if you set <b>Mode</b> to <b>Apply Virtual Offset</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Geometry Bias</strong></td>
            <td colspan="2">
                <p>Sets how far URP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry. This option only appears if you set <b>Mode</b> to <b>Override Virtual Offset Settings</b>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Ray Origin Bias</strong></td>
            <td colspan="2"><p>Override the distance between a probe's center and the point URP uses to determine the origin of that probe's sampling ray. This can be used to push rays beyond nearby geometry if the geometry causes issues. This option appears only if you set <b>Mode</b> to <b>Override Virtual Offset Settings</b>.</p>   
            </td>
        </tr>
        <tr>
            <td><strong>Intensity Scale</strong></td>
            <td colspan="2">
                <p>Change the brightness of all probes covered by the Probe Volumes Adjustment Volume component. Use this sparingly, because changing the intensity of probe data can lead to inconsistencies in the lighting. This option only appears if you set <strong>Mode</strong> to <strong>Intensity Scale</strong>.</p>
            </td>
        </tr>
        <tr>
            <td><strong>Sky Direction</strong></td>
            <td colspan="2">
                <p>Set the direction Unity uses to sample the ambient probe. This option only appears if you set <strong>Mode</strong> to <strong>Override Sky Direction</strong>.</p>
            </td>
        </tr>
        <tr>
            <td rowspan="5"><strong>Probes</strong></td>
        </tr>
        <tr>
            <td><strong>Direct Sample Count</strong></td>
            <td>Set the number of samples Unity uses to calculate the direct light each probe stores. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>
        <tr>
            <td><strong>Indirect Sample Count</strong></td>
            <td>Set the number of samples Unity uses to calculate the indirect light each probe stores, including environment samples. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>
        <tr>
            <td><strong>Sample Count Multiplier</strong></td>
            <td>Set the value Unity multiplies <strong>Direct Sample Count</strong> and <strong>Indirect Sample Count</strong> by. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>
        <tr>
            <td><strong>Max Bounces</strong></td>
            <td>Set the number of times Unity bounces light off objects. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>
        <tr>
            <td colspan="1" rowspan="10"><strong>Sky Occlusion</strong></td>
        </tr>        
        <tr>
            <td><strong>Sample Count</strong></td>
            <td>Set the number of samples Unity uses to calculate the amount of light each probe receives from the sky, if you enable <a href="probevolumes-skyocclusion.md">sky occlusion</a>. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>
        <tr>
            <td><strong>Max Bounces</strong></td>
            <td>Set the number of times Unity bounces light from the sky off objects to calculate the sky occlusion data, if you enable <a href="probevolumes-skyocclusion.md">sky occlusion</a>. This option only appears if you set <strong>Mode</strong> to <strong>Override Sample Count</strong>.</td>
        </tr>        
    </tbody>
</table>
