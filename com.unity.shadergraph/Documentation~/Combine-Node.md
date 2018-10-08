# Combine Node

## Description

Creates new vectors from the four inputs **R**, **G**, **B** and **A**. Output **RGBA** is a **Vector 4** composed of inputs **R**, **G**, **B** and **A**. Output **RGB** is a **Vector 3** composed of inputs **R**, **G** and **B**. Output **RG** is a **Vector 2** composed of inputs **R** and **G**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| R      | Input | Vector 1 | None | Defines red channel of output |
| G      | Input | Vector 1 | None | Defines green channel of output |
| B      | Input | Vector 1 | None | Defines blue channel of output |
| A      | Input | Vector 1 | None | Defines alpha channel of output |
| RGBA | Output      |    Vector 4 | None | Output value as **Vector 4** |
| RGB | Output      |    Vector 3 | None | Output value as **Vector 3** |
| RG | Output      |    Vector 2 | None | Output value as **Vector 2** |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
{
    RGBA = float4(R, G, B, A);
    RGB = float3(R, G, B);
    RG = float2(R, G);
}
```