# Upgrading HDRP from 2023.1 to 2023.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 15.x to 16.x.

## Adaptive Probe Volume

HDRP version 16 uses Probe Volumes for light probe systems by default.

## LOD dithering

HDRP 16 deprecates the `supportDitheringCrossFade` setting in the HDRP Asset. Instead, use the Quality Settings property `enableLODCrossFade`.
When you upgrade to 2023.2 HDRP automatically sets the Quality Settings property  `enableLODCrossFade`  to `True` if you enabled it in the HDRP Asset.

## Decals in HDRP Path Tracer 

HDRP 16 includes path tracer decal rendering, which means you might need to increase the [Maximum Lights per Cell (Ray Tracing)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/HDRP-Asset.html#Lights) value and the size of the [decal atlas](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/HDRP-Asset.html#Decals) to render decals correctly. 

## Default Volume Profile

The HDRP Default Volume defines the default values for the Default layer and all other volume layers.

## Light Baking

From version 16, baked probe volumes and lightmaps that contain lights that use the **Mixed** mode take the **Intensity multiplier** property into account.

## Volume Framework

When you create a custom Volume component class that overrides the `VolumeComponent.Override(VolumeComponent state, float interpFactor)` method, your implementation must set the `VolumeParameter.overrideState` property to `true` whenever the `VolumeParameter` value is changed. This ensures that the Volume framework resets the parameters to their correct default values. This lets the framework to use fewer resources every frame which improves performance.

For an example, refer to the [Override(VolumeComponent, float)](xref:UnityEngine.Rendering.VolumeParameter.overrideState) description.
