# Radians To Degrees Node

## Description

Returns the value of input **In** converted from radians to degrees. One radian is equivalent to approximately 57.2958 degrees and a full rotation of 2 Pi radians is equal to 360 degrees.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_RadiansToDegrees_float4(float4 In, out float4 Out)
{
    Out = degrees(In);
}
```