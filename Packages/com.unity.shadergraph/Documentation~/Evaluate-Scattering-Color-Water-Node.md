# Evaluate Scattering Color

This node calculates the scattered diffuse color of water.

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Scattering Color** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **AbsorptionTint** | Input | Vector3 | An absorption factor that HDRP uses to blend between the water surface and the refracted underwater color. |
| **LowFrequencyHeight** | Input | Float | The vertical displacement of the water surface. This doesn't include ripples. |
| **HorizontalDisplacement** | Input | Float | The horizontal displacement of the water surface.  |
| **SSSMask** | Input | Float | Mask that defines where the water surface has subsurface scattering. |
| **DeepFoam** | Input | Float | The amount of foam under the water's surface. |
| **ScatteringColor** | Output | Vector3 | The diffuse color of the water. |
