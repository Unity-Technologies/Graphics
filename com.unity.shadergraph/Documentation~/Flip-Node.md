# Flip Node

## Description

Flips the individual channels of input **In** selected by the [Node](Node.md)'s parameters. Positive values become negative values and vice versa.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red      | Toggle | True, False | If true red channel will be flipped. |
| Green      | Toggle | True, False | If true green channel will be flipped. Disabled if **In** is Float. |
| Blue      | Toggle | True, False | If true blue channel will be flipped. Disabled if **In** is Vector 2 or smaller. |
| Alpha      | Toggle | True, False | If true alpha channel will be flipped. Disabled if **In** is Vector 3 or smaller. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _Flip_Flip = float4(Red, Green, Blue, Alpha);

void Unity_Flip_float4(float4 In, float4 Flip, out float4 Out)
{
    Out = (Flip * -2 + 1) * In;
}
```
