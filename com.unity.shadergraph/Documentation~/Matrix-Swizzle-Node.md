# Matrix Swizzle Node

## Description

Create a new matrix or vector using the elements from the input matrix. The dropdown on the node is used to select the output type of the matrix or the vector. 

Indices, which will be used for selecting elements from the input matrix, can be put in via the input fields on the node. The indices should be smaller than the input matrix dimension. Input fields will be greyed out according to the selected output type on the dropdown. 
The output matrix/vector will be composed of the selected elements from the input matrix. 


* **Matrix4x4** : The output will be a matrix 4x4.
* **Matrix3x3** : The output will be a matrix 3x3.
* **Matrix2x2** : The output will be a matrix 2x2.
* **Vector4** : The output will be a vector4.
* **Vector3** : The output will be a vector3.
* **Vector2** : The output will be a vector2.
* **Vector1** : The output will be a vector1.

If the indices are bigger than the input matrix dimension, the node will give an error message on the badge. 
If the indices are not digits, the input will be auto correct to default value(00). 
The input string length can only be 2. 


## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Matrix | Input value |
| Out | Output      |    Matrix 4/Matrix 3/Matrix 2/Vector4/Vector3/Vector2/Vector1 | Output value |


## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|Output Size| Dropdown | Matrix4x4, Matrix3x3, Matrix2x2, Vector4, Vector3, Vector2, Vector1  | Selects the output value size. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2x2 _MatrixSwizzle_Out = float2x2 (In[0].r,In[0].g,In[1].r,In[1].g);
```