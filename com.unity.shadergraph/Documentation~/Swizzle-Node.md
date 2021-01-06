# Swizzle Node

## Description

Reorders the elements of the input vector as you specify in the input mask. The length of the input mask determines the dimensions of the output vector.

The error "Invalid Mask" indicates an input mask value which includes one or more channels that do not exist in the input vector.


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mask     | Inputfield | x, y, z, w, r, g, b, a (depending on input vector dimension) | The swizzle mask is a combination of one to eight characters that can be x, y, z, w, r, g, b or a. The size of output value depends on the length of the mask input.|


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = In.xyzw;
