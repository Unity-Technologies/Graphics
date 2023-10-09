# Upgrading HDRP from 2023.1 to 2023.2

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 15.x to 16.x.

## Adaptive Probe Volume

Starting from 2023.2, Probe Volume is the default choice for light probe system.

## Light Baking

Starting from 2023.2, APV and lightmap bakes containing lights that are of type "Mixed" will now consider the "intensity multiplier" property, which was not taken into account previously.
