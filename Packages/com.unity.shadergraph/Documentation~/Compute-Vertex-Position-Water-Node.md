# Compute Water Vertex Position

This node provides access to the water mesh vertex position. It's used in water instead of the [Position node](Position-Node.html).

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. Don't modify the settings of this node. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Compute Water Vertex Position** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **PositionWS** | Output | Vector3 | The position of the water surface vertex in world space. |
