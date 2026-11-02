---
uid: sdf-text-parameters-node
---

# SDF Text Parameters node

[!include[](include_note_uitk.md)]

Outputs the text parameters that UI Toolkit resolves for the current SDF text element, including outline and shadow colors, dilate, offset, softness, and synthetic bold values from the element's styles.

Use this node to read a parameter, such as the dilate applied to bold text, and derive your own value before you connect it to the [Default SDF Text](xref:default-sdf-text-node) node. If you connect each output to its matching input, the text renders the same as it does with the default shader.

This node doesn't output the face color. To read the face color, which is the element's tint, use the [Element Tint](xref:element-tint-node) node.

[!include[sdf-text-units](./snippets/sdf-text-units.md)]

For content outside SDF text, such as on solid backgrounds or bitmap text, all outputs are zero.

Use this node in both the vertex and fragment stages.

## Ports

| Name              | Direction | Type    | Description |
|-------------------|-----------|---------|-------------|
| Dilate            | Output    | Float   | The additional dilation of the glyph shape. Positive for synthetic bold. |
| Sharpness         | Output    | Float   | The sharpness of the glyph edges, from the font atlas sharpness. |
| Outline Color     | Output    | Vector4 | The outline color, as straight (not premultiplied) RGBA, from `-unity-text-outline-color`. The alpha is zero when the outline width is zero. |
| Outline Dilate    | Output    | Float   | The outline width, from `-unity-text-outline-width`. |
| Shadow Color      | Output    | Vector4 | The shadow color, as straight RGBA, from `text-shadow`. |
| Shadow Offset     | Output    | Vector2 | The shadow offset, with x to the right and y down like the `text-shadow` offset. |
| Shadow Softness   | Output    | Float   | The softness of the shadow edge, from the `text-shadow` blur radius. |

## Additional resources

- [Default SDF Text node](xref:default-sdf-text-node)
- [Element Tint node](xref:element-tint-node)
- [Render Type Branch node](xref:render-type-branch-node)
