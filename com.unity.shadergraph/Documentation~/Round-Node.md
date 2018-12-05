# Round Node

## Description

Returns the value of input **In** rounded to the nearest integer, or whole number. 

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Round_float4(float4 In, out float4 Out)
{
    Out = round(In);
}
```