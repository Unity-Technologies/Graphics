# Invert Colors Node

## Description

Inverts the colors of input **In** on a per channel basis. This [Node](Node.md) assumes all input values are in the range 0 - 1.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red      | Toggle | True, False | If true red channel is inverted |
| Green     | Toggle | True, False | If true green channel is inverted. Disabled if input vector dimension is less than 2 |
| Blue     | Toggle | True, False | If true blue channel is inverted. Disabled if input vector dimension is less than 3 |
| Alpha     | Toggle | True, False | If true alpha channel is inverted. Disabled if input vector dimension is less than 4 |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _InvertColors_InvertColors = float4(Red, Green, Blue, Alpha);

void Unity_InvertColors_float4(float4 In, float4 InvertColors, out float4 Out)
{
    Out = abs(InvertColors - In);
}
```