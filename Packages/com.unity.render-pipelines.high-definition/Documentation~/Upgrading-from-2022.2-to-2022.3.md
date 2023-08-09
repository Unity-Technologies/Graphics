# Upgrading HDRP from 2022.2 to 2022.3

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 2022.2 to 2022.3

## Light Baking

Starting from 2022.3, APV and lightmap bakes containing lights that are of type "Mixed" will now consider the "intensity multiplier" property, which was not taken into account previously.
