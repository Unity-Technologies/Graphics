---
uid: sample-element-texture-node
---

# Sample Element Texture node

[!include[](include_note_uitk.md)]

The Sample Element Texture node samples a texture at specific UV coordinates. You can use this node to get multiple samples of the texture assigned to the element, for example to create complex visual effects or manipulate the texture.

Sampling multiple times with this node is more efficient than using several separate nodes for individual samples. This is because each node introduces overhead by traversing internal branches to select the correct texture slot. By combining multiple samples into a single node, you reduce this overhead and improve performance.

## Ports

| Name    | Direction | Type    | Description                          |
|---------|-----------|---------|--------------------------------------|
| UV 0    | Input     | Vector2 | The UV coordinates to sample.        |
| UV 1    | Input     | Vector2 | The second set of UV coordinates to sample.    |
| UV 2    | Input     | Vector2 | The third set of UV coordinates to sample.     |
| UV 3    | Input     | Vector2 | The fourth set of UV coordinates to sample.    |
| Color 0 | Output    | Color   | The sampled color from UV 0.                   |
| Color 1 | Output    | Color   | The sampled color from UV 1.                   |
| Color 2 | Output    | Color   | The sampled color from UV 2.                   |
| Color 3 | Output    | Color   | The sampled color from UV 3.                   |

## Additional resources

- [Create and apply custom shaders](xref:uie-create-apply-custom-shaders)
