---
uid: element-layout-uv-node
---

# Element Layout UV node

[!include[](include_note_uitk.md)]

Outputs the geometric coordinates (UV) relative to the UI element, such as a button. It allows you to determine your position within the element itself, regardless of the texture applied. It represents the relative coordinates within the layout rect of the visual element, where `(0,0)` is the bottom-left corner and `(1,1)` is the top-right corner.

## Ports

| Name               | Direction | Type    | Description                          |
|--------------------|-----------|---------|--------------------------------------|
| Layout UV          | Output    | Vector2 | The UV coordinates for the element.  |

## Additional resources

- [Element Texture UV node](xref:element-texture-uv-node)
- [Element Texture Size node](xref:element-texture-size-node)