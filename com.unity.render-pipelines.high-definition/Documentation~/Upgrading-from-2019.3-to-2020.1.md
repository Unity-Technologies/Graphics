# Upgrading HDRP from Unity 2019.3 to Unity 2020.1

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2019.3 to 2020.1.

## Mesh LOD Transition

From Unity 2020.1, HDRP no longer uses dithering for the LOD crossfade transition between a LOD that uses a material with tessellation and a LOD that uses a material with no tessellation. Instead, HDRP smoothly decreases the tessellation displacement strength. This improves the transition between the first high-quality LOD with tessellation and a second mid-quality LOD without tessellation. The remaining transitions between non-tessellation materials still use dithering.

## Scene View Camera Settings

From Unity 2020.1, the HDRP-specific settings of the scene view camera (anti-aliasing mode and stop NaNs) can be found in the same pop-up window as the standard scene camera settings, which are accessible by clicking the scene camera button on the toolbar of the scene window. These settings were previously in the HDRP preferences window (Edit > Preferences).

## Cookie baking

From Unity 2020.1, Cookie on light are not taken into account for the lightmaps / Lightprobes. This support is always enable with HDRP.

## Default Volume Profile

From Unity 2020.1, the Default Volume Profile asset has changed so that the Exposure component sets the default Compensation to 0.