## Description

Masks values of input **In** on channels selected in dropdown **Channels**. Outputs a vector of the same length as the input vector but with the selected channels set to 0. Channels available in the dropdown **Channels** will represent the amount of channels present in input **In**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |   Dynamic Vector | None | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Channels      | Mask Dropdown | Dynamic | Selects any number of channels to mask |