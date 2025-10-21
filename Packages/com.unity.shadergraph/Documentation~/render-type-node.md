---
uid: render-type-node
---

# Render Type node

[!include[](include_note_uitk.md)]

The Render Type node outputs the current render type the shader is processing. You can use this node to create custom logic based on the render type. For example, you can use the output of the Render Type node to control logic that changes behavior depending on the render type.

## Ports

| Name       | Direction | Type    | Description                          |
|------------|-----------|---------|--------------------------------------|
| Solid      | Output    | Boolean | True when the shader is processing the Solid render type. |
| Texture    | Output    | Boolean | True when the shader is processing the Texture render type. |
| SDF Text   | Output    | Boolean | True when the shader is processing the SDF Text render type. |
| Bitmap Text| Output    | Boolean | True when the shader is processing the Bitmap Text render type. |
| Gradient   | Output    | Boolean | True when the shader is processing the Gradient render type. |

## Additional resources

- [Render Type Branch node](xref:render-type-branch-node)