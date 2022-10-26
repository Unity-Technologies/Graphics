# Texture Size Node

The Texture Size node takes a Texture 2D input and returns the width and height texel resolution of the texture. It also returns the width and height size of each texel of the texture. The node uses the built in variable `{texturename}_TexelSize` to access the special properties of the given Texture 2D input.

The term "texel" is short for "texture element" or "texture pixel." It represents a single pixel in the texture.  So, for example, if Texture resolution is 512x512 texels, the texture is sampled over the range [0-1] in UV space,  so each texel is 1/512 x 1/512 in size in UV coordinates.

<!-- ![](images/) Add image of node-->

If you experience texture sampling errors while using this node in a graph which includes Custom Function Nodes or Sub Graphs, you can resolve them by upgrading your version of Shader Graph to version 10.3 or later.

> [!NOTE]
> Don't use the default input to reference your **Texture 2D**, as this affects the performance of your graph. Connect a [Texture 2D Asset Node](Texture-2D-Asset-Node.md) to the Texture Size node's Texture input port and re-use this definition for sampling.

## Create Node menu category

The Texture Size node is under the **Input** &gt; **Texture** category in the Create Node menu.

## Compatibility

The Texture Size node is compatible with all render pipelines.

## Ports

| Name         | Direction | Type     | Binding | Description |
|:------------ |:----------|:---------|:--------|:------------|
| Texture      | Input     | Texture  | None    | The Texture 2D asset to measure. |
| Width        | Output    | Float    | None    | The width of the Texture 2D asset in texels. |
| Height       | Output    | Float    | None    | The height of the Texture 2D asset in texels. |
| Texel Width  | Output    | Float    | None    | The texel width of the Texture 2D asset in UV coordinates. |
| Texel Height | Output    | Float    | None    | The texel height of the Texture 2D asset in UV coordinates. |


<!-- ## Example graph usage -->

<!-- Add example usage of node -->

## Generated Code Example

The following example code represents one possible outcome of this node.

```
float _TexelSize_Width = Texture_TexelSize.z;
float _TexelSize_Height = Texture_TexelSize.w;
```
