# Upgrading HDRP from Unity 2019.3 to Unity 2020.1

In the High Definition Render Pipeline (HDRP), some features work differently between major versions of Unity. This document helps you upgrade HDRP from Unity 2019.3 to 2020.1.

## Mesh LOD Transition

From Unity 2020.1, the cross LOD fade transition between a LitTessellation.shader or a LayeredLit.shader to object with a Material with no tessellation is no longer a dithering but a smooth decrease of the displacement strenght. It allow to improve transition between Firt high quality LOD done with tessellation to a second mid-qality LOD without it. The remaining transition are the same, i.e transition to low decimated mesh will still be a dithering.

## Scene View Camera Settings

From Unity 2020.1, the HDRP-specific settings of the scene view camera (anti-aliasing mode and stop NaNs) can be found in the same pop-up window as the standard scene camera settings, which are accessible by clicking the scene camera button on the toolbar of the scene window. These settings were previously in the HDRP preferences window (Edit > Preferences).

## Cookie baking

From Unity 2020.1, Cookie on light are not taken into account for the lightmaps / Lightprobes. This support is always enable with HDRP.

## Default Volume Profile

From Unity 2020.1, the Default Volume Profile asset has changed so that the Exposure component sets the default Compensation to 0. This may cause a decrease of brightness of 1EV on scene that haven't change the default settings and aren't overriding it.
