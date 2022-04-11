# Vector 4 Node

## Description

Defines a **Vector 4** value in the shader. If [Ports](Port.md) **X**, **Y**, **Z** and **W** are not connected with [Edges](Edge.md) this [Node](Node.md) defines a constant **Vector 4**, otherwise this [Node](Node.md) can be used to combine various **Float** values.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| X      | Input | Float    | None | Input x component value |
| Y      | Input | Float    | None | Input y component value |
| Z      | Input | Float    | None | Input z component value |
| W      | Input | Float    | None | Input w component value |
| Out | Output      |    Vector 4 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4 _Vector4_Out = float4(X, Y, Z, W);
```
