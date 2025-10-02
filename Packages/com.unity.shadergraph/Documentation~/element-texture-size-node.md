---
uid: element-texture-size-node
---

# Element Texture Size node

[!include[](include_note_uitk.md)]

Outputs the texture size that's assigned to the UI element. The output is undefined if the render type is solid.

This node is similar to the [**Texture Size**](Texture-Size-node.md) node, but it specifically retrieves the size of the texture assigned to the UI element. This is important because UI elements can have textures assigned through styles or images, and these textures might differ from the material's main texture. For example, a button might have a background image set through its style, which is different from the material's texture.

Follow these guidelines to decide which node to use:

- Use the **Texture Size** node if you want to apply a texture-based effect that's not element-specific (for example, a soft mask). In this case, set the texture explicitly on the material and use the Texture Size node to get its size.
- Use the **Element Texture Size** node if you need the size of the texture assigned to a specific VisualElement, such as a background image or an image element. This includes textures set via styles or textures used in custom meshes drawn with `MeshGenerationContext.DrawMesh(texture)`.

## Ports

| Name               | Direction | Type    | Description                          |
|--------------------|-----------|---------|--------------------------------------|
| Width              | Output    | Float    | The width of the texture.           |
| Height             | Output    | Float    | The height of the texture.          |
| Texel Width        | Output    | Float    | The texel width of the texture.     |
| Texel Height       | Output    | Float    | The texel height of the texture.    |

## Additional resources

- [Element Texture UV node](xref:element-texture-uv-node)
- [Element Layout UV node](xref:element-layout-uv-node)
- [Texture Size node](Texture-Size-node.md)