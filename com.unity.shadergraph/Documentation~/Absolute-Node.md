# Absolute Node

## Description

Returns the absolute value of the input **In**. Components of the input Dynamic Vector that are positive will remain positive and components that are negative will be inverted and become positive.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Absolute_float4(float4 In, out float4 Out)
{
    Out = abs(In);
}
```