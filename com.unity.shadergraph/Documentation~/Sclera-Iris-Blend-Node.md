# Sclera Iris Blend Node

This node blends all the properties of the Iris and the Sclera so that they can be fed to the master node.

## Render pipeline compatibility

| **Node**                   | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------------- | ----------------------------------- | ------------------------------------------ |
| **Sclera Iris Blend Node** | No                                  | Yes                                        |

## Ports

| name                         | **Direction** | type              | description                                                  |
| ---------------------------- | ------------- | ----------------- | ------------------------------------------------------------ |
| **Sclera Color**             | Input         | Color             | Color of the sclera at the target fragment.                  |
| **Sclera Normal**            | Input         | Vector3           | Normal of the sclera at the target fragment.                 |
| **Sclera Smoothness**        | Input         | float             | Smoothness of the sclera at the target fragment.             |
| **Iris Color**               | Input         | Color             | Color of the iris at the target fragment.                    |
| **Iris Normal**              | Input         | Vector3           | Normal of the iris at the target fragment.                   |
| **Cornea Smoothness**        | Input         | float             | Smoothness of the cornea at the target fragment.             |
| **IrisRadius**               | Input         | float             | The radius of the Iris in the model. For the default model, this value should be **0.225**. |
| **PositionOS**               | Input         | Vector3           | Position in object space of the current fragment to shade.   |
| **Diffusion Profile Sclera** | Input         | Diffusion Profile | Diffusion profile used to compute the subsurface scattering of the sclera. |
| **Diffusion Profile Iris**   | Input         | Diffusion Profile | Diffusion profile used to compute the subsurface scattering of the iris. |
| **EyeColor**                 | Output        | Color             | Final Diffuse color of the Eye.                              |
| **Surface Mask**             | Output        | float             | Linear, normalized value that defines where the fragment is. On the Cornea, this is 1 and on the Sclera, this is 0. |
| **Diffuse Normal**           | Output        | Vector3           | Normal of the diffuse lobes.                                 |
| **Specular Normal**          | Output        | Vector3           | Normal of the specular lobes.                                |
| **EyeSmoothness**            | Output        | float             | Final smoothness of the Eye.                                 |
| **SurfaceDiffusionProfile**  | Output        | Diffusion Profile | Diffusion profile of the target fragment.                    |
