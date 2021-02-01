# Matrix Split Node

## Description

Splits a square matrix defined by input **In** into vectors. Output vector dimension is defined by the dimension of the input matrix.

The dropdown on the node can be used to select whether the output values are taken from the rows or columns of the input matrix.

* **Row** : Output vectors are composed of matrix rows from top to bottom.
* **Column** : Output vectors are composed of matrix columns from left to right.

An input matrix of type **Matrix 2x2** or **Matrix 3x3** will return 0 values in the rows (or columns, depending on dropdown selection) that are beyond their dimension.

For example, connecting **Matrix 2x2** type to input **In** will return the correct **Vector 2** type outputs to output slots **M0** and **M1**, leaving outputs **M2** and **M3** to return 0 values.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Matrix | Input value |
| M0 | Output      |    Dynamic Vector | First row or column |
| M1 | Output      |    Dynamic Vector | Second row or column |
| M2 | Output      |    Dynamic Vector | Third row or column |
| M3 | Output      |    Dynamic Vector | Fourth row or column |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Dropdown | Row, Column | Selects how the output vectors should be filled |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _MatrixSplit_M0 = float2(In[0].r, In[0].g);
float2 _MatrixSplit_M1 = float2(In[1].r, In[1].g);
float2 _MatrixSplit_M2 = float2(0, 0);
float2 _MatrixSplit_M3 = float2(0, 0);
```
