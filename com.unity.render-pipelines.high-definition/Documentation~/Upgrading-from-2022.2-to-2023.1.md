# Upgrading HDRP from 2022.2 to 2023.1

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 14.x to 15.x.

## Rendering Layers

The Receive Decals property of Materials in HDRP does not affect emissive decals anymore when Decal Layers are enabled. To disable emissive decals on specific meshes, you will have to use a Decal Layer instead of relying on the Receive Decals property.
