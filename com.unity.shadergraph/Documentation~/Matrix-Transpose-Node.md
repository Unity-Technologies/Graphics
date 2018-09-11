## Description

Returns the transposed value of the matrix defined by input **In**. This can be seen as the operation of flipping the matrix over its diagonal. The result is that it switches the row and column indices of the matrix.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Matrix | Input value |
| Out | Output      |    Dynamic Matrix | Output value |

## Shader Function

`Out = transpose(In)`