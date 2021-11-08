# What's new in HDRP version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Added

### HDR Output Support

HDRP 13.0 introduces support to output to HDR Displays, supporting both  HDR10 and scRGB standards.

When outputting to an HDR-capable device, if the project enabled HDR Output, HDRP will make use of the higher brightness contrast and wider color gamut capabilities of the display.
The user can also chose for various options on how to customize the tonemapping for the HDR case and few options are provided to allow the game to adapt to various output displays and calibrate basing on device metadata or user preferences.

For more information consult the [HDR Output](HDR-Output.md) documentation

## Updated

### Depth Of Field
HDRP version 13 includes optimizations in the physically based depth of field implementation. In particular, image regions that are out-of-focus are now computed at lower resolution, while in-focus regions retain the full resolution. For many scenes this results in significant speedup, without any visible reduction in image quality.
