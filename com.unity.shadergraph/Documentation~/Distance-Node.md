# Distance Node

## Description

Returns the euclidean distance between the values of the inputs **A** and **B**. This is useful for, among other things, calculating the distance between two points in space and is commonly used in calculating a [Signed Distance Function](https://en.wikipedia.org/wiki/Signed_distance_function).

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| A      | Input | Dynamic Vector | First input value |
| B      | Input | Dynamic Vector | Second input value |
| Out | Output      |   Float    | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Distance_float4(float4 A, float4 B, out float Out)
{
    Out = distance(A, B);
}
```
