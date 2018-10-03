## Description

Returns the length of input **In**. This is also known as magnitude. A vector's length is calculated with <a href=https://en.wikipedia.org/wiki/Pythagorean_theorem>Pythagorean Theorum</a>.

The length of a **Vector 3** can be calculated as:

![](https://github.com/Unity-Technologies/ShaderGraph/wiki/Images/NodeLibrary/Nodes/PageImages/LengthNodePage03.png)

Where *x*, *y* and *z* are the components of the input vector. Length can be calculated for other dimension vectors by adding or removing components.

And so on.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |   Vector 1 | Output value |

## Shader Function

`Out = length(In)`