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

## Physically Based Sky

Starting from HDRP 14.x, the **Ground Color** and **Ground Emission** textures of the PBR Sky have been flipped on the x axis. When upgrading a project, these textures might need to be flipped.

## Refraction

Objects with Transparent Materials and a Refraction Model enabled now fall back to a higher quality default refraction behavior.

Previously, Materials did not contain a refraction result unless you configured a Reflection Probe and the GameObject was within the probe's extents.

Unity now uses the bounding box of an object as a fallback approximation to compute the Refraction. When you upgrade a project, refractive objects that are not within the extents of a Reflection Probe demonstrate this improved behavior.

## Unity Material Ball

The old Unity material ball has been modified to remove all references to the old Unity logo.

The new Unity material ball with the new Unity logo can be accessed from com.unity.render-pipelines.high-definition\Runtime\RenderPipelineResources\Prefab.

Material Samples have been updated accordinly.
