# Custom Interpolator block node reference

Pass per-vertex data from the vertex stage to the fragment stage through a [Custom Interpolator node](Custom-Interpolator-node-reference.md).

To complete the setup, you need to [add a Custom interpolator](Custom-Interpolators.md) that corresponds to the Custom Interpolator block node.

## Settings

| Property          | Description                                                                                                                                                                                                                                                                                                                                                                         |
|:------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**          | Sets the unique name of the custom interpolator to identify and reference it in the graph. |
| **Type**          | Sets the number of channels the Custom Interpolator exposes. The default value is **Vector 4**, which exposes x, y, z, and w channels.                                                                                                                                                                                                                  |
| **Interpolation** | Selects how Unity interpolates the value from vertex to fragment across the surface. The following options are available: <ul><li><b>Linear</b>: Applies the default linear interpolation, which preserves correct rates of change in screen space.</li><li><b>No Perspective</b>: Doesn't correct perspective, which can warp data, depending on the angle between the surface and the camera.</li><li><b>No Interpolation</b>: Doesn't interpolate the data, which creates hard edges between triangles.</li></ul> |

## Additional resources

* [Built-in blocks](Built-In-Blocks.md)
* [Add a custom interpolator](Custom-Interpolators.md)
* [Custom Interpolator node reference](Custom-Interpolator-node-reference.md)
