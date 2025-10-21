---
uid: default-gradient-node
---

# Default Gradient node

[!include[](include_note_uitk.md)]

Outputs the gradient specified for your UI elements. For example, if you set the background image of a button to use a vector graphic with a linear gradient from top red to bottom green, the Default Gradient node outputs that gradient for the button.

You can use the Default Gradient node combined with other nodes to create custom effects for the gradient render type. For example, you can multiply the output of this node with a **Color** node to filter unwanted colors from the gradient.

## Ports

| Name     | Direction | Type    | Description                          |
|----------|-----------|---------|--------------------------------------|
| Gradient | Output    | Gradient| The gradient specified for the UI element.  |


## Additional resources

- [Default Solid node](xref:default-solid-node)
- [Default Texture node](xref:default-texture-node)
- [Default SDF Text node](xref:default-sdf-text-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)
- [Render Type Branch node](xref:render-type-branch-node)