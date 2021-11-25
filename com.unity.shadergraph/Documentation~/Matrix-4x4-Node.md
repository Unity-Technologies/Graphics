# Matrix 4x4 Node

## Description

Defines a constant **Matrix 4x4** value in the shader.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Matrix 4 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  | Matrix 4x4 |  | Sets output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float4x4 _Matrix4x4 = float4x4(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1);
```
