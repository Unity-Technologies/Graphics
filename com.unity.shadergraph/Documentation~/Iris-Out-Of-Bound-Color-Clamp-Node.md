# Iris Out of Bound Color Clamp Node

Clamps the color of the Iris to a given color. This is useful in case the refraction ray reaches the inside of the cornea.

## Render pipeline compatibility

| **Node**                               | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------------------------- | ----------------------------------- | ------------------------------------------ |
| **Iris Out of Bound Color Clamp Node** | No                                  | Yes                                        |

## Ports

| name            | **Direction** | type    | description                                                  |
| --------------- | ------------- | ------- | ------------------------------------------------------------ |
| **IrisUV**      | Input         | Vector2 | Normalized UV coordinates to sample either a texture or procedurally generate an Iris Texture. |
| **Iris Color**  | Input         | Color   | Previously sampled or generated color of the Iris.           |
| **Clamp Color** | Input         | Color   | The color to clamp the Iris to.                              |
| **Iris Color**  | Output        | Color   | Result Iris color for the rest of the pipeline.              |
