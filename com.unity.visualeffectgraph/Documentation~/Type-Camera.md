# Camera

A Unity Camera defined by a Transform, field of view, near-plane, far-plane, aspect ratio, resolution. You can also access the color and depth buffer.

## Properties

| **Property**         | **Description**                                              |
| -------------------- | ------------------------------------------------------------ |
| **Transform**        | The transform of the camera. This is a [Transform](Type-Transform.md) that contains the position |
| **Field Of View**    | The height of the camera's view angle (in degrees) along the local y-axis. |
| **Near Plane**       | The closest plane, relative to the camera, at which drawing occurs. |
| **Far Plane**        | The furthest plane, relative to the camera, at which drawing occurs. |
| **Aspect Ratio**     | The proportional relationship between the camera's width and height. |
| **Pixel Dimensions** | The width and height of the camera in pixels.                |
| **Depth Buffer**     | Specifies the depth buffer for this camera.                  |
| **Color Buffer**     | Specifies the color buffer for this camera.                  |