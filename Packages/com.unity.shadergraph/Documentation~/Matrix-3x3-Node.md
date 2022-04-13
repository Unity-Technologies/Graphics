# Matrix 3x3 Node

## Description

Defines a constant **Matrix 3x3** value in the shader.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Out | Output      |    Matrix 3 | None | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|  | Matrix 3x3 |  | Sets output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float3x3 _Matrix3x3 = float3x3(1, 0, 0, 0, 1, 0, 0, 0, 1);
```
