# Matrix Construction Node

## Description

Constructs square matrices from the four input vectors **M0**, **M1**, **M2** and **M3**. This node can be used to generate matrices of types **Matrix 2x2**, **Matrix 3x3** and **Matrix 4x4**.

The dropdown on the node can be used to select whether the inputs values specify the matrix rows or columns.

* **Row** : Input vectors specify matrix rows from top to bottom.
* **Column** : Input vectors specify matrix columns from left to right.

Matrix outputs are taken from the top left corner of the construction of the inputs. This can be used to generate different dimension square matrices from different dimension vectors.

For example, connecting **Vector 2** type values to inputs **M0** and **M1** will generate the desired matrix from the output **2x2**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| M0      | Input | Vector 4 | First row or column |
| M1      | Input | Vector 4 | Second row or column |
| M2      | Input | Vector 4 | Third row or column |
| M3      | Input | Vector 4 | Fourth row or column |
| 4x4 | Output      |    Matrix 4x4 | Output as Matrix 4x4 |
| 3x3 | Output      |    Matrix 3x3 | Output as Matrix 3x3 |
| 2x2 | Output      |    Matrix 2x2 | Output as Matrix 2x2 |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Dropdown | Row, Column | Selects how the output matrix should be filled |

## Generated Code Example

The following example code represents one possible outcome of this node per mode.

**Row**

```
void Unity_MatrixConstruction_Row_float(float4 M0, float4 M1, float4 M2, float3 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
{
    Out4x4 = float4x4(M0.x, M0.y, M0.z, M0.w, M1.x, M1.y, M1.z, M1.w, M2.x, M2.y, M2.z, M2.w, M3.x, M3.y, M3.z, M3.w);
    Out3x3 = float3x3(M0.x, M0.y, M0.z, M1.x, M1.y, M1.z, M2.x, M2.y, M2.z);
    Out2x2 = float2x2(M0.x, M0.y, M1.x, M1.y);
}
```

**Column**

```
void Unity_MatrixConstruction_Column_float(float4 M0, float4 M1, float4 M2, float3 M3, out float4x4 Out4x4, out float3x3 Out3x3, out float2x2 Out2x2)
{
    Out4x4 = float4x4(M0.x, M1.x, M2.x, M3.x, M0.y, M1.y, M2.y, M3.y, M0.z, M1.z, M2.z, M3.z, M0.w, M1.w, M2.w, M3.w);
    Out3x3 = float3x3(M0.x, M1.x, M2.x, M0.y, M1.y, M2.y, M0.z, M1.z, M2.z);
    Out2x2 = float2x2(M0.x, M1.x, M0.y, M1.y);
}
```
