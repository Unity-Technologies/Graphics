# Fraction Node

## Description

Returns the fractional (or decimal) part of input **In**; which is greater than or equal to 0 and less than 1.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Fraction_float4(float4 In, out float4 Out)
{
    Out = frac(In);
}
```