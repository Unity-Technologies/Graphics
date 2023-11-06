# Sky Occlusion

Probe Volumes's Sky Occlusion enhances Unity's probe-based lighting by adding dynamic time-of-day support. This is done by precomputing a directional visibility factor (a gray value stored in Spherical Harmonics), and at runtime (during shading), attenuating the lighting contribution from the Sky.

The Sky contribution is retrieved in the real-time sky ambient probe and thus can be dynamic, see [Visual Environment Volume override](Override-Visual-Environment.md).

The Sky Occlusion supports dynamic sky lighting, for both direct and indirect lighting (when the **bounces** parameter is greater than 0).

Since it is fully integrated in Probe Volumes it can lit static geometries and dynamic geometries. The main limitation is that dynamic geometries won't affect the occlusion since it is baked.

## Enable Sky Occlusion

You can enable Sky Occlusion in the **Lighting window** in the **Probe Volumes section**, see **Sky Occlusion Bake Settings**. Note that if you decide to switch it on or off you will need to rebake the Probe Volume.

## How to tweak Sky Occlusion

Sky Occlusion comes with some adjustable parameters

<table>
    <thead>
        <tr>
            <th><strong>Property</strong></th>
            <th colspan="2"><strong>Description</strong></th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td><strong>Samples</strong></td>
            <td>Control the number of samples used for sky occlusion baking. Increasing this value may improve the quality but increases the time required for baking to complete.</td>
        </tr>
        <tr>
            <td><b>Bounces</b></td>
            <td>Control the number of bounces used for sky occlusion baking.</td>
        </tr>
        <tr>
            <td><strong>Albedo Override</strong></td>
            <td>Bounced lighting for sky occlusion does not take material properties such as color and roughness into account. Thus a single color is applied to all materials. The colors range from 0 (black) to 1 (white).</td>
        </tr>
        <tr>
            <td><b>Backface Culling</b></td>
            <td>Enable backface culling for sky occlusion baking.</td>
        </tr>
        <tr>
            <td><b>Sky Direction</b></td>
            <td>In addition to sky occlusion, bake the most suitable direction to sample the ambient probe at runtime. Without it, surface normals would be used as a fallback and might lead to inaccuracies when updating the probes with the sky color.</td>
        </tr>
    </tbody>
</table>

## Sky Direction

By default, Sky Direction is turned off. When Sky Direction is off the surface normal of the object being lit by probes is used to sample the sky ambient probe.

Enabling Sky Direction can provide better results, especially in cave-like scenarios where the sky lighting needs to bounce several times on surfaces before reaching a surface. By enabling the Sky Direction, a better sampling direction will be computed at bake time, corresponding to the most appropriate direction for incoming sky lighting.
You can also override Unity's pre-baked Sky Direction by using a [Probe Adjustment Volume](probevolumes-concept.md#volume).

## Debugging Sky Occlusion

You can inspect the Sky Occlusion value using the **Display Probes** option in the [Rendering Debugger](Render-Pipeline-Debug-Window.md#ProbeVolume). Two views are provided in the **Probe Shading Mode** dropdown:
1. **Sky Occlusion SH**: Display the gray mask to occlude the sky value.
2. **Sky Direction**: Displays a green dot on a black sphere corresponding to the direction used to sample the sky ambient probe. If **Sky Direction** was not enabled or could not be computed this displays a red probe.

## Limitations

1. Sky Occlusion does not work if you select the **Progressive CPU Lightmapper**.
2. To enable or disable the Sky Occlusion you will need to rebake your Probe Volumes.
3. There is no interpolation in-between probes for the Sky Direction if you bake it, this may result in harsh lighting in some cases.

# Additional resources



* [Understand Probe Volumes](probevolumes-concept.md)
* [Visual Environment Volume override](Override-Visual-Environment.md)