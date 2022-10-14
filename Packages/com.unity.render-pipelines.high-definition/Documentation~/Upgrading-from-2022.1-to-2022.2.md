# Upgrading from HDRP 13.x to 14.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 13.x to 14.x.

## Directional Light Surface Texture

When you use a Physically Based Sky, you can assign a surface texture to Directional lights in the **Celestial Body** section. 

HDRP 14 flips the UVs on the X axis to correct this texture's orientation.

## XR

From 14.x, HDRP disables Motion Blur by default when you use XR. To change this behaviour:

1. Open the [HDRP asset](HDRP-Asset.md).
2. Find the **XR** section.
3. Enable **Allow Motion Blur**.

## Materials

HDRP 14.x makes the following changes to Materials:

- When you upgrade a project to HDRP version 14.x, the **Ground Color** and **Ground Emission** textures of the PBR Sky might appear incorrectly. To fix this, flip both Textures on the x axis.
- The **Alpha to Mask** option does not exist in HDRP version 14.x. Instead, this version enables the Alpha to Mask behavior by default when you enable MSAA.
- The default setting for the Physical Camera's **Gate Fit parameter** setting is now Vertical.

## Refraction

HDRP 14.x introduces the following Refraction behavior:

- When you upgrade a project, refractive GameObjects use their own bounding boxes to approximate Refraction if they are not in range of a Reflection Probe.
- GameObjects that use a transparent Material and a **Refraction Model** use high quality refraction by default.
- The **Box** Refraction Model is now called **Planar** to indicate the kind of surface you should use this model on.

## Cloud Layer

HDRP 14.x changes the raymarching algorithm to improve scattering, and to provide more consistent results when you change the number of steps. 

Depending on your lighting conditions, you might have to tweak the **Density** and **Exposure** sliders to get the same results as earlier HDRP versions.

## Decal Projectors

HDRP 14.x improves the precision of the Decal Projector's **Angle Fade** property. This means that when you upgrade your project, Decal Projectors that use **Angle Fade** may produce different visual results.

## Reflection Probe Atlas

HDRP 14.x combines the planar and cube reflection probe textures into the same 2D texture atlas. This texture atlas previously contained only planar reflection probe textures. 

When HDRP automatically upgrades a project, it adjusts the atlas memory allocation so that the planar and cube reflection probes fit.

Unity might display a "No more space in Reflection Probe Atlas" error. To fix this error:

- Open the [HDRP Asset](HDRP-Asset.md).
- Increase the **Reflection 2D Atlas Size** value.