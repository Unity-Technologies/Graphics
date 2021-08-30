# Circle Pupil Animation Node

This node applies a deformation to a normalized IrisUV coordinate to simulate the opening and closure of the pupil.

## Render pipeline compatibility

| **Node**                        | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------------------- | ----------------------------------- | ------------------------------------------ |
| **Circle Pupil Animation Node** | No                                  | Yes                                        |

## Ports

| name                       | **Direction** | type    | description                                                  |
| -------------------------- | ------------- | ------- | ------------------------------------------------------------ |
| **IrisUV**                 | Input         | Vector2 | Position of the fragment to shade in object space.           |
| **Pupil Radius**           | Input         | float   | Direction of the incident ray in object space. Either from the camera in rasterization or from the previous bounce in ray tracing. |
| **Maximal Pupil Aperture** | Input         | float   | The normal of the eye surface in object space.               |
| **Minimal Pupil Aperture** | Input         | float   | The index of refraction of the eye (**1.333** by default).   |
| **Pupil Apertur**          | Input         | float   | Distance between the end of the cornea and the iris plane. For the default model, this value should be **0.02** |
| **IrisUV**                 | Output        | Vector2 | Position of the refracted point on the iris plane in object space. |
