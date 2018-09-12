## Description

Returns the input **In** clamped between the minimum and maximum values defined by inputs **Min** and **Max** respectively.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Min      | Input | Dynamic Vector | Minimum value |
| Max      | Input | Dynamic Vector | Maximum value |
| Out | Output      |    Dynamic Vector | Output value |

## Shader Function

`Out = clamp(In, Min, Max)`