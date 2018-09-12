## Description

Returns the **Width** and **Height** of the texel size of **Texture** input. Uses the built in variable `{texturename}_TexelSize` to access special properties of a `Texture 2D Asset`.

**Note:** Do not use the default input to reference your Texture 2D Asset. It makes your graph perform worse. Connect this node to a separate Texture 2D Asset node per image example.

## Ports

| Name        | Direction           | Type  | Binding | Description |
|:------------ |:-------------|:-----|:---|:---|
| Texture      | Input | Texture | None | Texture asset |
| Width      | Output | Vector 1 | None | Texel width |
| Height | Output      |    Vector 1 | None | Texel height |

