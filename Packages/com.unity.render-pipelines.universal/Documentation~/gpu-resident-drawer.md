
# Use the GPU Resident Drawer

The GPU Resident Drawer automatically uses the [`BatchRendererGroup`](https://docs.unity3d.com/Manual/batch-renderer-group.html) API to draw GameObjects with GPU instancing, which reduces the number of draw calls and frees CPU processing time. For more information, refer to [How BatchRendererGroup works](https://docs.unity3d.com/Manual/batch-renderer-group-how.html).

The GPU Resident Drawer works only with the following:

- The [Forward+](rendering/forward-plus-rendering-path.md) rendering path.
- [Graphics APIs](https://docs.unity3d.com/6000.0/Documentation/Manual/GraphicsAPIs.html) and platforms that support compute shaders.
- GameObjects that have a [Mesh Renderer component](https://docs.unity3d.com/Manual/class-MeshRenderer.html).

Otherwise, Unity falls back to drawing the GameObject without GPU instancing.

If you enable the GPU Resident Drawer, the following applies:

- Build times are longer because Unity compiles all the `BatchRendererGroup` shader variants into your build.

## Enable the GPU Resident Drawer

Follow these steps:

1. Go to **Project Settings** > **Graphics**, then in the **Shader Stripping** section set **BatchRendererGroup Variants** to **Keep All**.
2. Go to the active [URP Asset](universalrp-asset.md) and enable **SRP Batcher**.
3. Double-click the renderer in the **Renderer List** to open the Universal Renderer, then set **Rendering Path** to **Forward+**.
4. Set **GPU Resident Drawer** to **Instanced Drawing**.

If you change or create GameObjects each frame, the GPU Resident Drawer updates with the changes.

To include or exclude GameObjects from the GPU Resident Drawer, refer to [Make a GameObject compatible with the GPU Resident Drawer](make-object-compatible-gpu-rendering.md).

## Analyze the GPU Resident Drawer

To analyze the results of the GPU Resident Drawer, you can use the following:

- [Frame Debugger](https://docs.unity3d.com/Manual/FrameDebugger.html). The GPU Resident Drawer groups GameObjects into draw calls with the name **Hybrid Batch Group**.
- [Rendering Debugger](features/rendering-debugger.md#gpu-resident-drawer).
- [Rendering Statistics](https://docs.unity3d.com/Manual/RenderingStatistics.html) to check if the number of frames per second increases, and the CPU processing time and SetPass calls decreases.
- [Unity Profiler](optimize-for-better-performance.md)

## Optimize the GPU Resident Drawer

How much the GPU Resident Drawer speeds up rendering depends on your scene. The GPU Resident Drawer is most effective in the following setups:

- The scene is large.
- Multiple GameObjects use the same mesh, so Unity can group them into a single draw call.

Rendering usually speeds up less in the Scene view and the Game view, compared to Play mode or your final built project.

The following might speed up the GPU Resident Drawer:

- Go to **Project Settings** > **Player**, then in the **Other Settings** section disable **Static Batching**.
- Go to **Window** > **Panels** > **Lighting**, then in the **Lightmapping Settings** section enable **Fixed Lightmap Size** and disable **Use Mipmap Limits**.

## Additional resources

- [Reduce rendering work on the CPU](reduce-rendering-work-on-cpu.md)
- [Graphics performance fundamentals](https://docs.unity3d.com/Manual/OptimizingGraphicsPerformance.html)
- [GPU occlusion culling](gpu-culling.md)
