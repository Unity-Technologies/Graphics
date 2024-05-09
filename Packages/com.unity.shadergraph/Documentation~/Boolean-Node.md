# Boolean Node

## Description

Defines a constant **Boolean** value in the [Shader Graph](index.md), although internally to the shader this is treated as a constant **float** value that is ether 0 or 1, similar to Shaderlab's [Toggle](https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html) property. Can be converted to a **Boolean** type [Property](Property-Types.md) via the [Node's](Node.md) context menu.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Boolean | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|       | Toggle |  | Defines the output value. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _Boolean = 1;
```
