# Load Texture2DArray

Menu Path : **Operator > Sampling > Load Texture2DArray** 

The **Load Texture2DArray** Operator reads a Texture2DArray’s texel value for specified coordinates and slice. This Operator returns the float4 texel value without any filtering.

This translates to a Load() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Input**     | **Type**       | **Description**                                              |
| ------------- | -------------- | ------------------------------------------------------------ |
| **Texture**   | Texture2DArray | The texture array this Operator loads from.                  |
| **X**         | uint           | The X coordinate of the texel to read. This is in the range of 0 to the width of the texture minus 1. |
| **Y**         | uint           | The Y coordinate of the texel to read. This is in the range of 0 to the height of the texture minus 1. |
| **Z**         | uint           | The slice to read from.                                      |
| **Mip Level** | uint           | The mip level this Operator reads from.                      |

| **Output** | **Type** | **Description**         |
| ---------- | -------- | ----------------------- |
| **s**      | Vector4  | The value of the texel. |

## Limitations

This Operator only runs on the GPU, therefore it does not work when plugged into **Spawner context** ports.