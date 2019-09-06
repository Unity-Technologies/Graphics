# Mask and detail maps

The High Definition Render Pipeline (HDRP) uses [channel-packed](Glossary.html#ChannelPacking) Textures to store multiple Material maps in a single Texture. Channel packing is efficient because it allows the renderer to sample up to four grayscale maps that use the same UV coordinates with a single Texture fetch. HDRP uses two types of channel-packed Textures: the [Mask Map](#MaskMap), and the [Detail Map](#DetailMap). They can use a different set of UV coordinates, or a different UV tiling value, giving you more creative freedom.

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

The detail map contains two grayscale Textures and one two-component Texture, which is the Material's normal map.

| **Color channel** | **Map**            |
| ----------------- | ------------------ |
| **Red**           | Desaturated albedo |
| **Green**         | Normal Y           |
| **Blue**          | Smoothness         |
| **Alpha**         | Normal X           |

The following example image demonstrates the individual components of a full detail map.

![](Images/MaskMapAndDetailMap3.png)