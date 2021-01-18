# Load Texture3D

Menu Path : **Operator > Sampling > Load Texture3D** 

The **Load Texture3D** Operator allows you to read a Texture3D’s texel value for specified coordinates and mip level. The float4 texel value is returned without any filtering.

This translates to a Load() call on the texture in High-Level Shading Language (HLSL). For information on the differences between loading and sampling, see [Loading and sampling](#loading-and-sampling).

[!include[](Snippets/Operator-LoadingAndSampling.md)]

## Operator properties

| **Input**     | **Type**  | **Description**                                              |
| ------------- | --------- | ------------------------------------------------------------ |
| **Texture**   | Texture3D | The Texture to read from.                                    |
| **X**         | uint      | The X coordinate of the texel to read. This is in the range of 0 to the width of the texture minus 1. |
| **Y**         | uint      | The Y coordinate of the texel to read. This is in the range of 0 to the height of the texture minus 1. |
| **Z**         | uint      | The Z coordinate of the texel to read. This is in the range of 0 to the depth of the texture minus 1. |
| **Mip Level** | uint      | The mip level to read from.                                  |

| **Output** | **Type** | **Description**        |
| ---------- | -------- | ---------------------- |
| **s**      | Vector4  | The value of the texel |

## Limitations

This is a GPU only operator and therefore does not work when plugged into **Spawn Context** ports.