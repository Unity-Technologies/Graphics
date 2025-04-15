# Simulate currents with a water decal

To add deformation, foam, or current effects to a water surface, use a water decal, which is a texture projected onto the surface.

A water decal is a shader graph Master Stack. It's applied in world space, allowing it to project across the water surface and integrate with the entire environment, not just a single area.

By default, water decal regions are anchored to the camera. You can also anchor them to a GameObject.

> [!NOTE]
> For backward compatibility, [water masks and current water decals](enable-mask-and-current-water-decals.md) are disabled by default.

## Create a water decal

1. In the main menu, select **GameObject** > **Water** > **Water Decal**.

    Unity adds a **Water Decal** GameObject to your scene.
2. Move the **Water Decal** GameObject to the area of water you want to affect.
3. To add deformation, foam, or current effects to the water decal shader graph, select the **Water Decal** GameObject, then under **Water Decal (Material)** select **Edit...**.

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

## Enable horizontal deformation

To enable horizontal deformation, go to the active [HDRP Asset](hdrp-asset.md), then under **Rendering** > **Water** enable **Horizontal Deformation**.

Enabling horizontal deformation has the following effects:

- You can add a new **HorizontalDeformation** feature in the Graph Inspector of a water decal shader graph.
- HDRP creates a new buffer, which increases the amount of memory HDRP uses.
- The results of water scripts and [underwater effects](water-underwater-view.md) and [script interactions](float-objects-on-a-water-surface.md)might be less accurate.

## Additional resources

- The **RollingWave** scene in the [Water package samples](HDRP-Sample-Content.md#water-samples).
- [Shader Graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest)
