# Simulate currents with a water decal

To add deformation, foam, or current effects to a water surface, use a water decal, which is a texture projected onto the surface.

A water decal is a shader graph Master Stack. It's applied in world space, allowing it to project across the water surface and integrate with the entire environment, not just a single area.

By default, water decal regions are anchored to the camera. You can also anchor them to a GameObject.

> [!NOTE]
> For backward compatibility, [water masks and current water decals](enable-mask-and-current-water-decals.md) are disabled by default.

## Create a water decal

1. In the main menu, go to **GameObject** > **Water** > **Water Decal**.

    Unity adds a **Water Decal** GameObject to your scene.

1. Move the **Water Decal** GameObject to the area of water you want to affect.

1. To add deformation, foam, or current effects to the water decal shader graph, in the **Inspector** window of the **Water Decal**, go to **Water Decal (Script)**, then select **Edit** in the **Water Decal (Material)** section.

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
