# Swizzle Node

## Description

Creates a new [vector](https://docs.unity3d.com/Manual/VectorCookbook.html) from the reordered elements of the input vector. This is called swizzling.

To specify how input elements should be swizzled, enter a formatting string in the input mask.
To invert the order of the input elements, for example, use the string "wzyx" or "abgr".

The length of the input mask determines the dimensions of the output vector, while the channels used by the input mask determine the dimensions of the input vector.

For example;
* To output a vector4 that contains two copies of a vector2 input, set **Mask** to `xyxy`.
* To output the alpha value from a vector4 input, set **Mask** to `a`.



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
