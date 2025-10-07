---
uid: element-texture-uv-node
---

# Element Texture UV node

[!include[](include_note_uitk.md)]

Outputs the texture coordinates (UV) of the texture mapped onto a UI element.

This node might output different coordinates than the [**Element Layout UV**](xref:element-layout-uv-node) node, which provides coordinates within the element's bounding rectangle. The coordinates are more likely to be different if you tile, offset, or transform the texture.

If the texture is part of an atlas, its UV coordinates only map to a specific region within the atlas. If you repeat UV coordinates or sample outside them, the data comes from other textures in the atlas. Use texture coordinates (UV) when you need precise control over how a texture appears on a UI element, and be mindful of atlas constraints.

The texture UV can also originate from a custom mesh when you call [`MeshGenerationContext.DrawMesh`](xref:UnityEngine.UIElements.MeshGenerationContext.DrawMesh(Unity.Collections.NativeSlice`1<UnityEngine.UIElements.Vertex>,Unity.Collections.NativeSlice`1<System.UInt16>,UnityEngine.Texture)). In such cases, the UV values might vary depending on the mesh data. 

## Ports

| Name            | Direction | Type    | Description                          |
|-----------------|-----------|---------|--------------------------------------|
| Texture UV      | Output    | Vector2 | The UV coordinates for the texture.  |

## Additional resources

- [Element Layout UV node](xref:element-layout-uv-node)
- [Element Texture Size node](xref:element-texture-size-node)