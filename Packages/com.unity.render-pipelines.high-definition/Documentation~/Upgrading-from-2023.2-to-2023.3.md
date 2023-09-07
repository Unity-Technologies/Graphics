# Upgrading HDRP from 2023.1 to 2023.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 16.x to 17.x.

## Materials

HDRP 17.x makes the following changes to Materials:

- When using path tracing, the effect of the **ambient occlusion** channel of materials with a mask map has been slightly modified. This was done to ensure that both rectangular area lights and rectangular meshes with emissive materials have the same lighting effect under ambient occlusion. As a consequence, the visual appearance of **ambient occlusion** has changed slightly.
