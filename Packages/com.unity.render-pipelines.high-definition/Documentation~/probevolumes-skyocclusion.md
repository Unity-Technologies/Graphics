# Sky Occlusion

Sky Occlusion stores the amount of lighting from the Sky affecting probes in an Adaptive Probe Volume. During run-time, this data can be combined with lighting from the Scene’s Ambient Probe to dynamically relight the Scene based on changes to the Sky. See [Visual Environment Volume override](Override-Visual-Environment.md).

When Sky Occlusion is enabled for Adaptive Probe Volumes, an additional directional visibility factor is calculated for each probe during bake time. This gray value - stored as a spherical harmonic - is used during shading to attenuate the lighting contribution from the Sky. As multiple bounces can be used, the Sky’s effect upon probes with indirect paths to the Sky can also be calculated.

Static and dynamic objects can both receive lighting with Sky Occlusion. However, only static objects can affect the baked result. Enabling Sky Occlusion can lengthen the time required to bake lighting and uses additional memory at run-time.

## Enable Sky Occlusion

Sky Occlusion is enabled from the **Sky Occlusion** section of the **Adaptive Probe Volumes** tab within the **Lighting Window**.

Note that lighting data must be recalculated if Sky Occlusion is enabled for the first time, or is disabled following a bake.

## Modifying Sky Occlusion properties

It is possible to affect the visual quality and appearance of Sky Occlusion using these properties:

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
            <td>Determines the number of samples used when calculating the sky contribution for each probe. Increasing this value improves the accuracy of lighting data at the cost of the time required to bake Adaptive Probe Volumes.</td>
        </tr>
        <tr>
            <td><b>Bounces</b></td>
            <td>The number of bounces used when calculating the sky’s contribution on probes. Increasing the number of bounces can be useful in Scenes where probes may have very indirect routes to the Sky. This will also affect the time required to bake Adaptive Probe Volumes.</td>
        </tr>
        <tr>
            <td><strong>Albedo Override</strong></td>
            <td>Sky Occlusion does not consider the albedo (color) of Materials used throughout the Scene when calculating bounced lighting. Instead a single color is a used throughout the Scene. Albedo Override allows this color to be modified. Lower values darken and higher values will brighten the intensity of this value.</td>
        </tr>
        <tr>
            <td><b>Sky Direction</b></td>
            <td>Whether probes should store the dominant direction of incoming light from the Sky. Sky Direction increases memory usage but produces more accurate lighting. Without Sky Direction, the surface normals of objects are used instead and in some Scenes this can produce visual inaccuracies.</td>
        </tr>
    </tbody>
</table>

## Sky Direction

By default, Sky Direction is disabled and the surface normals of objects lit by probes are used to sample the Ambient Probe generated from the Sky.
When Sky Direction is enabled, Unity calculates - for each probe - the most appropriate incoming sky lighting direction. Where desirable, this can be locally overridden in specific areas of the Scene using a [Probe Adjustment Volume](probevolumes-concept.md#volume).

Enabling Sky Direction can improve visual results, especially in cave-like scenarios where the sky lighting needs to bounce several times on surfaces before reaching a surface. However the additional data required increases the time needed to bakelighting data. It also increases memory usage during run-time.

## Debugging Sky Occlusion

You can inspect the Sky Occlusion value using the **Display Probes** option in the [Rendering Debugger](Render-Pipeline-Debug-Window.md#ProbeVolume). Two views are provided in the **Probe Shading Mode** dropdown:
1. **Sky Occlusion SH**: Display the gray value (scalar) used to attenuate Sky lighting.
2. **Sky Direction**: Displays a green dot corresponding to the direction used to sample the Ambient Probe. If **Sky Direction** was not enabled or could not be computed this displays a red probe.

## Limitations

1. Currently Sky Occlusion does not work if the **Progressive CPU Lightmapper** is selected.
2. If Sky Occlusion is enabled or disabled, the Scene must be rebaked to update lighting data.
3. Sky Direction is not interpolated between probes. This may result in harsh lighting transitions where neighboring probes are storing very different results.

# Additional resources



* [Understand Probe Volumes](probevolumes-concept.md)
* [Visual Environment Volume override](Override-Visual-Environment.md)