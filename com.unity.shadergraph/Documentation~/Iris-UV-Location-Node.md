# Iris UV Location Node

This node converts the object position of the cornea/iris to a UV Sampling coordinate.

## Render pipeline compatibility

| **Node**                  | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------------- | ----------------------------------- | ------------------------------------------ |
| **Iris UV Location Node** | No                                  | Yes                                        |

## Ports

| name            | **Direction** | type    | description                                                  |
| --------------- | ------------- | ------- | ------------------------------------------------------------ |
| **Position OS** | Input         | Vector3 | Position on the iris Plane in object space.                  |
| **Iris Radius** | Input         | float   | The radius of the Iris in the used model. For the default model, this value should be **0.225**. |
| **IrisUV**      | Output        | Vector2 | ormalized UV coordinates that can be used to sample either a texture or procedurally generate an Iris Texture. |