# Swizzle Node

Swizzling allows you to create a new vector based on rearranging or combining the channels from an existing vector. 

## Description

The Swizzle node creates a new Output [vector](https://docs.unity3d.com/Manual/VectorCookbook.html) as a Vector 4, based on the channels the node receives from an Input vector. You can use the dropdowns on the node to specify which channel from the Input vector should go to a specific channel on the Output vector. 

The length of the Input vector's dimension determines the channel dropdown parameters on the Swizzle node. The Swizzle node can only output a Vector 4, and won't display any channels that aren't present on the Input vector as options in its dropdowns. 


## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In  | Input | Dynamic Vector | None | Input value |
| Out | Output  | Vector4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red Out     | Dropdown | Red, Green, Blue, Alpha (depending on input vector dimension) | Choose which channel from the Input vector you want to use for the Red channel of the Output vector. |
| Green Out | Dropdown  | Red, Green, Blue, Alpha (depending on input vector dimension) | Choose which channel from the Input vector you want to use for the Green channel of the Output vector. |
| Blue Out  | Dropdown  | Red, Green, Blue, Alpha (depending on input vector dimension) | Choose which channel from the Input vector you want to use for the Blue channel of the Output vector. |
| Alpha Out | Dropdown  | Red, Green, Blue, Alpha (depending on input vector dimension) | Choose which channel from the Input vector you want to use for the Alpha channel of the Output vector. |


## Generated code example

The following example code represents one possible outcome of this node.

```
float4 _Swizzle_Out = In.wzyx;
```