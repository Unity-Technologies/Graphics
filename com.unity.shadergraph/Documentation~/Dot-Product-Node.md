# Dot Product Node

## Description

Returns the dot product, or scalar product, of the two input vectors **A** and **B**.

The dot product is a value equal to the magnitudes of the two vectors multiplied together and then multiplied by the cosine of the angle between them.

For normalized input vectors, the **Dot Product** node returns 1 if they point in exactly the same direction, -1 if they point in completely opposite directions and 0 if the vectors are perpendicular.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Float    | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_DotProduct_float4(float4 A, float4 B, out float Out)
{
    Out = dot(A, B);
}
```
