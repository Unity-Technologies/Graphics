# Cross Product Node

## Description

Returns the cross product of the values of the inputs **A** and **B**. The cross product of two vectors results in a third vector which is perpendicular to the two input vectors. The result's magnitude is equal to the magnitudes of the two inputs multiplied together and then multiplied by the sine of the angle between the inputs. You can determine the direction of the result vector using the "left hand rule".

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Vector 3 | First input value |
| B      | Input | Vector 3 | Second input value |
| Out | Output      |    Vector 3 | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_CrossProduct_float(float3 A, float3 B, out float3 Out)
{
    Out = cross(A, B);
}
```
