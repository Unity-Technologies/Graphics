---
uid: element-opacity-node
---

# Element Opacity node

[!include[](include_note_uitk.md)]

Outputs the opacity of a UI element.

UI Toolkit fades elements with a per-element opacity that isn't part of the raw vertex data. Use this node to read that opacity.

By default, the shader applies the element's opacity automatically at the end of the fragment stage. If you turn off **Automatic Opacity** in the Graph Settings, the shader no longer applies it, and you can use this node's output to apply it yourself.

You can use this node in both the vertex and fragment stages.

## Ports

| Name    | Direction | Type    | Description                |
|---------|-----------|---------|----------------------------|
| Opacity | Output    | Vector1 | The element's opacity.     |

## Additional resources

- [Element Tint node](xref:element-tint-node)
- [Element Texture UV node](xref:element-texture-uv-node)
