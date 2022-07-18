# Upgrading HDRP from 7.x to 8.x

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 7.x to 8.x.

## Mesh LOD Transition

From 8.x, HDRP no longer uses dithering for the LOD crossfade transition between a LOD that uses a material with tessellation and a LOD that uses a material with no tessellation. Instead, HDRP smoothly decreases the tessellation displacement strength. This improves the transition between the first high-quality LOD with tessellation and a second mid-quality LOD without tessellation. The remaining transitions between non-tessellation materials still use dithering.

## Scene View Camera Settings

From 8.x, you can find the HDRP-specific settings of the Scene view Camera (**Antialiasing Mode** and **Stop NaNs**) in the same pop-up window as the standard Scene Camera settings, which are accessible by clicking the scene camera button on the toolbar of the Scene view. These settings were previously in the HDRP preferences window (menu: **Edit** > **Preferences**).

## Cookie baking

From 8.x, Cookies on Lights aren't taken into account for the lightmaps and Light Probes. This support is always enabled with HDRP.

## Default Volume Profile

From 8.x, the default Volume Profile Asset has the [Exposure](Override-Exposure.md) override's **Compensation** set to 0. This may cause a decrease in brightness of 1 [EV](Physical-Light-Units.md#EV) in Scenes where you haven't changed the default settings and don't override them.

## Custom Pass Volume

From 8.x, the Custom Pass System executes all the custom passes in the Scene even if you have more than one **Custom Pass Volume** configured with the same injection point. Before 8.x, having two or more **Custom Pass Volume** with the same injection point resulted in an execution of only one **Custom Pass Volume**. This was an undefined behavior as you couldn't predict which **Custom Pass Volume** would be executed.

There is now a priority in the **Custom Pass Volume** which can be used to modify the execution order between custom passes that uses the same injection point.
