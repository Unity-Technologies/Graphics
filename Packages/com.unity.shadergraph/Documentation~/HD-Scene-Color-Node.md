# HD Scene Color node

The HD Scene Color node samples the color buffer of the current camera, using the screen space coordinates you input. The node works the same way as the [Scene Color Node](Scene-Color-Node.md) but returns the mipmap levels of the color buffer.

To make sure the HD Scene Color node outputs the correct values, follow these steps:

1. Connect the node to the fragment [shader stage](Shader-Stage.md). The HD Scene Color node doesn't support the vertex shader stage.
2. In the **Graph Settings** tab of the [**Graph Inspector**](Internal-inspector.md) window, set **Surface Type** to **Transparent**. Otherwise, the node samples the color buffer before Unity renders all the opaque contents in the scene.

The node uses trilinear clamp mode to sample the color buffer, so it smoothly interpolates between the mipmap levels.

## Render pipeline support 

The HD Scene Color node supports the High Definition Render Pipeline (HDRP) only. If you use the node with an unsupported pipeline, it returns 0 (black).

If you use your own custom render pipeline, you must define the behavior of the node yourself. Otherwise, the node returns a value of 0 (black).

## Ports

| Name | Direction | Type | Binding | Description |
|:--|:--|:--|:--|:--|
| **UV** | Input | Vector 4 | Screen position | The normalized screen space coordinates to sample from. |
| **Lod** | Input | float | None | The mipmap level to sample. |
| **Output** | Output | Vector 3 | None | The color value from the color buffer at the coordinates and mipmap level. |

## Properties

| **Property** | **Description** |
|-|-|
| **Exposure** | Applies [exposure](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Override-Exposure.html) to the camera color. This property is disabled by default to avoid double exposure. |

## Additional resources

- [Scene Color Node](Scene-Color-Node.md) 
- [Custom pass buffers and pyramids](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/Custom-Pass-buffers-pyramids.html)
