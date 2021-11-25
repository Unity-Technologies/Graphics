# Arctangent Node

## Description

Returns the arctangent of the value of input **In**. Each component should be within the range of -Pi/2 to Pi/2.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Arctangent_float4(float4 In, out float4 Out)
{
    Out = atan(In);
}
```
