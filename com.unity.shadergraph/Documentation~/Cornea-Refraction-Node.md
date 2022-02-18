# Cornea Refraction Node

This node performs the refraction of the view ray in object space and returns the object space position that results. This is used to simulate the refraction that can be seen when looking at an eye.

## Render pipeline compatibility

| **Node**                   | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------------- | ----------------------------------- | ------------------------------------------ |
| **Cornea Refraction Node** | No                                  | Yes                                        |

## Ports

| name                    | **Direction** | type    | description                                                  |
| ----------------------- | ------------- | ------- | ------------------------------------------------------------ |
| **Position OS**         | Input         | Vector3 | Position of the fragment to shade in object space.           |
| **View Direction OS**   | Input         | Vector3 | Direction of the incident ray in object space. Either from the camera in rasterization or from the previous bounce in ray tracing. |
| **Cornea Normal OS**    | Input         | Vector3 | The normal of the eye surface in object space.               |
| **Cornea IOR**          | Input         | float   | The index of refraction of the eye (**1.333** by default).   |
| **Iris Plane Offset**   | Input         | float   | Distance between the end of the cornea and the iris plane. For the default model, this value should be **0.02** |
| **RefractedPositionOS** | Output        | Vector3 | Position of the refracted point on the iris plane in object space. |
