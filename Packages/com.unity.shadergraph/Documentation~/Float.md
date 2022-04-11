# Float Node

## Description

Defines a **Float** value in the shader. If [Port](Port.md) **X** is not connected with an [Edge](Edge.md) this [Node](Node.md) defines a constant **Float**.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| X      | Input | Float    | None | Input x component value |
| Out | Output      |    Float    | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Vector1_Out = X;
```
