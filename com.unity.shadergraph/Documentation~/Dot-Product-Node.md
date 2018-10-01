## Description

Returns the dot product, or scalar product, of the values of the inputs **A** and **B**. 

The dot product is a value equal to the magnitudes of the two vectors multiplied together and then multiplied by the cosine of the angle between them.

For normalized input vectors the **Dot Product** node returns 1 if they point in exactly the same direction, -1 if they point in completely opposite directions and zero if the vectors are perpendicular.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Vector 1 | Output value |

## Shader Function

`Out = dot(A, B)`