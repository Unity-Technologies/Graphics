# Pack Water Vertex Data

This node packs multiple water properties into two UV properties for the vertex context.

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. Don't modify the settings of this node. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Pack Water Vertex Data** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **PositionWS** | Input | Vector3 | The position of the water surface vertex in world space. |
| **Displacement** | Input | Vector3 | The vertical and horizontal displacement of the water. |
| **LowFrequencyHeight** | Input | Float | The vertical displacement of the water surface. This doesn't include ripples. |
| **SSSMask** | Input | Float | Mask that defines where the water surface has subsurface scattering.  |
| **PositionOS** | Output | Vector3 | The position of the water surface vertex in object space.  |
| **NormalOS** | Output | Vector3 | The water surface normal in object space. |
| **uv0** | Output | Vector4 | The inputs packed into a UV coordinate set for the vertex context. |
| **uv1** | Output | Vector4 | The inputs packed into a UV coordinate set for the vertex context. |
