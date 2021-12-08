# Upgrading HDRP from 2022.1 to 2022.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 13.x to 14.x.

## Directional Light Surface Texture

Directional Lights can set a surface texture in the Celestial Body section of the Inspector for use by the Physically Based Sky. From HDRP 2022.2, the texture is flipped on the x axis when displayed by the Physically Based Sky to fix display.
