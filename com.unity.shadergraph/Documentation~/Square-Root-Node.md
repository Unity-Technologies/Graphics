# Square Root Node

## Description

Returns the square root of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_SquareRoot_float4(float4 In, out float4 Out)
{
    Out = sqrt(In);
}
```