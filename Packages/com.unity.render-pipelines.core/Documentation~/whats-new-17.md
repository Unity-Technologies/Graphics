# What's new in SRP Core version 17 / Unity 6

This page contains an overview of new features, improvements, and issues resolved in version 17 of the Scriptable Render Pipeline (SRP) Core package, embedded in Unity 6.

## Improvements

### Render graph system optimization

The render graph system API and compiler are now carefully optimized to reduce their cost on the main CPU thread. To prevent Unity compiling the render graph each frame, there's now a caching system so Unity only compiles when the rendering is different from the previous frame. This means performance on the main CPU thread should be faster, especially in non-development builds.

### Native Render Pass support in the render graph system

The render graph system API now provides automatic Native Render Pass support using the `AddRasterRenderPass` API. This means Unity can use framebuffer fetch operations on platforms with tile-based GPUs, which improves performance. 

Native Render Pass support is implemented in the Universal Render Pipeline (URP). For more information, refer to [Render graph system](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/render-graph.html) in the URP manual.

**Note:** You can't use the `AddRasterRenderPass` API with the existing `AddRenderPass` API. Instead, use the new `AddComputePass` and `AddUnsafePass` APIs.
