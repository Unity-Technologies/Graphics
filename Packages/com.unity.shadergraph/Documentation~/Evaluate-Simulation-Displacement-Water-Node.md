# Evaluate Water Simulation Displacement

This node calculates water surface displacement. 

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Water Simulation Displacement** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **PositionWS** | Input | Vector3 | The position of the water surface vertex in world space. |
| **BandsMultiplier** | Input | Vector4 | The amount to dampen displacement for each water band. Bands are different wave frequencies that create swells, agitations or ripples on the water. |
| **Displacement** | Output | Vector3 | The vertical and horizontal displacement of the water. |
| **LowFrequencyHeight** | Output | Float | The vertical displacement of the water surface. This doesn't include ripples. |
| **SSSMask** | Output | Float | Mask that defines where the water surface has subsurface scattering. |
