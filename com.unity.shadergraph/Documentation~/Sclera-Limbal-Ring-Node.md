# Sclera Limbal Ring Node

Calculates the intensity of the Sclera ring, a darkening feature of eyes.

## Render pipeline compatibility

| **Node**                    | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| --------------------------- | ----------------------------------- | ------------------------------------------ |
| **Sclera Limbal Ring Node** | No                                  | Yes                                        |

## Ports

| name                       | **Direction** | type    | description                                                  |
| -------------------------- | ------------- | ------- | ------------------------------------------------------------ |
| **PositionOS**             | Input         | Vector3 | Position in object space of the current fragment to shade.   |
| **View Direction OS**      | Input         | Vector3 | Direction of the incident ray in object space. Either from the camera in rasterization or from the previous bounce in ray tracing. |
| **IrisRadius**             | Input         | float   | The radius of the Iris in the used model. For the default model, this value should be **0.225**. |
| **LimbalRingSize**         | Input         | float   | Normalized [0, 1] value that defines the relative size of the limbal ring. |
| **LimbalRingFade**         | Input         | float   | Normalized [0, 1] value that defines strength of the fade out of the limbal ring.** |
| **LimbalRing Intensity**   | Input         | float   | Positive value that defines how dark the limbal ring is.     |
| **Iris Limbal Ring Color** | Output        | Color   | Intensity of the limbal ring (blackscale).                   |
