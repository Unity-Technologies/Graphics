---
uid: default-bitmap-text-node
---

# Default Bitmap Text node

[!include[](include_note_uitk.md)]

Outputs the text color set for bitmap text rendering and includes a tint input you can use to modify the color of the text. For example, if you connect a **Color** node to the tint input and set it to red, and connect the output to bitmap text render type, the text color of your bitmap text becomes red.

## Ports

| Name        | Direction           | Type  | Description |
|:----------- |:--------------------|:------|:------------|
| Tint       | Input              | Color | The color tint to apply to the text. |
| Bitmap text| Output             | Texture| The rendered bitmap of the text. |

## Additional resources

- [Default Solid node](xref:default-solid-node)
- [Default Gradient node](xref:default-gradient-node)
- [Default Texture node](xref:default-texture-node)
- [Default SDF Text node](xref:default-sdf-text-node)
- [Render Type Branch node](xref:render-type-branch-node)