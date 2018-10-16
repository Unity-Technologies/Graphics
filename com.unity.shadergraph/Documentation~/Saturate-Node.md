# Saturate Node

## Description

Returns the value of input **In** clamped between 0 and 1.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Saturate_float4(float4 In, out float4 Out)
{
    Out = saturate(In);
}
```