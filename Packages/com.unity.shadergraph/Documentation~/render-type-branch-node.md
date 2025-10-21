---
uid: render-type-branch-node
---

# Render Type Branch node

[!include[](include_note_uitk.md)]

The Render Type Branch node routes inputs based on the current render type and outputs the appropriate results for the Fragment node. You can connect the inputs to various nodes to define how each render type is processed.

UI Shader Graph provides default nodes, such as the [Default Solid node](xref:default-solid-node) or [Default Texture node](xref:default-texture-node), for each render type. You can use these nodes as starting points when customizing your shaders. For best performance, if you want an input to use its default value for a render type, leave it disconnected rather than connecting it to a default node. The Render Type Branch node automatically uses the default values for that input and handles branching more efficiently when you disconnect inputs. Only connect these default nodes if you want to customize the shader’s behavior.

## Ports

| Name       | Direction | Type    | Description                          |
|------------|-----------|---------|--------------------------------------|
| Solid      | Input     | Color   | The color to use for backgrounds and borders. |
| Texture    | Input     | Texture | The texture to use for texture graphics.        |
| SDF Text   | Input     | Texture | The texture to use for SDF text.                |
| Bitmap Text| Input     | Texture | The texture to use for bitmap text.             |
| Gradient   | Input     | Texture | The texture to use for vector graphic gradients.|
| Color      | Output    | Color   | The output color.                    |
| Alpha      | Output    | Float   | The output alpha value.              |

## Additional resources

- [Render Type node](xref:render-type-node)
- [Default Solid node](xref:default-solid-node)
- [Default Texture node](xref:default-texture-node)
- [Default SDF Text node](xref:default-sdf-text-node)
- [Default Bitmap Text node](xref:default-bitmap-text-node)
- [Default Gradient node](xref:default-gradient-node)
