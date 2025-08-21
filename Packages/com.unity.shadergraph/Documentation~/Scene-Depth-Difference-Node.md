# Scene Depth Difference node

The Scene Depth Difference node returns the difference in depth between a world space position and a value from the depth buffer.

## Ports

| **Name** | **Direction** | **Type** | **Binding** | **Description** |
|-|-|-|-|-|
| **Scene UV** | Input | Vector 4 | None | Sets the normalized coordinates at which to fetch the scene depth from the depth buffer. The default is the normalized x, y coordinates of the fragment in screen space. For more information about the options, refer to the [Screen Position node](Screen-Position-Node.md). |
| **Position WS** | Input | Vector 3 | None | Sets the world space position to compare the depth value at **Scene UV** to. The default is the x, y, z position of the fragment in world space. |
| **Out** | Output | Float | None | The difference in depth between **Scene UV** and **Position WS**. The value depends on the **Sampling mode** property. The distance is negative if the depth value from **Scene UV** is closer to the camera than the depth from **Position WS**. |

## Sampling modes

| **Name** | **Description** |
|----------|------------------------------------|
| **Linear 01** | Returns the distance in linear normalized space. The minimum distance is 0, and the maximum distance is 1. |
| **Raw** | Returns the distance in the non-linear space the depth buffer uses. The minimum distance is 0, and the maximum distance is 1. |
| **Eye** | Returns the distance in meters. |
