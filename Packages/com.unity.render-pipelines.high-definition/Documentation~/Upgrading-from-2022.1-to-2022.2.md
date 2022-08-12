# Upgrading from HDRP 13.x to 14.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 13.x to 14.x.

## Directional Light Surface Texture

When you use Physically Based Sky, you can assign a surface Texture to Directional lights in the **Celestial Body** section. The orientation of the texture was incorrect, in HDRP 14 it is fixed by flipping UVs on the x axis.

## XR

From HDRP 14.x, when you use XR Motion Blur is disabled by default. To change this behaviour:

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

- When you upgrade a project, refractive GameObjects that are not in range of a Reflection Probe use the bounding box of that GameObject to approximate Refraction.
- Objects that use a transparent Material and a Refraction Model use high quality refraction by default.

## Cloud Layer

HDRP 14 changes the raymarching algorithm to improve scattering, and to give more consistent results when you change the number of steps. Depending on your lighting conditions, you might have to tweak the **Density** and **Exposure** sliders to get the same result as earlier HDRP versions.

### Decal Projectors

HDRP 14.x improves the precision of the Decal Projector's **Angle Fade** property. This means that when you upgrade your project, Decal Projectors that use angle fade might appear differently.

## Local Volumetric Fog

The local volumetric fog system was completely rewritten to improve the performances, flexibility and artistic workflow.

This update removes the limit regarding the size of the 3D texture in the local volumetric fog mask value. A change was also made in the voxelization algorithm causing slightly different look on local volumetric fogs that can be corrected by increasing a bit the blend distance.

Also, note that the 3D texture atlas storing the fog mask textures is gone, and with it, the 3D texture copy executed each time the source texture was changed.

Additionally, because the 3D atlas was removed, HDRP doesn't automatically generates mipmaps for your 3D textures anymore. This can cause texture aliasing when the volume is small on the screen, to fix that, please enable mipmaps on your 3D textures.
