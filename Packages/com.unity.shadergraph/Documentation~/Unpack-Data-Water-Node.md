# Unpack Water Data

This node unpacks and outputs water properties for the fragment context.

See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Unpack Water Data** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **LowFrequencyHeight** |  Output | Float | The vertical displacement of the water surface. This doesn't include ripples. |
| **HorizontalDisplacement** |  Output | Float | The horizontal displacement of the water surface. |
| **SSSMask** | Output | Float | A mask that defines where the water surface has subsurface scattering. |

