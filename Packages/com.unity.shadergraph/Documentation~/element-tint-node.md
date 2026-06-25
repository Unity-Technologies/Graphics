---
uid: element-tint-node
---

# Element Tint node

[!include[](include_note_uitk.md)]

Outputs the tint color of a UI element: its vertex color combined with the element's dynamic color.

UI Toolkit can override the color of an element at runtime, for example when you set UsageHints.DynamicColor. This override isn't part of the raw vertex color, so reading the vertex color directly doesn't reflect it. Use this node to get the color the element actually renders with.

The tint does not include the element's opacity. To read the opacity, use the [Element Opacity](xref:element-opacity-node) node.

You can use this node in both the vertex and fragment stages.

## Ports

| Name  | Direction | Type    | Description                                                          |
|-------|-----------|---------|---------------------------------------------------------------------|
| Tint  | Output    | Vector4 | The element's tint color (vertex color combined with the dynamic color), as RGBA. |

## Additional resources

- [Element Opacity node](xref:element-opacity-node)
- [Element Texture UV node](xref:element-texture-uv-node)
