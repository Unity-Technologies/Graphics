# Mask and detail maps

The High Definition Render Pipeline (HDRP) uses [channel-packed](Glossary.md#ChannelPacking) textures to store multiple Material maps in a single texture. Channel packing is efficient because it allows the renderer to sample up to four grayscale maps that use the same UV coordinates with a single texture fetch. HDRP uses two types of channel-packed textures: the [Mask Map](#MaskMap), and the [Detail Map](#DetailMap). They can use a different set of UV coordinates, or a different UV tiling value, giving you more creative freedom.

This document describes the format of the mask map and detail map so that you can author your own to use in HDRP.

**Note**: When you import your texture into Unity, in the **Image Importer** Inspector window, make sure you disable **sRGB (Color Texture)** and you set **Texture Type** to **Default**.

<a name="MaskMap"></a>

## Mask map

The mask map contains four grayscale textures, one in each color channel. The default expected value of each channel is 0.5. HDRP uses the mask map to store the metallic map, ambient occlusion map, detail mask, and smoothness map for the material. The mask map stores these textures in the following channels:

| **Color channel** | **Map**     |
| ----------------- | ----------- |
| **Red**           | Metallic    |
| **Green**         | Ambient Occlusion|
| **Blue**          | Detail mask |
| **Alpha**         | Smoothness  |


**Note:** The detail mask texture allows you to control where the detail texture is applied on your model. Use a value of `1` to display the detail texture and a value of `0` to mask it. For instance, if your model has skin pores, you might mask the lips and eyebrows to prevent the pores from appearing in those areas.

To create a mask map, create a linear composited map in a photo editor, using the channels as described in the table above.

The following example image demonstrates the individual components of a full mask map.

![A full mask map, with the metallic texture in the R channel, the ambient occlusion texture in the G channel, the detail mask in the B channel, and the smoothness in the A channel.](Images/MaskMapAndDetailMap2.png)

<a name="DetailMap"></a>

## Detail map

The detail map enables you to overlay a second set of textures on top of the base surface information. Typically, the detail map scales several times across the object’s surface to add small details to a material. The detail map contains two grayscale textures and one two-component texture, which is the Material's detail normal map.

| **Color channel** | **Map**            |
| ----------------- | ------------------ |
| **Red**           | Desaturated albedo |
| **Green**         | Normal Y           |
| **Blue**          | Smoothness         |
| **Alpha**         | Normal X           |

To create a detail map, create a linear composited map in a photo editor, using the channels as described in the table above.

The following example image demonstrates the individual components of a full detail map.

![A full detail map, with the desaturated albedo texture in the R channel, the red channel of the normal map in the G channel, the green channel of the normal map in the A channel, and smoothness in the B channel.](Images/MaskMapAndDetailMap3.png)

### Desaturated albedo (red channel)

The red channel represents the albedo variation. It makes the underlying material's albedo gradually darken down to black when going from `0.5` to `0` or brighten up to white when going from `0.5` to `1`. A value of `0.5` is neutral, which means the detail map doesn't modify the albedo.

The image below shows the impact of the detail albedo on the final color. HDRP calculates color interpolation in sRGB space.

![A red material with a gradient detail map. When Detail Albedo Scale is 1, the two textures create a smooth red gradient. When Detail Albedo Scale is 2, the gradient is banded.](Images/DetailMap-red.png)

### Smoothness (blue channel)

The blue channel represents the smoothness variation and HDRP calculates it the same way as the albedo variation. The underlying material's smoothness gradually decreases if the detail smoothness is below `0.5` or increases if it's above `0.5`. A value of `0.5` is neutral, which means the detail map doesn't modify the smoothness.

The image below shows the impact of the detail smoothness on the final color.

![A material of smoothness 0.5 with a gradient detail map. When Detail Smoothness Scale is 1, the two textures create a smoothness gradient. When Detail Smoothness Scale is 2, the gradient transitions faster.](Images/DetailMap-blue.png)

The following example shows the same gradient detail map as above, used by three Lit materials with different smoothness values.

![Three squares with smoothness values of 0, 0.5, and 1.0. As the smoothness value increases, more of the right side of the square reflects the scene.](Images/DetailMap-smoothness.png)
