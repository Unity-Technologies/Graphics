# Iris Limbal Ring Node

Calculates the intensity of the Limbal ring, a darkening feature of eyes.

## Render pipeline compatibility

| **Node**                  | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------------- | ----------------------------------- | ------------------------------------------ |
| **Iris Limbal Ring Node** | No                                  | Yes                                        |

## Ports

| name                       | **Direction** | type    | description                                                  |
| -------------------------- | ------------- | ------- | ------------------------------------------------------------ |
| **IrisUV**                 | Input         | Vector2 | Normalized UV coordinates that can be used to sample either a texture or procedurally generate an Iris Texture. |
| **View Direction OS**      | Input         | Vector3 | Direction of the incident ray in object space. Either from the camera in rasterization or from the previous bounce in ray tracing. |
| **LimbalRingSize**         | Input         | float   | Normalized [0, 1] value that defines the relative size of the limbal ring. |
| **LimbalRingFade**         | Input         | float   | Normalized [0, 1] value that defines strength of the fade out of the limbal ring. |
| **LimbalRingIntensity**    | Input         | float   | Positive value that defines how dark the limbal ring is.     |
| **Iris Limbal Ring Color** | Output        | Color   | Intensity of the limbal ring.                                |
