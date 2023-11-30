# Control exposure

To work with physically based lighting and Materials, you need to set up the Scene exposure correctly. The High Definition Render Pipeline (HDRP) includes several methods for calculating exposure to suit most situations. HDRP expresses all exposure values that it uses in [EV<sub>100</sub>](Physical-Light-Units.md#EV).

## Use the exposure volume override

**Exposure** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Exposure** properties, you must add an **Exposure** override to a [Volume](understand-volumes.md) in your Scene. To add **Exposure** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** and select **Exposure**.

HDRP applies **Exposure** correction to any Camera this Volume affects.

[!include[](snippets/volume-override-api.md)]

To learn how to use exposure properties, refer to [exposure volume override reference](reference-override-exposure.md).

<a name="DebugModes"></a>

### Exposure Debug modes

HDRP offers several debug modes to help you to set the correct exposure for your scene. You can activate these in the [Debug window](rendering-debugger-window-reference.md). For more information, refer to [Debug exposure](test-debug-exposure.md).


