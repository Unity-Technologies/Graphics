# Volumetric Lighting

The High Definition Render Pipeline (HDRP) includes a volumetric lighting system that renders Volumetric Fog. HDRP also implements a unified lighting system, which means that all Scene components (such as [Lights](Light-Component.md), as well as opaque and transparent GameObjects) interact with the fog to make it volumetric.

## Enabling Volumetric Lighting

To enable and customize Volumetric Lighting in an [HDRP Asset](HDRP-Asset.md):

1. Select an HDRP Asset in your Unity Project and view it in the Inspector. In the **Lighting** section, enable the **Volumetrics** checkbox.
2. If you want to increase the resolution of the volumetrics, enable the **High Quality** checkbox. Volumetric lighting is a resource intensive effect and this option can potentially increase the resource intensity by up to eight times.
3. Go to **Edit > Project Settings > HDRP Default Settings** and, in the **Default Frame Settings** section, under the **Lighting** subsection, make sure you enable **Fog** and **Volumetrics** if they are not already.
4. Still in **Default Frame Settings**, if you want to enable reprojection support, enable **Reprojection**. This option improves the lighting quality in the Scene by taking previous frames into account when calculating the lighting for the current frame. Currently, this option is not compatible with dynamic lights, so you may encounter ghosting artifacts behind moving Lights. Additionally, using high values for **Anisotropy** in the [Fog](Override-Fog.md) Volume override may cause flickering Shadows.

## Notes
Volumetric fog does not work for Cameras that use oblique projection matrices. If you want a Camera to render volumetric fog, do not assign an off-axis projection to it.
