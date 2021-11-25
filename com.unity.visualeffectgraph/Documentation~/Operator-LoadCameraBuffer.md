# Load CameraBuffer

Menu Path : **Operator > Sampling > Load CameraBuffer**

The **Load CameraBuffer** Operator allows you to read a CameraBuffer texel value for specified coordinates. This Operator returns the float4 texel value without any filtering.

This translates to a **Load** call on the Texture in High-Level Shading Language (HLSL).

## Operator properties

| **Input**        | **Type**                             | **Description**                        |
| ---------------- | ------------------------------------ | -------------------------------------- |
| **CameraBuffer** | [CameraBuffer](Type-CameraBuffer.md) | The CameraBuffer to read from.         |
| **X**            | uint                                 | The X coordinate of the texel to read. |
| **Y**            | uint                                 | The Y coordinate of the texel to read. |

| **Output** | **Type** | **Description**         |
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  | The value of the texel. |

## Limitations

This is a GPU only Operator and therefore does not work when plugged into **Spawn Context** ports.
