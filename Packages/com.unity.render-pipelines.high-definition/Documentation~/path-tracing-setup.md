# Setup path tracing

Path tracing shares the general requirements and setup as other ray tracing effects, so for information on hardware requirements and set up, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md). You must carry out this setup before you can add path tracing to your Scene.

## Add path tracing to a scene

Path tracing uses the [Volume](understand-volumes.md) framework, so to enable this feature, and modify its properties, you must add a Path Tracing override to a [Volume](understand-volumes.md) in your Scene. To do this:

1. In the Scene or Hierarchy view, select a GameObject that contains a Volume component to view it in the Inspector.
2. In the Inspector, select **Add Override** > **Ray Tracing** > **Path Tracing**.
3. In the Inspector for the Path Tracing Volume Override, set **State** to **Enabled**. If you don't see **State**, make sure your HDRP Project supports ray tracing. For information on setting up ray tracing in HDRP, see [getting started with ray tracing](Ray-Tracing-Getting-Started.md). This switches HDRP to path-traced rendering and you should initially see a noisy image that converges towards a clean result.
4. If the image doesn't converge over time, select the drop-down next to the effect toggle and enable Always Refresh.

![Gizmo menu with the effect toggle dropdown highlighted.](Images/RayTracingPathTracing3.png)

## Properties

For information about path tracing properties, refer to [Path tracing reference](reference-path-tracing.md).