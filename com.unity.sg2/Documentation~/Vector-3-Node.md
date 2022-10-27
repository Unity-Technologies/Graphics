# Vector 3 Node

## Description

Defines a **Vector 3** value in the shader. If [Ports](Port.md) **X**, **Y** and **Z** are not connected with [Edges](Edge.md) this [Node](Node.md) defines a constant **Vector 3**, otherwise this [Node](Node.md) can be used to combine various **Float** values.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| X      | Input | Float    | None | Input x component value |
| Y      | Input | Float    | None | Input y component value |
| Z      | Input | Float    | None | Input z component value |
| Out | Output      |    Vector 3 | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3 _Vector3_Out = float3(X, Y, Z);
```
