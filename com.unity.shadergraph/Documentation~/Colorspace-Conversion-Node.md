## Description

Returns the result of converting the value of input **In** from one colorspace space to another. The spaces to transform from and to are defined the values of the dropdowns on the node.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Vector 3 | Input value |
| Out | Output      |   Vector 3 | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| From      | Dropdown | RGB, Linear, HSV | Selects the colorspace to convert from |
| To      | Dropdown | RGB, Linear, HSV | Selects the colorspace to convert to |