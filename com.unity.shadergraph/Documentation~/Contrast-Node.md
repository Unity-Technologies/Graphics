# Contrast Node

## Description

Adjusts the contrast of input **In** by the amount of input **Contrast**. A **Contrast** value of 1 will return the input unaltered. A **Contrast** value of 0 will return the midpoint of the input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Contrast      | Input | Float    | None | Contrast value |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Contrast_float(float3 In, float Contrast, out float3 Out)
{
    float midpoint = pow(0.5, 2.2);
    Out = (In - midpoint) * Contrast + midpoint;
}
```