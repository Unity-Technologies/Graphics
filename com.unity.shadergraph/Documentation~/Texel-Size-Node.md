# Texel Size Node

## Description

Returns the **Width** and **Height** of the texel size of **Texture 2D** input. Uses the built in variable `{texturename}_TexelSize` to access special properties of a **Texture 2D**.

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading to version 10.3 or later.

**Note:** Do not use the default input to reference your **Texture 2D**. It makes your graph perform worse. Instead connect this node to a separate [Texture 2D Asset Node](Texture-2D-Asset-Node.md) and re-use this definition for sampling.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture | None | Texture asset |
| Width      | Output | Float    | None | Texel width |
| Height | Output      |    Float    | None | Texel height |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _TexelSize_Width = Texture_TexelSize.z;
float _TexelSize_Height = Texture_TexelSize.w;
```
