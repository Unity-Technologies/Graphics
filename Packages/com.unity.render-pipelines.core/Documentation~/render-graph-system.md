# Render graph system in SRP Core

The render graph system is a set of APIs you can use to write a custom scriptable render pipeline in a modular, maintainable way. A render graph is a high-level representation of the render passes in the pipeline. 

The render graph system automatically optimizes the render pipeline to minimize the number of render passes, and the memory and bandwidth the render passes use.

In each frame, you create a new instance of a render graph and follow these steps:

1. Set up the render passes. For each render pass, you tell the render graph system the textures or render textures to use (the recording stage) and the graphics commands to execute (the execution stage).
2. Compile the graph. The render graph removes render passes that don't contribute to the final frame, and calculates the lifetime of resources and the synchronization points between render passes.
3. Execute the graph. 

During execution, the render graph system allocates memory for resources before each render pass that uses them, then releases them if later render passes don't use them.

## Differences from a standard custom render pipeline

The render graph system differs from the standard workflow for [creating a custom render pipeline](srp-custom.md) in the following ways:

- You no longer handle resources directly and instead use handles such as `RTHandle` that are specific to the render graph system. This affects how to write shader code and how to set up render passes.
- Each render pass must state which resources it reads from and writes to.
- Resource references are only accessible within the execution code of a render pass.
- The resources you create inside one frame or render graph can't carry over to the next frame or render graph, unless you import them. For more information, refer to [Resources in the render graph system](render-graph-resources.md).

## How the render graph system optimizes rendering

The render graph system automatically optimizes the graph to minimize the number of render passes, and the memory and bandwidth the render passes use, in the following ways:

- Avoids allocating resources the frame doesn't use.
- Removes render passes if the final frame doesn't use their output.
- Optimizes GPU memory, for example by reusing allocated memory if a texture has the same properties as an earlier texture.
- Automatically synchronizes the compute and graphics GPU command queues, if you use compute shaders.

On mobile platforms that use tile-based deferred rendering (TBDR), render graph can also merge multiple render passes into a single native render pass. A native render pass keeps textures in tile memory, rather than copying textures from the GPU to the CPU. As a result, rendering uses less memory bandwidth and rendering time.

## Additional resources

- [The RTHandle system](rthandle-system.md)
