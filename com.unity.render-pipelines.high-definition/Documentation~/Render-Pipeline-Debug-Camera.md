# Camera Debug Panel

In HDRP, each active camera will have its own debug panel. The purpose of this panel is to change camera [frame settings](Frame-Settings.html) on the fly without altering the camera data in the scene.

![](D:/Projects/SRP-Master/SRP/com.unity.render-pipelines.high-definition/Documentation~/Images/FrameSettings2.png)

This window of the selected Camera’s Frame Settings helps you understand why a feature may not work correctly. Here, you can access all of the information that HDRP uses to render the Camera you select.

Note: The camera debug panel is currently only accessible for Cameras and not for Reflection Probes.

| **Column**     | **Description**                                              |
| -------------- | ------------------------------------------------------------ |
| **Debug**      | Displays modifiable Frame Setting values for the selected Camera. You can use these to temporarily alter the Camera’s Frame Settings for debugging purposes. Note that you can not enable Frame Setting features that your HDRP Asset does not support. |
| **Sanitized**  | Displays the Frame Setting values that the selected Camera uses after Unity checks to see if your HDRP Asset supports them. |
| **Overridden** | Displays the Frame Setting values that the selected Camera overrides. If you do not check the **Custom Frame Settings** checkbox, or do check it and do not override any settings, this column is identical to the **Default** column. |
| **Default**    | Displays the default Frame Setting values in your current [HDRP Asset](HDRP-Asset.html). |

Unity processes **Sanitized**, **Overridden**, and **Default** in a specific order. First it checks the **Default** Frame Settings, then checks the selected Camera’s **Overridden** Frame Settings. Finally, it checks whether the HDRP Asset supports the selected Camera’s Frame Settings and displays that result in the **Sanitized** column.

### Interpreting the Debug Window

- In the image above, you can see that the **Light Layers** checkbox is disabled at the **Sanitized** step. This means that, although you enabled **Light Layers** in the Frame Settings this Camera uses, you did not enable it in your HDRP Asset’s **Render Pipeline Supported Features**.
- Also in the image above, you can also see that the **Decals** checkbox is disabled at the **Overridden** step. This means that you enabled **Decals** in the default Camera Frame Settings and then disabled **Decals** for that specific Camera’s **Custom Frame Settings**.