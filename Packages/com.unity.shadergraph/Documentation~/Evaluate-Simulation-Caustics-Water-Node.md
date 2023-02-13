# Evaluate Simulation Caustics

This node calculates [water caustics](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem-caustics.html).

This node outputs caustics as monochrome in the red channel. If you connect the output of this node to a **Base Color** block, all the channels are red. To prevent this, split the output and use only the red channel. You can't apply a tint to caustics.

Caustics don't have an effect above the water unless you script this behavior. For example:

- If your scene contains a boat that sits in water, HDRP doesn't project caustics on the part of the boat's hull that's above water.
- A swimming pool inside a room doesn't bounce caustics off the walls or ceiling.

The High Definition Render Pipeline (HDRP) uses this node in the default water shader graph. See the HDRP documentation for more information about [the Water System](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@14.0/manual/WaterSystem.html).

## Render pipeline compatibility

| **Node**               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ---------------------- | ----------------------------------- | ------------------------------------------ |
| **Evaluate Simulation Caustics** | No                                  | Yes                                        |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **RefractedPositionWS** | Input | Vector3 | The refracted position of the water bed you observe through the water, in world space. |
| **DistortedWaterNDC** | Input | Vector2 | The screen space position of the refracted point. |
| **Caustics** | Output | Float | The intensity of the caustics. |