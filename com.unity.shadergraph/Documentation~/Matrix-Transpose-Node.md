# Matrix Transpose

## Description

Returns the transposed value of the matrix defined by input **In**. This can be seen as the operation of flipping the matrix over its diagonal. The result is that it switches the row and column indices of the matrix.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Matrix | Input value |
| Out | Output      |    Dynamic Matrix | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_MatrixTranspose_float4x4(float4x4 In, out float4x4 Out)
{
    Out = transpose(In);
}
```