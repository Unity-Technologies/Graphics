## Description

Adjusts the strength of the normal map defined by input **In** by the amount of input **Strength**. A **Strength** value of 1 will return the input unaltered. A **Strength** value of 0 will return a blank normal map.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Vector 3 | None | Input value |
| Strength      | Input | Vector 1 | None | Strength value |
| Out | Output      |    Vector 3 | None | Output value |

## Shader Function

```
Out = {precision}3(In.rg * Strength, In.b);
```