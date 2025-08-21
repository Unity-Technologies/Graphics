# Exposure node

The Exposure node outputs the exposure value of the current camera. You can output the value from the current frame or the previous frame.

## Render pipeline compatibility

The Exposure node is compatible only with the High Definition Render Pipeline (HDRP). 

For more information about exposure, refer to [Control exposure](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Exposure.html) in the HDRP manual.

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|--- | --- | --- | --- |
| **Output** | Output | Float | The exposure value of the camera. |

## Type

Use the **Type** property to select which exposure value to get. The options are:

- **Current Multiplier**: Gets the camera's exposure value from the current frame.
- **Inverse Current Multiplier**: Gets the inverse of the camera's exposure value from the current frame.
- **Previous Multiplier**: Gets the camera's exposure value from the previous frame.
- **Inverse Previous Multiplier**: Gets the inverse of the camera's exposure value from the previous frame.
