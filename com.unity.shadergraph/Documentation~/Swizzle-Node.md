# Swizzle Node

Swizzling allows you to create a new vector based on rearranging or combining the channels from an existing vector. 

## Description

Reorders the elements of the input vector as you specify in a formatting string in the input mask. For example, "wzyx" and "abgr" both invert the order of the input elements. The length of the input mask also determines the dimensions of the output vector. To output a vector3 with the x, y and z elements of the input vector, use the input mask “xyz”.

The error "Invalid Mask" indicates an input mask value which includes one or more channels that do not exist in the input vector.


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In  | Input | Dynamic Vector | None | Input value |
| Out | Output  | Vector4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mask     | Inputfield | x, y, z, w (depending on input vector dimension) | The swizzle mask is a combination of one to four characters that can be x, y, z, w (or r, g, b, a). The size of output value depends on the length of the mask input.|


## Generated code example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = In.wzyx;
```
