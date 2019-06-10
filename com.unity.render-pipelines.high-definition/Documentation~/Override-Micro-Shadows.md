# Micro Shadows

Micro shadows are shadows that the High Definition Render Pipeline (HDRP) simulates for small details embedded in the Material of a GameObject, but not in its Mesh geometry. HDRP uses the details from the normal map and the ambient occlusion map to estimate the shadows those maps would cast if they were Meshes.

*In this image, the different layers of details in the Material shadow each other.*

![](Images/OverrideMicroShadows1.png)

*Micro shadows at 0.85 opacity on a mossy ground material viewed from the top.*

## Using Micro Shadows

HDRP uses the [Volume](Volumes.html) framework to generate **Micro Shadows**, so to enable and modify **Micro Shadow** properties, you must add a **Micro Shadows** override to a [Volume](Volumes.html) in your Scene. To add **Micro Shadows** to a Volume:

1. Select the Volume component in the Scene or Hierarchy to view it in the Inspector
2. In the Inspector, navigate to **Add Override > Shadowing** and click on **Micro Shadows**. 
   HDRP now processes **Micro Shadows** for any Camera this Volume affects.

## Properties

![](Images/OverrideMicroShadows2.png)

| **Property** | **Description**                                              |
| ------------ | ------------------------------------------------------------ |
| **Enable**   | Enable the checkbox to make HDRP calculate micro shadows when this Volume affects the Camera. |
| **Opacity**  | Use the slider to set the opacity of micro shadows for this Volume. |

## Details

Micro shadowing gives the impression of extremely detailed lighting that can capture small details. it relies on how you generate your Textures. When authoring the normal map and ambient occlusion map of the Material, consider the following: 

- For better results, capture the details of both Textures in a consistent way.
- Always use the same pipeline to produce your ambient occlusion maps.

Note that processing micro shadows is more resource intensive than not processing them.

### Useful links

- This micro shadowing technique is inspired by this presentation from Naughty Dog : [Technical Art of Uncharted 4](<http://advances.realtimerendering.com/other/2016/naughty_dog/index.html>).
- For an example of how to generate ambient occlusion maps in a consistent way, see this presentation from Sledgehammer : [Material Advances in Call of Duty WWII]( http://advances.realtimerendering.com/s2018/MaterialAdvancesInWWII.pdf).