# Camera Fade

Menu Path : **Output > Camera Fade**

The **Camera Fade** Block fades out particles when they are too close to the near plane of the camera. It calculates an interpolation of the current depth between provided its **Faded Distance** and the **Visible Distance** properties to determine the amount of fade to apply. To fade particles, this Block modifies their color and/or alpha attributes.

![](Images/Block-CameraFadeExample.gif)

If you input a **Faded Distance** that is greater than the **Visible Distance**, the result is that particles fade in as they come close to the camera, rather than fade out as they come close.

## Block compatibility

This Block is compatible with the following Contexts:

- Any output Context

## Block settings

| **Setting**         | **Type** | **Description**                                              |
| ------------------- | -------- | ------------------------------------------------------------ |
| **Cull When Faded** | Bool     | **(Inspector)** Indicates whether to cull the particle culled when it is fully faded to reduce overdraw. |
| **Fade Mode**       | Enum     | Specifies how to fade the particle out when it gets near the camera. The options are:<br/>&#8226; **Color**: Fades out the particle's color.<br/>&#8226; **Alpha**: Fades out the particle's alpha.<br/>&#8226; **Color And Alpha**: Fades out both the particle's color and its alpha. |

## Block properties

| **Input**            | **Type** | **Description**                                              |
| -------------------- | -------- | ------------------------------------------------------------ |
| **Faded Distance**   | float    | The distance from the camera at which to fully fade out the particle. |
| **Visible Distance** | float    | The distance from the camera at which to begin to fade out the particle. At this distance, the particle is fully visible. Between this distance and **Faded Distance**, the particle fades out. |
