# One Minus Node

## Description

Returns the result of input **In** subtracted from 1.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_OneMinus_float4(float4 In, out float4 Out)
{
    Out = 1 - In;
}
```
