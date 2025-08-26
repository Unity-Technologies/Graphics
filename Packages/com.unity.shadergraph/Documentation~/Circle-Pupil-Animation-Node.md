# Circle Pupil Animation Node

This node applies a deformation to a normalized IrisUV coordinate to simulate the opening and closure of the pupil.

## Render pipeline compatibility

| **Node**                        | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------------------- | ----------------------------------- | ------------------------------------------ |
| **Circle Pupil Animation Node** | No                                  | Yes                                        |

## Ports

| name                       | **Direction** | type    | description                                                  |
| -------------------------- | ------------- | ------- | ------------------------------------------------------------ |
| **Iris UV**                 | Input         | Vector2 | Normalized UV coordinates that can be used to sample either a texture or procedurally generate an Iris Texture.           |
| **Pupil Radius**           | Input         | float   | Radius of the pupil in the iris texture as a percentage. |
| **Pupil Aperture**          | Input         | float   | Set the current diameter of the pupil opening. |
| **Maximal Pupil Aperture** | Input         | float   | Define the largest size the pupil opening can reach.               |
| **Minimal Pupil Aperture** | Input         | float   | Define the smallest size the pupil opening can reach.   |