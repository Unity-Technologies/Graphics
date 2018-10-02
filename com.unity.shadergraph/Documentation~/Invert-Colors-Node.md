## Description

Inverts the colors of input **In** on a per channel basis. This node assumes all input values are in the range 0 - 1.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red      | Toggle | True, False | If true red channel is inverted |
| Green     | Toggle | True, False | If true green channel is inverted. Disabled if input vector dimension is less than 2 |
| Blue     | Toggle | True, False | If true blue channel is inverted. Disabled if input vector dimension is less than 3 |
| Alpha     | Toggle | True, False | If true alpha channel is inverted. Disabled if input vector dimension is less than 4 |