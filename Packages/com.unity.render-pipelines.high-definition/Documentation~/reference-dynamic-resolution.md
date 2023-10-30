# HD Dynamic Resolution component properties

The HD Dynamic Resolution component changes the screen resolution based on the average amount of GPU frame time between each frame over a given number of frames.
If the average frame time is different from the target frame time, HDRP changes the resolution in a series of steps.

| **Property**                  | **Description**                                              |
| ----------------------------- | ------------------------------------------------------------ |
| **Default Target Frame Rate** | The desired target frame rate in FPS. If `Application.targetFrameRate` is already set, `Application.targetFrameRate` overrides this parameter. |
| **Evaluation Frame Count**    | The number of frames HDRP takes into account to calculate GPU's average performance. HDRP uses these frames to determine if the frame time is short enough to meet the target frame rate. |
| **Scale Up Duration**         | The number of groups of evaluated frames above the target frame time that HDRP requires to increase dynamic resolution by one step.<br/><br/>To control how many frames HDRP evaluates in each group, change the **Evaluation Frame Count** value. |
| **Scale Down Duration**       | The number of groups of evaluated frames below the target frame time that HDRP requires to reduce dynamic resolution by one step.<br><br>To control how many frames HDRP evaluates in each group, change the **Evaluation Frame Count** value. |
| **Scale Up Step Count**       | The number of downscale steps between the minimum screen percentage to the maximum screen percentage. For example, a value of 5 means that each step upscales 20% of the difference between the maximum and minimum screen resolutions. <br/><br/>You can set the minimum and maximum screen percentage in the [HDRP Asset](HDRP-Asset.md). |
| **Scale Down Step Count**     | The number of downscale steps between the maximum screen percentage to the minimum screen percentage. For example, a value of 5 means that each step downscales 20% of the difference between the maximum and minimum screen resolutions. <br/><br/>You can set the minimum and maximum screen percentage in the [HDRP Asset](HDRP-Asset.md). |
| **Enable Debug View**         | Enables the debug view of dynamic resolution.                |