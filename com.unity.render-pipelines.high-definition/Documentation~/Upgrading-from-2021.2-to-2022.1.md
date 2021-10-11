# Upgrading HDRP from 2021.1 to 2021.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 12.x to 13.x.

## Color Grading

Starting from HDRP 13.x, HDRP will use ACEScg as a default color space to perform color grading even when using a non-ACES  if  unlike the previously used sRGB color space.  It is possible to go back to sRGB in the Global Settings Asset.
