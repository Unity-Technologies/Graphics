# Evaluate Simulation Additional Data

This node provides access to Water's surface foam, surface gradient, and deep foam. You can also use this node to dampen the normals for each [water band](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-simulation.html#simulation-bands).

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph to fetch data from the water simulation. Don't modify the settings of this node. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Simulation Additional Data** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **BandsMultiplier** | Input | Vector4 | The amount to dampen displacement for each water band. Bands are different wave frequencies that create swells, agitations or ripples on the water. |
| **SurfaceGradient** | Output | Vector3 | The perturbation of the normal, as a surface gradient.|
| **LowFrequencySurfaceGradient** | Output | Vector3 | The perturbation of the low frequency normal, as a surface gradient. The low frequency normal is the normal of the water surface without high frequency details such as ripples. |
| **SurfaceFoam** | Output | Float | The amount of foam. |
| **DeepFoam** | Output | Float | The amount of foam under the water's surface. |

