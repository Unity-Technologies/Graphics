# Probe Adjustment Volume component reference

Select a [Probe Adjustment Volume Component](probevolumes-fixissues.md#add-a-probe-adjustment-volume-component) and open the Inspector to view its properties.

Refer to the following for more information about using the Probe Adjustment Volume component:

- [Fix issues with Adaptive Probe Volumes](probevolumes-fixissues.md)

| Property | Description                                                                        |
| :------- | :--------------------------------------------------------------------------------- |
| **Influence Volume**    | Set the shape of the Adjustment Volume to either **Box** or **Sphere**. <ul><li>**Shape**: Set the shape of the Adjustment Volume to either **Box** or **Sphere**.</li> <li>**Size**: Set the size of the Adjustment Volume. This property only appears if you set **Shape** to **Box**.</li> <li>**Radius**: Set the radius of the Adjustment Volume. This property only appears if you set **Shape** to **Sphere**.</li> </ul> |
| **Mode**   | Select how to override probes inside the Adjustment Volume. <ul><li>**Invalidate Probes:** Mark selected probes as invalid. Refer to [How light probe validity works](probevolumes-fixissues.md#how-light-probe-validity-works) for more information.</li> <li>**Override Validity Threshold:** Override the threshold HDRP uses to determine whether Light Probes are marked as invalid. Refer to [Adjust Dilation](probevolumes-fixissues.md#adjust-dilation) for more information.</li> <li>**Apply Virtual Offset:** Change the position Light Probes use when sampling the lighting in the scene during baking. Refer to [Adjust Virtual Offset](probevolumes-fixissues.md#adjust-virtual-offset) for more information.</li> <li>**Override Virtual Offset Settings:** Override the biases HDRP uses during baking to determine when Light Probes use Virtual Offset, and calculate sampling positions. Refer to [Adjust Virtual Offset](probevolumes-fixissues.md#adjust-virtual-offset) for more information. </li> <li>**Intensity Scale:** Override the intensity of probes to brighten or darken affected areas.</li> <li>**Override Sky Direction:** Override the directions Unity uses to sample the ambient probe, if you enable [sky occlusion](probevolumes-skyocclusion.md).</li> <li>**Override Sample Count:** Override the number of samples Unity uses for Adaptive Probe Volumes.</li> </ul>  |
| **Dilation Validity Threshold**   | Override the ratio of backfaces a probe samples before HDRP considers it invalid. This option only appears if you set **Mode** to **Override Validity Threshold**, and you enable **Additional Properties**.   |
| **Virtual Offset Rotation**  | Set the rotation angle for the Virtual Offset vector on all probes in the Adjustment Volume. This option only appears if you set **Mode** to **Apply Virtual Offset**.   |
| **Virtual Offset Distance**   | Set how far HDRP pushes probes along the Virtual Offset Rotation vector. This option only appears if you set **Mode** to **Apply Virtual Offset**.  |
| **Geometry Bias**  | Sets how far HDRP pushes a probe's capture point out of geometry after one of its sampling rays hits geometry. This option only appears if you set **Mode** to **Override Virtual Offset Settings**.   |
| **Ray Origin Bias**   | Override the distance between a probe's center and the point HDRP uses to determine the origin of that probe's sampling ray. This can be used to push rays beyond nearby geometry if the geometry causes issues. This option appears only if you set **Mode** to **Override Virtual Offset Settings**.  |
| **Intensity Scale**   | Change the brightness of all probes covered by the Probe Volumes Adjustment Volume component. Use this sparingly, because changing the intensity of probe data can lead to inconsistencies in the lighting. This option only appears if you set **Mode** to **Intensity Scale**.   |
| **Preview Probe Adjustments**   | Preview the effect of the adjustments in the Scene view and the [Rendering Debugger](rendering-debugger-window-reference.md).   |
| **Bake Probe Volumes**   | Bake the Adaptive Probe Volumes with the adjustments.   |

## Override Sample Count properties

These properties are visible only when you set **Mode** to **Override Sample Count**.

### Probes

| Property | Description |
|:--------|:-------|
| **Direct Sample Count** | Set the number of samples Unity uses to calculate direct lighting. |
| **Indirect Sample Count** | Set the number of samples Unity uses to calculate indirect lighting. |
| **Sample Count Multiplier** | Set a value to multiply **Direct Sample Count** and **Indirect Sample Count** by. |
| **Max Bounces** | Set the maximum number of times Unity bounces light off objects when it calculates indirect lighting. |

### Sky Occlusion

These properties only have an effect if you enable sky occlusion. Refer to [Update light from the sky at runtime with sky occlusion](probevolumes-skyocclusion.md) for more information.

| Property | Description |
|:------|:------|
| **Sample Count** | Set the number of samples Unity uses to calculate a sky occlusion value for each probe. |
| **Max Bounces** | Set the maximum number of times Unity bounces light off objects when it calculates a sky occlusion value. |
