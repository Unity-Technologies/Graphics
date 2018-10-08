# Vector 2 Node

## Description

Defines a **Vector 2** value in the shader. If [Ports](Port.md) **X** and **Y** are not connected with [Edges](Edge.md) this [Node](Node.md) defines a constant **Vector 2**, otherwise this [Node](Node.md) can be used to combine various **Vector 1** values.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| X      | Input | Vector 1 | None | Input x component value |
| Y      | Input | Vector 1 | None | Input y component value |
| Out | Output      |    Vector 2 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float2 _Vector2_Out = float2(X, Y);
```