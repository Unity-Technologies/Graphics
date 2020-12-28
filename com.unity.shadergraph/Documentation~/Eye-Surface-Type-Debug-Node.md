# Eye Surface Type Debug Node

Debug node that allows you to visually validate the current pupil radius.

## Render pipeline compatibility

| **Node**                        | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------------------- | ----------------------------------- | ------------------------------------------ |
| **Eye Surface Type Debug Node** | No                                  | Yes                                        |

## Ports

| name             | **Direction** | type    | description                                                  |
| ---------------- | ------------- | ------- | ------------------------------------------------------------ |
| **PositionOS**   | Input         | Vector3 | Position in object space of the current fragment to shade.   |
| **EyeColor**     | Input         | Color   | Final Diffuse color of the Eye.                              |
| **IrisRadius**   | Input         | float   | The radius of the Iris in the used model. For the default model, this value should be **0.225**. |
| **Pupil Radius** | Input         | float   | Radius of the pupil in the iris texture as a percentage.     |
| **IsActive**     | Input         | bool    | Flag that defines if the node should be active.              |
| **SurfaceColor** | Output        | Color   | Final Diffuse color of the Eye.                              |