# Introduction to the render graph system

The render graph system is a set of APIs you use to write a [Scriptable Render Pass](renderer-features/intro-to-scriptable-render-passes.md) in the Universal Render Pipeline (URP).

When you use the render graph API to create a Scriptable Render Pass, you tell URP the following:

1. The textures or render textures to use. This stage is the recording stage.
2. The graphics commands to execute, using the textures or render textures from the recording stage. This stage is the execution stage.

You can then [add your Scriptable Render Pass to the URP renderer](renderer-features/custom-rendering-pass-workflow-in-urp.md). Your Scriptable Render Pass becomes part of URP's internal render graph, which is the sequence of render passes URP steps through each frame. URP automatically optimizes your render pass and the render graph to minimize the number of render passes, and the memory and bandwidth the render passes use.

## How URP optimizes rendering

URP does the following to optimize rendering in the render graph:

- Merges multiple render passes into a single render pass.
- Avoids allocating resources the frame doesn't use.
- Avoids executing render passes if the final frame doesn't use their output.
- Avoids duplicating resources, for example by replacing two texture that have the same properties with a single texture.
- Automatically synchronizes the compute and graphics GPU command queues.

On mobile platforms that use tile-based deferred rendering (TBDR), URP can also merge multiple render passes into a single native render pass. A native render pass keeps textures in tile memory, rather than copying textures from the GPU to the CPU. As a result, URP uses less memory bandwidth and rendering time.

To check how URP optimizes rendering in your custom render passes, refer to [Analyze a render graph](render-graph-view.md).

## Additional resources

- [Use frame data](accessing-frame-data.md)
- [Transfer a texture between render passes](render-graph-pass-textures-between-passes.md)
- [Inject a render pass via scripting](customize/inject-render-pass-via-script.md)
- [Inject a render pass using a Scriptable Renderer Feature](renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)
- [The render graph system](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/render-graph-system.html) in the Scriptable Render Pipeline (SRP) Core manual.
- [CommandBuffer.BeginRenderPass](https://docs.unity3d.com/2023.3/Documentation/ScriptReference/Rendering.CommandBuffer.BeginRenderPass.html)
