# Swizzle Node

## Description

Creates a new [vector](https://docs.unity3d.com/Manual/VectorCookbook.html) from the reordered elements of the input vector. This is called swizzling.

To specify how input elements should be swizzled, enter a formatting string in the input mask.
To invert the order of the input elements, for example, use the string <code>wzyx</code> or <code>abgr</code>.

The length of the input mask determines the dimensions of the output vector. The error "Invalid Mask" indicates an input mask value which includes one or more channels that do not exist in the input vector.

To output a vector3 with the x, y and z elements of the input vector, for example, use the input mask “xyz” or “rgb”.


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Mask     | Inputfield | x, y, z, w (depending on input vector dimension) | The swizzle mask is a combination of one to four characters that can be x, y, z, w (or r, g, b, a). The size of output value depends on the length of the mask input.|


## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = In.wzyx;
