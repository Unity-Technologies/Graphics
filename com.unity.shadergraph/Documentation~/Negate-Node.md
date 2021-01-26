# Negate Node

## Description

Returns the flipped sign value of input **In**. Positive values become negative and negative values become positive.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Negate_float4(float4 In, out float4 Out)
{
    Out = -1 * In;
}
```
