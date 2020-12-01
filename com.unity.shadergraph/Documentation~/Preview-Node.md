# Preview Node

## Description

Provides a preview window and passes the input value through without modification. This [Node](Node.md) is useful for providing a preview at a specific point in a [Shader Graph](Shader-Graph.md) if the user prefers to generally collapse [Node](Node.md) previews.

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
