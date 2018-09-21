## Description

Adjusts the contrast of input **In** by the amount of input **Contrast**. A **Contrast** value of 1 will return the input unaltered. A **Contrast** value of 0 will return the midpoint of the input.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Contrast      | Input | Vector 1 | None | Contrast value |
| Out | Output      |    Vector 3 | None | Output value |

## Shader Function

```
float midpoint = pow(0.5, 2.2);
Out =  (In - midpoint) * Contrast + midpoint;
```