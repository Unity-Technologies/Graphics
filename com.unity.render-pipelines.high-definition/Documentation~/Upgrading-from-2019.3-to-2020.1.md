# Upgrading HDRP from 7.x to 8.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 7.x to 8.x.

## Mesh LOD Transition

From 8.x, HDRP no longer uses dithering for the LOD crossfade transition between a LOD that uses a material with tessellation and a LOD that uses a material with no tessellation. Instead, HDRP smoothly decreases the tessellation displacement strength. This improves the transition between the first high-quality LOD with tessellation and a second mid-quality LOD without tessellation. The remaining transitions between non-tessellation materials still use dithering.

## Scene View Camera Settings

From 8.x, the HDRP-specific settings of the scene view camera (anti-aliasing mode and stop NaNs) can be found in the same pop-up window as the standard scene camera settings, which are accessible by clicking the scene camera button on the toolbar of the scene window. These settings were previously in the HDRP preferences window (Edit > Preferences).

## Cookie baking

From 8.x, Cookie on light are not taken into account for the lightmaps / Lightprobes. This support is always enable with HDRP.

## Default Volume Profile

From 8.x, the default Volume Profile Asset has the [Exposure](Override-Exposure.md) override's **Compensation** set to 0. This may cause a decrease in brightness of 1[EV](Physical-Light-Units.md#EV) in scene's that have not changed the default settings and do not override them.

## Custom Pass Volume

From 8.x, the Custom Pass System executes all the custom passes in the scene even if you have more than one **Custom Pass Volume** configured with the same injection point. Prior to this version, having two or more **Custom Pass Volume** with the same injection point resulted in an execution of only one **Custom Pass Volume**. This was an undefined behavior as you couldn't predict which **Custom Pass Volume** would be executed.

There is now a priority in the **Custom Pass Volume** which can be used to modify the execution order between custom passes that uses the same injection point.
