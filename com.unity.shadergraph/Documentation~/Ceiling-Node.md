# Ceiling Node

## Description

Returns the smallest integer value, or whole number, that is greater than or equal to the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Ceiling_float4(float4 In, out float4 Out)
{
    Out = ceil(In);
}
```
