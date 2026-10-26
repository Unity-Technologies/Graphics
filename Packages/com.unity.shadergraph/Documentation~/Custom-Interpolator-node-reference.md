# Custom Interpolator node reference

Read data from a [Custom Interpolator block node](Custom-Interpolator-block-node-reference.md) of the vertex stage to pass it to the fragment stage.

To be able to add a Custom Interpolator node, you first need to [add a Custom interpolator block node](Custom-Interpolators.md) to the vertex stage.

## Output port

| Name | Type | Description |
| :--- | :--- | :--- |
| Out | Depends on the **Type** selected in the [Node Settings](#settings) | The data read from the matching Custom interpolator block node from the vertex stage. |

## Settings

| Property          | Description                                                                                                                                                                                                                                                                                                                                                                         |
|:------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**          | Sets the unique name of the custom interpolator to identify and reference it in the graph. |
| **Type**          | Sets the number of channels the Custom Interpolator exposes. The default value is **Vector 4**, which exposes x, y, z, and w channels.                                                                                                                                                                                                                  |
| **Interpolation** | Selects how Unity interpolates the value from vertex to fragment across the surface. The following options are available: <ul><li><b>Linear</b>: Applies the default linear interpolation, which preserves correct rates of change in screen space.</li><li><b>No Perspective</b>: Doesn't correct perspective, which can warp data, depending on the angle between the surface and the camera.</li><li><b>No Interpolation</b>: Doesn't interpolate the data, which creates hard edges between triangles.</li></ul> |

## Additional resources

* [Add a custom interpolator](Custom-Interpolators.md)
* [Custom Interpolator block node reference](Custom-Interpolator-block-node-reference.md)
