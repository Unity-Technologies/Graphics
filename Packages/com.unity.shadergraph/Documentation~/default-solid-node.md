---
uid: default-solid-node
---

# Default Solid node

[!include[](include_note_uitk.md)]

Outputs the solid color specified for your UI elements, such as the background color of a button. For example, if you set the background color of a button to yellow, the **Default Solid** node outputs yellow for that button.

You can use this node combined with other nodes to create custom effects for the Solid color render type. For example, you can multiply the output of this node with a **Color** node to filter unwanted colors.

## Ports

| Name     | Direction | Type    | Description                                   |
|----------|-----------|---------|-----------------------------------------------|
| Solid    | Output    | Color   | The solid color specified for the UI element. |

## Additional resources

- [Default Gradient node](xref:default-gradient-node)
- [Default Texture node](xref:default-texture-node)
- [Default SDF Text node](xref:default-sdf-text-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)
- [Render Type Branch node](xref:render-type-branch-node)