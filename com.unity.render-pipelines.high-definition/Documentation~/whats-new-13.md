# What's new in HDRP version 13 / Unity 2022.1

This page contains an overview of new features, improvements, and issues resolved in version 12 of the High Definition Render Pipeline (HDRP), embedded in Unity 2021.2.

## Added

## Updated

### Depth Of Field
HDRP version 13 includes optimizations in the physically based depth of field implementation. In particular, image regions that are out-of-focus are now computed at lower resolution, while in-focus regions retain the full resolution. For many scenes this results in significant speedup, without any visible reduction in image quality.
