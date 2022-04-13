# Truncate Node

## Description

Returns the integer, or whole number, component of the value of input **In**. For example, given an input value of 1.7, this node will return the value 1.0.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Truncate_float4(float4 In, out float4 Out)
{
    Out = trunc(In);
}
```
