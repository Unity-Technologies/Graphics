# Hyperbolic Sine Node

## Description

Returns the hyperbolic sine of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_HyperbolicSine_float4(float4 In, out float4 Out)
{
    Out = sinh(In);
}
```