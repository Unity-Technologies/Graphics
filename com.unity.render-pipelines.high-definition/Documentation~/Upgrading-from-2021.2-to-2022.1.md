# Upgrading HDRP from 2021.1 to 2021.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 12.x to 13.x.

## Color Grading

ACEScg is now the default color space for color grading HDRP projects. When you upgrade a project from an earlier version of HDRP, that project's color space for grading automatically changes to ACEScg. To revert your project's grading color space to sRGB, navigate to **Edit > Project Settings > Graphics > [HDRP Global Settings](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@13.0/manual/Default-Settings-Window.html)** > Color Grading Space.
