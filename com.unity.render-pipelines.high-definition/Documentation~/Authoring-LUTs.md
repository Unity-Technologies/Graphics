# Authoring lookup textures for HDRP

Lookup textures (LUTs) are color cubes that HDRP can apply to produce a final color-graded image. HDRP takes the original image color as a vector and then uses that to address the LUT to get the graded value. LUTs for the High Definition Render Pipeline (HDRP) use a CUBE file.

The guides in this section describe how to create an sRGB-targeting CUBE file with different external softwares, import it into a Unity Project that uses HDRP, and use it for [color grading](https://en.wikipedia.org/wiki/Color_grading) in your Scene.

The current guides cover the following software:

- [Adobe Photoshop](LUT-Authoring-Photoshop.md).
- [DaVinci Resolve](LUT-Authoring-Resolve.md).
