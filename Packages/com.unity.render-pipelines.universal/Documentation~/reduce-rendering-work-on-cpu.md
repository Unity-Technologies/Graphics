# Reduce rendering work on the CPU

You can use the GPU Resident Drawer or GPU occlusion culling to speed up rendering. When you enable these features, Unity optimizes the rendering pipeline so the CPU has less work to do each frame, and the GPU draws GameObjects more efficiently.

|Page|Description|
|-|-|
|[Use the GPU Resident Drawer](gpu-resident-drawer.md)|Automatically use the `BatchRendererGroup` API to use instancing and reduce the number of draw calls.|
|[Make a GameObject compatible with the GPU Resident Drawer](make-object-compatible-gpu-rendering.md)|Include or exclude a GameObject from the GPU Resident Drawer.|
|[Use GPU occlusion culling](gpu-culling.md)|Use the GPU instead of the CPU to exclude GameObjects from rendering when they're occluded behind other GameObjects.|

## Additional resources

- [Graphics performance fundamentals](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)
