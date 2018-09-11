## Description

Flips the individual channels of input **In** selected by the node's parameters. Positive values become negative values and vice versa.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Red      | Toggle | True, False | If true red channel will be flipped. |
| Green      | Toggle | True, False | If true green channel will be flipped. Disabled if **In** is Vector 1. |
| Blue      | Toggle | True, False | If true blue channel will be flipped. Disabled if **In** is Vector 2 or smaller. |
| Alpha      | Toggle | True, False | If true alpha channel will be flipped. Disabled if **In** is Vector 3 or smaller. |