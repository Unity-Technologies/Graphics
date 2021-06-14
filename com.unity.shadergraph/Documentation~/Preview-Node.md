# Preview Node

## Description

This node enables you to inspect a preview at a specific point in a [Shader Graph](Shader-Graph.md). It does not modify any input values.

By default, the Editor automatically selects a preview mode. That decision is determined by both the type of the node you are previewing and other upstream nodes.
With [Preview Mode Control](Preview-Mode-Control), you can manually select your preferred preview mode.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| In      | Input | Dynamic Vector | None | Input value |
| Out | Output      |    Dynamic Vector | None | Output value |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
void Unity_Preview_float4(float4 In, out float4 Out)
{
    Out = In;
}
```
