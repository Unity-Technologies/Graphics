# Slider Node

## Description

Defines a constant **Float** value in the shader using a **Slider** field. Can be converted to a **Float** type [Property](Property-Types.md) with a **Mode** setting of **Slider** via the [Node's](Node.md) context menu.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Float    | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|       | Slider |  | Defines the output value. |
| Min   | Float    |  | Defines the slider parameter's minimum value. |
| Max   | Float    |  | Defines the slider parameter's maximum value. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Slider_Out = 1.0;
```
