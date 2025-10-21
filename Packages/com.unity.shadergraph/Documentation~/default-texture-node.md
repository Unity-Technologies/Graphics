---
uid: default-texture-node
---

# Default Texture node

[!include[](include_note_uitk.md)]

Provides the texture assigned to the UI element. 

You can use this node to access the texture assigned to a UI element, such as a Texture 2D background image. The node includes UV and tint inputs that allow you to modify how the texture is applied. For example, you can connect a **Tiling and Offset** node to the UV input to create a repeating effect for the background image, or connect a **Color** node to the tint input to adjust the tint color of the background image.

## Ports

| Name     | Direction | Type    | Description                          |
|----------|-----------|---------|--------------------------------------|
| UV       | Input     | Vector2 | The UV coordinates for the texture.   |
| Tint     | Input     | Color   | The tint color to apply to the texture. |
| Texture  | Output    | Texture | The resulting texture.       |

## Additional resources

- [Default Solid node](xref:default-solid-node)
- [Default Gradient node](xref:default-gradient-node)
- [Default SDF Text node](xref:default-sdf-text-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)