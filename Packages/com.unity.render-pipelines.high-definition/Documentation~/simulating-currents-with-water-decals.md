# Simulate currents with water decals

To add deformation, foam, or current effects to a water surface, use a water decal, which is a texture projected onto the surface.

A water decal is a shader graph Master Stack. It's applied in world space, allowing it to project across the water surface and integrate with the entire environment, not just a single area.

By default, water decal regions are anchored to the camera. You can also anchor them to a GameObject.

> [!NOTE]
> For backward compatibility, [water masks and current water decals](enable-mask-and-current-water-decals.md) are disabled by default.

## Water decal shader graph Master Stack

By default, the water decal shader graph Master Stack contains the following properties:

- **Deformation**
- **SurfaceFoam**
- **DeepFoam**

Once you have [enabled water mask and current water decals](enable-mask-and-current-water-decals.md), you can add the following water features through the Graph Inspector:

- **SimulationMask**
- **SimulationFoamMask**
- **LargeCurrent**
- **LargeCurrentInfluence**
- **RipplesCurrent**
- **RipplesCurrentInfluence**

## Decal layer masks

To add foam, you can change material properties (base color, smoothness, normals, etc.) by using a [decal](decals.md) on a water surface. For example, you might use this technique to imitate debris floating on the water.
**Global Opacity** determines the amount of influence the decal has on the appearance of the water surface.

Enabling horizontal deformation has the following effects:

- You can add a new **HorizontalDeformation** feature in the Graph Inspector of a water decal shader graph.
- HDRP creates a new buffer, which increases the amount of memory HDRP uses.
- The results of water scripts and [underwater effects](water-underwater-view.md) and [script interactions](float-objects-on-a-water-surface.md)might be less accurate.

## Additional resources

- [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest)
