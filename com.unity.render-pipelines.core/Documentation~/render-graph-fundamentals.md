# Render graph fundamentals

This document describes the main principles behind a render graph and an overview of how Unity executes it.

## Main principles

There are a few things to know before you can write render passes with the [RenderGraph](../api/UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.html) API. The following principles are the foundation of how a render graph works.

- You no longer handle resources directly and instead use render graph system-specific handles. All RenderGraph APIs use these handles to manipulate resources. The resource types a render graph manages are [RTHandles](rthandle-system.md), [ComputeBuffers](https://docs.unity3d.com/ScriptReference/ComputeBuffer.html), and [RendererLists](../api/UnityEngine.Experimental.Rendering.RendererList.html).
- Actual resource references are only accessible within the execution code of a render pass.
- The framework requires an explicit declaration of render passes. Each render pass needs to state which resources it reads from and/or writes to.
- There is no persistence between each execution of a render graph. This means that the resources you create inside one execution of the render graph cannot carry over to the next execution.
- For resources that need persistence (from one frame to another for example), you can create them outside of a render graph, like regular resources, and import them in. They behave as any other render graph resource in terms of dependency tracking, but the graph does not handle their lifetime.
- A render graph mostly uses `RTHandles` for texture resources. This has a number of implications on how to write shader code and how to set them up.

## Resource Management

The render graph system calculates the lifetime of each resource using the high-level representation of the whole frame. This means that when you create a resource via the RenderGraph API, the render graph system does not actually create the resource at that time. Instead, the API returns a handle that represents the resource that you then use with all RenderGraph APIs. The render graph only creates the resource just before the first pass that needs to write it. In this case, “creating” does not necessarily mean the render graph system allocates resources, but rather that it provides the necessary memory to represent the resource so that it can use the resource during a render pass. In the same manner, it also releases the resource memory after the last pass that needs to read it. This way, the render graph system can reuse memory in the most efficient manner based on what you declare in your passes. This also means that if the render graph system does not execute a pass that requires a specific resource, then the system does not allocate the memory for the resource.

## Render graph execution overview

Render graph execution is a three-step process the render graph system completes, from scratch, every frame. This is because a graph can change dynamically from frame to frame, depending on the actions of the user for example.

### Setup

The first step is to set up all the render passes. This is where you declare all the render passes to execute as well as the resources each render pass uses.

### Compilation

The second step is to compile the graph. During this step, the render graph system culls render passes if no other render pass uses their outputs. This allows for more careless setups because you can reduce specific logic when you set up the graph. A good example of that is debug render passes. If you declare a render pass that produces a debug output that you don't present to the back buffer, the render graph system culls that pass automatically.


This step also calculates the lifetime of resources. This allows the render graph system to create and release resources in an efficient way as well as compute the proper synchronization points when it executes passes on the asynchronous compute pipeline.

### Execution

Finally, execute the graph. The render graph system executes all render passes, that it did not cull, in declaration order. Before each render pass, the render graph system creates the proper resources and releases them after the render pass if later render passes do not use them.