# Upgrading HDRP from 2022.1 to 2022.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 13.x to 14.x.

## Directional Light Surface Texture

While Physically Based Sky is used, Directional lights can have a surface texture, located in section Celestial Body. The orientation of the texture was incorrect, in HDRP 14 it is fixed by flipping UVs on the x axis. When upgrading a project, suns texture might need to be flipped.

## XR

Starting from HDRP 14.x, Motion Blur is turned off by default when in XR. This behaviour can be changed in the XR section HDRP asset by enabling the option **Allow Motion Blur**.

## Material

### Alpha to mask

Starting from HDRP 14.x, Alpha to Mask option have been removed. Alpha to Mask is always enabled now when MSAA is enabled.

## Camera

Starting from HDRP 14.x, the default for the Gate Fit parameter on the Physical camera settings is Vertical as opposed to the old Horizontal default.

## Custom Pass

Starting from HDRP 14.x, the default custom color buffer format changed from R8G8B8A8_SNorm to R8G8B8A8_UNorm. The old option is still available in the custom color format so you can revert back to the SNorm format if you need to.

## Decals

An optimization performed on decals modified the rendering order between of decals that shares the same **Draw Order** value. If some of the decals in your scene overlaps, check the draw order in their material to ensure that they use different numbers.
