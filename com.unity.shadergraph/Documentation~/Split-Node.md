# Split Node

## Description

Splits the input vector **In** into four **Float** outputs **R**, **G**, **B** and **A**. These output vectors are defined by the individual channels of the input **In**; red, green, blue and alpha respectively. If the input vector **In**'s dimension is less than 4 (**Vector 4**) the output values not present in the input will be 0.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| R | Output      |    Float    | None | Red channel from input |
| G | Output      |    Float    | None | Green channel from input |
| B | Output      |    Float    | None | Blue channel from input |
| A | Output      |    Float    | None | Alpha channel from input |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Split_R = In[0];
float _Split_G = In[1];
float _Split_B = 0;
float _Split_A = 0;
```
