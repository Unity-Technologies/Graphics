---
uid: active-shadow2d-light-node
---

# Active Shadow2D Light node

The Active Shadow2D Light node outputs the position and radius of the 2D light that's currently casting the shadow. Use it in a Shadow2D shader graph to change the shape of a shadow based on the light, for example to make shadows longer as the light moves farther away.

## Render pipeline compatibility

| **Node**            | **Universal Render Pipeline (URP)** | **High Definition Render Pipeline (HDRP)** |
| -------------------- | ------------------------------------- | --------------------------------------------- |
| **Active Shadow2D Light** | Yes                                    | No                                             |

This node is only available with the **Shadow Caster 2D** shader graph target.

## Ports

| **Name** | **Direction** | **Type** | **Description** |
|------|-----------|------|-------------|
| **Position** | Output | Vector 3 | The position of the light GameObject in world space. |
| **Radius** | Output | Float | The radius of a circle, centered on the light position, that contains the whole area the light affects. |

## Generated code example

The following example code represents one possible outcome of this node.

```hlsl
void Unity_ShadowCaster2DLight(out float3 Position, out float Radius)
{
    Position = _LightPos;
    Radius = _ShadowRadius;
}
```

## Additional resources

- [Shadow2D shader graph reference for URP](https://docs.unity3d.com/Manual/urp/prebuilt-shader-graphs-urp-shadow2d.html)
- [Create a custom material for 2D shadows](https://docs.unity3d.com/Manual/urp/2d-shadow-custom-material.html)
