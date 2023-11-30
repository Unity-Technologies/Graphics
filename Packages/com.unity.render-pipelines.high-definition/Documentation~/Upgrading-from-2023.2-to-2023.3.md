# Upgrading HDRP from 2023.2 to 2023.3

In the High Definition Render Pipeline (HDRP), some features work differently between major versions. This document helps you upgrade HDRP from 16.x to 17.x.

## Materials

HDRP 17.x makes the following changes to Materials:

- When using path tracing, the effect of the **ambient occlusion** channel of materials with a mask map has been slightly modified. This was done to ensure that both rectangular area lights and rectangular meshes with emissive materials have the same lighting effect under ambient occlusion. As a consequence, the visual appearance of **ambient occlusion** has changed slightly.

## Reflection Probes and Planar Reflection Probes

**Reflection Probes** and **Planar Reflection Probes** now have an **Importance** setting to better sort them. The default value is 1 for **Reflection Probes** and 64 for **Planar Reflection Probes** so **Planar Reflection Probes** are displayed on top of **Reflection Probes**, as they are most of the time more accurate, while still allowing to sort **Reflection Probes** without interfering with **Planar Reflection Probes** until a certain point.

## Adaptive Probe Volume

With the introduction of the sky occlusion feature some asset data layout has changed. Previously baked data for adaptive probe volume will need to be rebaked.

## Path tracing noise pattern

Path tracing now has a *Seed Mode* parameter. The default is the **non repeating** noise pattern, which is different from the previous behavior. To match behavior in the last version, select the **repeating** pattern.
