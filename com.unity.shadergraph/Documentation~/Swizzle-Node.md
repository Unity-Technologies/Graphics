# Swizzle Node

## Description

Creates a new vector of the same dimension as the input vector. The channels of the output vector are the same as the input vector but re-ordered by the dropdown parameters on the node. This is called swizzling.

Channel dropdown parameters are dynamic depending on the length of the input vector's dimension. Dropdowns for channels that are not present will be disabled and dropdowns will only contain entries for channels that exist in the vector.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red out      | Dropdown | Red, Green, Blue, Alpha (depending on input vector dimension) | Defines which input channel should be used in the output's red channel |
| Green out      | Dropdown | Red, Green, Blue, Alpha (depending on input vector dimension) | Defines which input channel should be used in the output's green channel |
| Blue out      | Dropdown | Red, Green, Blue, Alpha (depending on input vector dimension) | Defines which input channel should be used in the output's blue channel |
| Alpha out      | Dropdown | Red, Green, Blue, Alpha (depending on input vector dimension) | Defines which input channel should be used in the output's alpha channel |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = In.xzyw;
```
