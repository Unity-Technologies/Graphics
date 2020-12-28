# Mask and detail maps

The High Definition Render Pipeline (HDRP) uses [channel-packed](Glossary.md#ChannelPacking) Textures to store multiple Material maps in a single Texture. Channel packing is efficient because it allows the renderer to sample up to four grayscale maps that use the same UV coordinates with a single Texture fetch. HDRP uses two types of channel-packed Textures: the [Mask Map](#MaskMap), and the [Detail Map](#DetailMap). They can use a different set of UV coordinates, or a different UV tiling value, giving you more creative freedom.

This document describes the format of the mask map and detail map so that you can author your own to use in HDRP.

To create a mask map:

1. Open image editing software that supports channel editing (such as Adobe Photoshop).
2. Drag your grayscale Textures into their respective color channel. For information about which Texture belongs in which channel, see [mask map](#MaskMap) and [detail map](#DetailMap).<br />![](Images/MaskMapAndDetailMap1.png)
3. Export your image.
4. When you import the image into Unity, make sure that it uses linear color space.

<a name="MaskMap"></a>

## Mask map

The mask map contains four grayscale Textures, one in each of its color channels.

| **Color channel** | **Map**     |
| ----------------- | ----------- |
| **Red**           | Metallic    |
| **Green**         | Occlusion   |
| **Blue**          | Detail mask |
| **Alpha**         | Smoothness  |

The following example image demonstrates the individual components of a full mask map.

![](Images/MaskMapAndDetailMap2.png)

<a name="DetailMap"></a>

## Detail map

The detail map allow you to overlay a second set of textures on top of the base surface information. Typically, the detail map would be scaled several times across the object’s surface to add small details to a material.
The detail map contains two grayscale Textures and one two-component Texture, which is the Material's detail normal map.
When you import the detail map, make sure to disable the **sRGB** checkbox in the Import Settings.

| **Color channel** | **Map**            |
| ----------------- | ------------------ |
| **Red**           | Desaturated albedo |
| **Green**         | Normal Y           |
| **Blue**          | Smoothness         |
| **Alpha**         | Normal X           |

The following example image demonstrates the individual components of a full detail map.

![](Images/MaskMapAndDetailMap3.png)

### Desaturated albedo (red channel)

The red channel represents the albedo variation. It can make the underlying material's albedo gradually darken down to black when going from `0.5` to `0` or brighten up to white when going from `0.5` to `1`. A value of `0.5` is neutral, which means the detail map does not modify the albedo.

The image below shows the impact of the detail albedo on the final color. As you can see, HDRP calculates color interpolation in sRGB space.

![](Images/DetailMap-red.png)

### Smoothness (blue channel)

The blue channel represents the smoothness variation and HDRP computes it the same way as the albedo variation. The underlying material's smoothness gradually decreases if the detail smoothness is below `0.5` or increases if it is above `0.5`. A value of `0.5` is neutral, which means the detail map does not modify the smoothness.

The image below shows the impact of the detail smoothness on the final color.

![](Images/DetailMap-blue.png)

The following example shows the same gradient detail map as above, used by three Lit materials with different smoothness values.

![](Images/DetailMap-smoothness.png)
