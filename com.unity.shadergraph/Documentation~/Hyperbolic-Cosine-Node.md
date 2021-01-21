# Hyperbolic Cosine Node

## Description

Returns the hyperbolic cosine of input **In**.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_HyperbolicCosine_float4(float4 In, out float4 Out)
{
    Out = cosh(In);
}
```
