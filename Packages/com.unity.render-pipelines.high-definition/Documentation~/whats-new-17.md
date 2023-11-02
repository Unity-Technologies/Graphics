# What's new in HDRP version 17 / Unity 2023.3

This page contains an overview of new features, improvements, and issues resolved in version 17 of the High Definition Render Pipeline (HDRP), embedded in Unity 2023.3.

## Added

### Lights

Disc and tube shaped area lights are now supported with path tracing.

### Physically Based Sky

The PBR sky now additionally includes an ozone layer as part of the atmosphere model.
Additionally, the precomputation steps have been optimized and can now be performed every frame without considerable framerate drop. The memory usage for the precomputed tables have also been reduced.
Finally, aerial perspective can now be enabled to simulate light absorption by particles in the atmosphere when looking at objects in the distance, such as mountains or clouds.

## Updated

### Environement effects

Planet parametrization for effects like fog, physically based sky and volumetric clouds have been moved to a shared place in the **Visual Environement** override.

### Volumetric clouds

The volumetric clouds are not clipped by the far plane anymore.

### Seed mode parameter in path tracing 

The HDRP path tracer now allows you to choose a *Seed mode*. This determines the noise pattern used to path trace.
