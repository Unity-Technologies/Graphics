# Sine Node

## Description

Returns the sine of the value of input **In**.

## Ports

| Name  | Direction  | Type           | Description   |
|:------|:-----------|:---------------|:--------------|
| In    | Input      | Dynamic Vector | Input value in radians.  |
| Out   | Output     | Dynamic Vector | Output value. Range (-1 to +1).  |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Sine_float4(float4 In, out float4 Out)
{
    Out = sin(In);
}
```
