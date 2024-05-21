# Gradient Node

## Description

Defines a constant **Gradient** for use in [Shader Graph](index.md), although internally to the shader this is defined as a **struct**. To sample the **Gradient** it should be used in conjunction with a [Sample Gradient Node](Sample-Gradient-Node.md). When using a separate **Gradient Node**, you can sample a **Gradient** multiple times with different Time parameters.

## Ports

| Name        | Direction           | Type  | Description |
|:------------ |:-------------|:-----|:---|
| Out | Output      |    Gradient | Output value |

## Controls

| Name        | Type           | Options  | Description |
|:------------ |:-------------|:-----|:---|
|      | Gradient Field |  | Defines the gradient. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
Gradient Unity_Gradient_float()
{
    Gradient g;
    g.type = 1;
    g.colorsLength = 4;
    g.alphasLength = 4;
    g.colors[0] = 0.1;
    g.colors[1] = 0.2;
    g.colors[2] = 0.3;
    g.colors[3] = 0.4;
    g.colors[4] = 0;
    g.colors[5] = 0;
    g.colors[6] = 0;
    g.colors[7] = 0;
    g.alphas[0] = 0.1;
    g.alphas[1] = 0.2;
    g.alphas[2] = 0.3;
    g.alphas[3] = 0.4;
    g.alphas[4] = 0;
    g.alphas[5] = 0;
    g.alphas[6] = 0;
    g.alphas[7] = 0;
    return g;
}

Gradient _Gradient = Unity_Gradient_float();
```
