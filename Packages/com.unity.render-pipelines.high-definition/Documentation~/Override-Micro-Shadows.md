# Micro Shadows

Micro shadows are shadows that the High Definition Render Pipeline (HDRP) simulates for small details embedded in the Material of a GameObject, but not in its Mesh geometry.

HDRP uses the details from the normal map, the ambient occlusion map, and specular occlusion to estimate the shadows those details would cast if they were meshes.

In this image, the different layers of details in the Material shadow each other.

![A ground texture with micro shadows enabled and disabled. With micro shadows enabled, the twigs and moss cast more realistic-looking shadows.](Images/OverrideMicroShadows1.png)

Micro shadows at 0.85 opacity on a mossy ground material viewed from the top.

## Use Micro Shadows

HDRP uses the [Volume](understand-volumes.md) framework to generate **Micro Shadows**, so to enable and modify **Micro Shadow** properties, you must add a **Micro Shadows** override to a [Volume](understand-volumes.md) in your Scene. To add **Micro Shadows** to a Volume:

1. Select the Volume component in the Scene or Hierarchy to view it in the Inspector
2. In the Inspector, go to **Add Override** > **Shadowing** and select **Micro Shadows**. HDRP now processes **Micro Shadows** for any Camera this Volume affects.

Micro shadowing only works with directional [Lights](Light-Component.md). If you enable micro shadows, make sure you have a directional Light in the Scene.

[!include[](snippets/volume-override-api.md)]

## Properties

[!include[](snippets/Volume-Override-Enable-Properties.md)]

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **State**    | When set to **Enabled**, HDRP calculates micro shadows when this Volume affects the Camera. |
| **Opacity**  | Use the slider to set the opacity of micro shadows for this Volume. |

## Details

Micro shadowing gives the impression of extremely detailed lighting that can capture small details. It relies on how you generate your Textures so, to produce better results, consider the following when you author the normal map and ambient occlusion map for a Material:

- Make sure to capture the details of both Textures in a consistent way.
- Always use the same pipeline to produce your normal maps and ambient occlusion maps.

Note that processing micro shadows is more resource intensive than not processing them.

### Useful links

- This micro shadowing technique is inspired by this presentation from Naughty Dog: [Technical Art of Uncharted 4](<http://advances.realtimerendering.com/other/2016/naughty_dog/index.html>).
- For an example of how to generate ambient occlusion maps in a consistent way, see this presentation from Sledgehammer: [Material Advances in Call of Duty WWII]( http://advances.realtimerendering.com/s2018/MaterialAdvancesInWWII.pdf).
