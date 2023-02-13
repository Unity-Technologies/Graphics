# Evaluate Foam Data

This node calculates water foam intensity.

This node outputs foam as monochrome in the red channel. If you connect the output of this node to a **Base Color** block, all the channels are red. To prevent this, split the output and use only the red channel. You can't apply a tint to foam.

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).


## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Foam Data** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **SurfaceGradient** | Input | Vector3 | The perturbation of the normal, as a surface gradient.|
| **LowFrequencySurfaceGradient** | Input | Vector3 | The perturbation of the low frequency normal, as a surface gradient. The low frequency normal is the normal of the water surface without high frequency details such as ripples. |
| **SimulationFoam** | Input | Float | The amount of foam. HDRP uses this property in the default water shader graph to fetch foam data from the simulation. |
| **CustomFoam** | Input | Float | The amount of foam, if you create your own foam. |
| **SurfaceGradient** | Output | Vector3 | The calculated water surface normal, as a surface gradient. |
| **Foam** | Output | Float | The combination of the amount of foam and a foam texture. |
| **Smoothness** | Output | Float | The smoothness of the water surface. For more information about this property, see [Settings and Properties Related to the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-Properties.html).|
