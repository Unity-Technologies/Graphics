---
uid: default-sdf-text-node
---

# Default SDF Text node

[!include[](include_note_uitk.md)]

Outputs the text color for SDF text rendering and includes a tint input you can use to modify the color of the text. For example, if you connect a **Color** node to the tint input and set it to red, and connect the output to SDF text render type, the text color of your SDF text becomes red.

## Ports

| Name     | Direction | Type    | Description                          |
|----------|-----------|---------|--------------------------------------|
| Tint     | Input     | Color   | The tint color to apply to the text. |
| SDF Text | Output    | Texture | The rendered SDF text as a texture.  |

## Additional resources

- [Default Solid node](xref:default-solid-node)
- [Default Gradient node](xref:default-gradient-node)
- [Default Texture node](xref:default-texture-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)
- [Render Type Branch node](xref:render-type-branch-node)