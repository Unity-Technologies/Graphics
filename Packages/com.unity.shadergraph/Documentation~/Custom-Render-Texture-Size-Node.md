# Custom Render Texture Size Node

## Description

Provides the size of the current **Custom Render Texture**.

For more information on Custom Render Textures, refer to the [Unity Manual](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html).

## Ports

| Name           | Direction           | Type      | Binding | Description                                                                                                  |
|:---------------|:-------------|:----------|:---|:-------------------------------------------------------------------------------------------------------------|
| Texture Width  | Output      | Float     | None | Width of the **Custom Render Texture**.                                                                      |
| Texture Height | Output      | Float   | None | Height of the **Custom Render Texture**.                                                                     |
| Texture Depth  | Output      | Float | None | Volume depth of the **Custom Render Texture**. This is valid only for 3D texture and 2D texture array types. |

## Generated Code Example

The following example code represents one possible outcome of this node.

```
_CustomRenderTextureWidth
```