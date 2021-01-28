# Main Camera

Menu Path : **Operator > BuiltIn > Main Camera**

The Main Camera Operator returns available information from the current main camera. It uses an equivalent behavior to [Camera.main](https://docs.unity3d.com/ScriptReference/Camera-main.html) and an integration into the active Scriptable Render Pipeline for depth buffer and color buffer access.

## Operator properties

| **Output** | **Type**                 | **Description**                    |
| ---------- | ------------------------ | ---------------------------------- |
| **o**      | [Camera](Type-Camera.md) | Information about the main camera. |

## Remarks

If the main camera is unavailable, which can occur if:

- If there is no camera GameObject tagged as **MainCamera** within the scene.
- You manually trigger [Simulate](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VisualEffect.Simulate.html)
- It is during the prewarm update of the Visual Effect component.

If this is the case, the [Camera](Type-Camera.md) this Operator returns has the following default values:

| **Property**       | **Default value**                                           |
| ------------------ | ----------------------------------------------------------- |
| **Transform**      | Transform identity : position zero, scale one, no rotation. |
| **fieldOfView**    | 60.0                                                        |
| **nearPlane**      | 0.3                                                         |
| **farPlane**       | 1000                                                        |
| **aspectRatio**    | 1.0                                                         |
| **pixelDimension** | (1920, 1080)                                                |
| **depthBuffer**    | Default texture2D array (gray texture)                      |
| **colorBuffer**    | Default texture2D array (gray texture)                      |

The **depthBuffer** and **colorBuffer** textures are only available in a Scriptable Render Pipeline that implements [VFXManager.SetCameraBuffer](https://docs.unity3d.com/Documentation/ScriptReference/VFX.VFXManager.SetCameraBuffer.html). HDRP officially supports this integration.
