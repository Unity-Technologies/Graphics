# Tangent Node

## Description

Returns the tangent of the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Tangent_float4(float4 In, out float4 Out)
{
    Out = tan(In);
}
```