# Evaluate Refraction Data

This node calculates water refraction.

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Refraction Data** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **NormalWS** | Input | Vector3 | The water surface normal in world space. |
| **LowFrequencyNormalWS** | Input | Vector3 | The low frequency normal of the water surface in world space. This is the normal of the water surface without high frequency details such as ripples. |
| **RefractedPositionWS** | Output | Vector3 | The refracted position of the water bed you observe through the water, in world space. |
| **DistortedWaterNDC** | Output | Vector2 | The screen space position of the refracted point. |
| **AbsorptionTint** | Output | Vector3 | An absorption factor that HDRP uses to blend between the water surface and the refracted underwater color. |
