## Description

Returns true if any of the components of the input **In** is not a number (NaN). This is useful for [Branching](Branch-Node.md).

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Boolean | None | Output value |

## Shader Function

`Out = (In < 0.0 || In > 0.0 || In == 0.0) ? 0 : 1;`