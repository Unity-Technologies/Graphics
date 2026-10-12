---
uid: light-texture-node
---

# Light Texture node

The Light Texture node outputs a shape light texture from the 2D Renderer. Use it to access a light texture in a Custom Lit or Unlit sprite shader and apply 2D lighting effects manually.

## Render pipeline compatibility

| **Node**            | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------- | ------------------------------------- | --------------------------------------------- |
| **Light Texture**   | Yes                                    | No                                             |

This node is only available with the **Sprite Custom Lit** or **Sprite Unlit** shader graph target.

## Controls

| **Name** | **Type** | **Options** | **Description** |
|------|------|---------|-------------|
| **Blend Style** | Enum | LightTex0, LightTex1, LightTex2, LightTex3 | Selects which blend style slot to read the shape light texture from. Each slot corresponds to a Blend Style configured in the 2D Renderer asset. |

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|------|-----------|------|-------------|
| **Out** | Output | Texture 2D | The shape light texture for the selected blend style slot. Returns a white texture when no shape light type is active. |

## Generated code example

The following example code represents one possible outcome of this node.

```hlsl
UnityTexture2D Unity_GetLightTexture0()
{
#if USE_SHAPE_LIGHT_TYPE_0 || USE_SHAPE_LIGHT_TYPE_1 || USE_SHAPE_LIGHT_TYPE_2 || USE_SHAPE_LIGHT_TYPE_3
return UnityBuildTexture2DStructNoScale(_ShapeLightTexture0);
#else
return UnityBuildTexture2DStructNoScale(_DefaultWhiteTex);
#endif
}
```

## Additional resources

- [Sample Texture 2D node](Sample-Texture-2D-Node.md)
