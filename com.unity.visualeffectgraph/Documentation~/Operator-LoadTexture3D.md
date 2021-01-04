# Load Texture3D

Menu Path : **Operator > Sampling > Load Texture3D** 

The **Load Texture3D** Operator allows you to read a Texture3D’s texel value for specified coordinates and mip level. The float4 texel value is returned without any filtering.

This translates to a **Load** call on the Texture in HLSL.

## Operator properties

| **Input**     | **Type**  | **Description**                        |
| ------------- | --------- | -------------------------------------- |
| **Texture**   | Texture3D | The Texture to read from.              |
| **X**         | uint      | The X coordinate of the texel to read. |
| **Y**         | uint      | The Y coordinate of the texel to read. |
| **Z**         | uint      | The Z coordinate of the texel to read. |
| **Mip Level** | uint      | The mip level to read from.            |

| **Output** | **Type** | **Description**        |
| ---------- | -------- | ---------------------- |
| **s**      | Vector4  | The value of the texel |

## Limitations

This is a GPU only operator and therefore does not work when plugged into **Spawn Context** ports.