# Is NaN Node

## Description

Returns true if any of the components of the input **In** is not a number (NaN). This is useful for [Branching](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Boolean | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_IsNan_float4(float4 In, out float Out)
{
    Out = (In < 0.0 || In > 0.0 || In == 0.0) ? 0 : 1;
}
```