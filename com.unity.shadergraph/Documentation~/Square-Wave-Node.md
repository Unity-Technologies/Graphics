# Square Wave Node

## Description

Returns a square wave from the value of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SquareWave_float4(float4 In, out float4 Out)
{
    Out = 1.0 - 2.0 * round(frac(In));
}
```
