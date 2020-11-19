# Iris Offset Node

Applies an offset to the center of the Iris as real world eyes are never symmetrical and centered.

## Render pipeline compatibility

| **Node**             | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------- | ----------------------------------- | ------------------------------------------ |
| **Iris Offset Node** | No                                  | Yes                                        |

## Ports

| name           | **Direction** | type    | description                                                  |
| -------------- | ------------- | ------- | ------------------------------------------------------------ |
| **IrisUV**     | Input         | Vector2 | Normalized UV coordinates to sample either a texture or procedurally generate an Iris Texture. |
| **IrisOffset** | Input         | Vector2 | Normalized [0, 1]x[0,1] value that defines on each axis the intensity of the offset of the Center of the pupil. |
| **IrisUV**     | Output        | Vector2 | Normalized UV coordinates to sample either a texture or procedurally generate an Iris Texture. |