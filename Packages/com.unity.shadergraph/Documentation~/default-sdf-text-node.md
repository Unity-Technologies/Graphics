---
uid: default-sdf-text-node
---

# Default SDF Text node

[!include[](include_note_uitk.md)]

Outputs the text color for SDF text rendering. Each input overrides one text parameter of the element. If you leave an input disconnected, the node uses the element's own value.

For example, to render the text in red, connect a **Color** node set to red to the **Tint** input, and connect the **SDF Text** output to the **SDF Text** input of the [Render Type Branch](xref:render-type-branch-node) node.

To start with the element's current values, for example, to widen the outline or to move the shadow relative to what the style defines, use the [SDF Text Parameters](xref:sdf-text-parameters-node) node and connect its outputs to the matching inputs.

[!include[sdf-text-units](./snippets/sdf-text-units.md)]

Unity clamps the following inputs to the same limits that apply to the style values:

- **Dilate**: between `-1` and `1`.
- **Outline Dilate**: between `0` and `1`.
- **Shadow Softness**: between `0` and `1`.
- **Shadow Offset**: a length up to `1` minus the softness.

## Ports

| Name              | Direction | Type    | Description |
|-------------------|-----------|---------|-------------|
| Tint              | Input     | Vector4 | The face color of the text, as straight (not premultiplied) RGBA. Defaults to the element's tint. |
| Dilate            | Input     | Float   | Additional dilation of the glyph shape. Defaults to the value UI Toolkit applies for synthetic bold. |
| Sharpness         | Input     | Float   | The sharpness of the glyph edges. Larger values produce crisper edges. Values below `-0.99` are clamped. Defaults to the font atlas sharpness: in Editor panels, the **Editor Text Sharpness** value in [**Preferences** > **General**](xref:um-preferences-general); at runtime, `0`. |
| Outline Color     | Input     | Vector4 | The outline color, as straight RGBA. Defaults to the element's `-unity-text-outline-color`, with zero alpha when the element has no outline width. |
| Outline Dilate    | Input     | Float   | The outline width. The outline extends on both sides of the glyph edge. If this value is `0`, Unity doesn't draw an outline, regardless of the outline color. Defaults to the element's `-unity-text-outline-width`. |
| Shadow Color      | Input     | Vector4 | The shadow color, as straight RGBA. Defaults to the element's `text-shadow` color. |
| Shadow Offset     | Input     | Vector2 | The shadow offset, with x to the right and y down like the `text-shadow` offset. Defaults to the element's `text-shadow` offset. |
| Shadow Softness   | Input     | Float   | The softness of the shadow edge. Defaults to the element's `text-shadow` blur radius. |
| SDF Text          | Output    | Vector4 | The rendered SDF text color. |

## Additional resources

- [SDF Text Parameters node](xref:sdf-text-parameters-node)
- [Element Tint node](xref:element-tint-node)
- [Default Solid node](xref:default-solid-node)
- [Default Gradient node](xref:default-gradient-node)
- [Default Texture node](xref:default-texture-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)
- [Render Type Branch node](xref:render-type-branch-node)

[!include[nodes-example-samples](./snippets/nodes-example-samples.md)]
