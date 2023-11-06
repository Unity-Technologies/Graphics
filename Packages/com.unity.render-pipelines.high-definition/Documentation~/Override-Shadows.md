# Use the shadows volume component override

The **Shadows** [Volume component override](volume-component.md) controls the maximum distance at which HDRP renders shadow cascades and shadows from [punctual lights](Glossary.md#PunctualLight). It uses cascade splits to control the quality of shadows cast by Directional Lights over distance from the Camera.

To learn about the Shadows volume component properties, refer to [Shadows volume override reference](reference-shadows-volume-override.md).

## Create a shadow volume override

**Shadows** uses the [Volume](understand-volumes.md) framework, so to enable and modify **Shadows** properties, add a **Shadows** override to a [Volume](understand-volumes.md) in your Scene.

The **Shadows** override comes as default when you create a **Scene Settings** GameObject (Menu: **GameObject** > **Rendering** > **Scene Settings**). You can also manually add a **Shadows** override to any [Volume](understand-volumes.md). To add **Shadows** to a Volume:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, go to **Add Override** > **Shadowing** and select **Shadows**. You can now use the **Shadows** override to alter shadow settings for this Volume.

[!include[](snippets/volume-override-api.md)]