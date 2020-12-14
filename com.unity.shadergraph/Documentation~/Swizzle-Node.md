# Swizzle Node

## Description

Creates a new vector using the same elements as the input vector (the dimension of the output vector depends on the input mask length). The channels of the output vector are the same as the input vector but re-ordered according to the input mask value on the node. This is called swizzling.

The dimension of the output vector depends on the length of input mask value. Input mask value that contains channel(s) not exist in the input vector will be flagged as "Invalid Mask".


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mask     | Inputfield | X, Y, Z, W (depending on input vector dimension) | The swizzle mask is a combination of one to four characters that can be x, y, z, or w. The size of output value depends on the length of the mask input.|


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = float4 (In.y,In.z,In.w,In.x);
