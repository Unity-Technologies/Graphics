# Load Texture2DArray

Menu Path : **Operator > Sampling > Load Texture2DArray** 

The **Load Texture2DArray** Operator reads a Texture2DArray’s texel value for specified coordinates and slice. This Operator returns the float4 texel value without any filtering.

This translates to a Load call on the texture in High-Level Shading Language (HLSL).

## Operator properties

| **Input**     | **Type**       | **Description**                                    |
| ------------- | -------------- | -------------------------------------------------- |
| **Texture**   | Texture2DArray | The texture array this Operator loads from.        |
| **X**         | uint           | The X coordinate of the texel this Operator reads. |
| **Y**         | uint           | The Y coordinate of the texel this Operator reads. |
| **Z**         | uint           | The llice this Operator reads from.                |
| **Mip Level** | uint           | The mip level this Operator reads from.            |

| **Output** | **Type** | **Description**         |
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  | The value of the texel. |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.