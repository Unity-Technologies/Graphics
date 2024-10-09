# Custom Render Texture Slice Node

## Description

Provides the slice index or cubemap face of the current **Custom Render Texture**. When a **Custom Render Texture** is a Cubemap, 3D texture, or 2D texture array, Shader Graph issues multiple draw calls to update each slice or face separately. Use this node to get the slice index or cubemap face.

For more information on Custom Render Textures, refer to the [Unity Manual](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html).

## Ports

| Name                | Direction           | Type      | Binding | Description                                                                                                                                     |
|:--------------------|:-------------|:----------|:---|:------------------------------------------------------------------------------------------------------------------------------------------------|
| Texture Cube Face   | Output      | Float     | None | The current face of the **Custom Render Texture** being updated. This value is an integer between 0 and 5 included.                             |
| Texture Depth Slice | Output      | Float   | None | The current slice index of the **Custom Render Texture** being updated. This value is an integer between 0 and the volume depth of the texture. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
_CustomRenderTextureCubeFace
```