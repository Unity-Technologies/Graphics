# Volumetric Lighting

 

The Volumetric Lighting system renders Volumetric Fog and High Definition Render Pipeline (HDRP) implements a unified lighting system, which means that all Scene components (such as Lights, as well as opaque and transparent GameObjects) interact with the fog. This component is one of the several components that comprise Atmospheric Scattering in HDRP is Volumetric Fog.

Please note that the Volumetric Lighting system does not yet support area lights.

## Enabling Volumetric Lighting

To toggle and customize Volumetric Lighting in an [HDRP Asset](HDRP-Asset.html) :

 

1. Open an HDRP Asset in your Unity Project and view it in the Inspector. Enable Volumetric Lighting by checking the **Volumetrics** property in the **Render Pipeline Supported Features** section.

![](Images/VolumetricLighting1.png)

1. If you want to increase the resolution of the volumetrics, check **Increase resolution of volumetrics**. Volumetric lighting is an expensive effect, and this option can potentially increase the cost of volumetric lighting by up to eight times.
2. In the **Default Frame Settings** section, under the **Lighting Settings** subsection, make sure you enable **Atmospheric Scattering** and **Volumetric** if they are not already.
   ![](Images/VolumetricLighting2.png)

1. If you want to enable reprojection support, check **Reprojection for Volumetrics** . This option improves the lighting quality in the Scene by taking previous frames into account when calculating the lighting for the current frame. Currently, this option is not compatible with dynamic lights, so you may encounter ghosting artifacts behind moving Lights. Additionally, using high values for **Global Anisotropy** in the [Volumetric Fog](Volumetric-Fog.html) Volume override may cause flickering Shadows.