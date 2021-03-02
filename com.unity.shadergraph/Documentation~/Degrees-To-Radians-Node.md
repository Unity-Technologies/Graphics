# Degrees To Radians Node

## Description

Returns the value of input **In** converted from degrees to radians.

One degree is equivalent to approximately 0.0174533 radians and a full rotation of 360 degrees is equal to 2 Pi radians.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_DegreesToRadians_float4(float4 In, out float4 Out)
{
    Out = radians(In);
}
```
