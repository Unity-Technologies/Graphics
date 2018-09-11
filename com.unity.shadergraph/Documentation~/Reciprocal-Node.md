## Description

Returns the result of dividing 1 by the input **In**. This can be calculated by a fast approximation on Shader Model 5 by setting **Method** to Fast.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| In      | Input | Dynamic Vector | Input value |
| Out | Output      |    Dynamic Vector | Output value |

## Parameters

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
| Method      | Dropdown | Default, Fast | Selects the method used |

## Shader Function

**Default**

`Out = 1.0/In`

**Fast** (Requires Shader Model 5)

`Out = rcp(In)`