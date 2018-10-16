# Channel Mask Node

## Description

Masks values of input **In** on channels selected in dropdown **Channels**. Outputs a vector of the same length as the input vector but with the selected channels set to 0. Channels available in the dropdown **Channels** will represent the amount of channels present in input **In**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |   Dynamic Vector | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Channels      | Mask Dropdown | Dynamic | Selects any number of channels to mask |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_ChannelMask_RedGreen_float4(float4 In, out float4 Out)
{
    Out = float4(0, 0, In.b, In.a);
}
```