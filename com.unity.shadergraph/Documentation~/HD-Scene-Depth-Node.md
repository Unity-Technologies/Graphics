# HD Scene Depth Node

## Description

The HD Scene Depth node uses a UV input to access the current Camera's depth buffer. Unity expects normalized screen coordinates for this value. You can also use this node to access the mipmaps in the depth buffer.

You can only use the HD Scene Depth node in the Fragment Shader Stage and with non-opaque materials.

## Render pipeline compatibility

| **Node**           | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| ------------------ | ----------------------------------- | ------------------------------------------ |
| **HD Scene Color** | No                                  | Yes                                        |

## Ports

| **Name**   | **Direction** | **Type** | **Binding**     | **Description**                                              |
| ---------- | ------------- | -------- | --------------- | ------------------------------------------------------------ |
| **UV**     | Input         | Vector 4 | Screen Position | Sets the normalized screen coordinates to sample.            |
| **Lod**    | Input         | float    | None            | Sets the mip level that the sampler uses to sample the depth buffer. |
| **Output** | Output        | Vector 3 | None            | Output value.                                                |

## Depth Sampling modes

| Name         | Description                        |
| ------------ | ---------------------------------- |
| **Linear01** | Linear depth value between 0 and 1 |
| **Raw**      | Raw depth value                    |
| **Eye**      | Depth converted to eye space units |

## Notes

To use the HD Scene Depth node in a Custom Render Pipeline, you need to explicitly define its behavior, otherwise it returns white.
