# Writing a render pipeline

This page covers the process of how to use the RenderGraph API to write a render pipeline. For information about the RenderGraph API, see [render graph system](render-graph-system.md) and [render graph fundamentals](render-graph-fundamentals.md).

### Initialization and cleanup of Render Graph

To begin, your render pipeline needs to maintain at least one instance of [RenderGraph](../api/UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.html). This is the main entry point for the API. You can use more than one instance of a render graph, but be aware that Unity does not share resources across `RenderGraph` instances so for optimal memory usage, only use one instance.

```c#
using UnityEngine.Experimental.Rendering.RenderGraphModule;

public class MyRenderPipeline : RenderPipeline
{
    RenderGraph m_RenderGraph;

    void InitializeRenderGraph()
    {
        m_RenderGraph = new RenderGraph(“MyRenderGraph”);
    }

    void CleanupRenderGraph()
    {
        m_RenderGraph.Cleanup();
          m_RenderGraph = null;
    }
}
```

To initialize a `RenderGraph` instance, call the constructor with an optional name to identify the render graph. This also registers a render graph-specific panel in the SRP Debug window which is useful for debugging the RenderGraph instance. When you dispose of a render pipeline, call the `Cleanup()` method on the RenderGraph instance to properly free all the resources the render graph allocated.

### Starting a render graph

Before you add any render passes to the render graph, you first need to initialize the render graph. To do this, call the `Begin` method. For details about this method's parameters, see the [API documentation](../api/UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.html)

```c#
var renderGraphParams = new RenderGraphExecuteParams()
{
    scriptableRenderContext = renderContext,
    commandBuffer = cmd,
    currentFrameIndex = frameIndex
};

m_RenderGraph.Begin(renderGraphParams);
```

### Creating resources for the render graph

When you use a render graph, you never directly allocate resources yourself. Instead, the RenderGraph instance handles the allocation and disposal of its own resources. To declare resources and use them in a render pass, you use render graph specific APIs that return handles to the resource.

There are two main types of resources that a render graph uses:

- **Internal resources**: These resources are internal to a render graph execution and you cannot access them outside of the RenderGraph instance. You also cannot pass these resources from one execution of a graph to another. The render graph handles the lifetime of these resources.
- **Imported resources**: These usually come from outside the render graph execution. Typical examples are the back buffer (provided by the camera) or buffers that you want the graph to use across multiple frames for temporal effects (like using the camera color buffer for temporal anti-aliasing). You are responsible for handling the lifetime of these resources.

After you create or import a resource, the render graph system represents it as a resource type-specific handle (`TextureHandle`, `ComputeBufferHandle`, or `RendererListHandle`). This way, the render graph can use internal and imported resources in the same way in all of its APIs.

```c#
public TextureHandle RenderGraph.CreateTexture(in TextureDesc desc);
public ComputeBufferHandle RenderGraph.CreateComputeBuffer(in ComputeBufferDesc desc)
public RendererListHandle RenderGraph.CreateRendererList(in RendererListDesc desc);

public TextureHandle RenderGraph.ImportTexture(RTHandle rt);
public TextureHandle RenderGraph.ImportBackbuffer(RenderTargetIdentifier rt);
public ComputeBufferHandle RenderGraph.ImportComputeBuffer(ComputeBuffer computeBuffer);
```

The main ways to create resources are described above, but there are variations of these functions. For the complete list, see the [API documentation](../api/UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph.html). Note that the specific function to use to import the camera back buffer is `RenderTargetIdentifier`.

To create resources, each API requires a descriptor structure as a parameter. The properties in these structures are similar to the properties in the resources they represent (respectively [RTHandle](rthandle-system.md), [ComputeBuffer](https://docs.unity3d.com/ScriptReference/ComputeBuffer.html), and [RendererLists](../api/UnityEngine.Experimental.Rendering.RendererList.html)). However, some properties are specific to render graph textures.

Here are the most important ones:

- **clearBuffer**: This property tells the graph whether to clear the buffer when the graph creates it. This is how you should clear textures when using the render graph. This is important because a render graph pools resources, which means any pass that creates a texture might get an already existing one with undefined content.

- **clearColor**: This property stores the color to clear the buffer to, if applicable.

There are also two notions specific to textures that a render graph exposes through the `TextureDesc` constructors:

- **xrReady**: This boolean indicates to the graph whether this texture is for XR rendering. If true, the render graph creates the texture as an array for rendering into each XR eye.
- **dynamicResolution**: This boolean indicates to the graph whether it needs to dynamically resize this texture when the application uses dynamic resolution. If false, the texture does not scale automatically.

You can create resources outside render passes, inside the setup code for a render pass, but not in the rendering code.

Creating a resource outside of all render passes can be useful for cases where the first pass uses a given resource that depends on logic in the code that might change regularly. In this case, you must create the resource before any of those passes. A good example is using the color buffer for either a deferred lighting pass or a forward lighting pass. Both of these passes write to the color buffer, but Unity only executes one of them depending on the current rendering path chosen for the camera. In this case, you would create the color buffer outside both passes and pass it to the correct one as a parameter.


Creating a resource inside a render pass is usually for resources the render pass produces itself. For example, a blur pass requires an already existing input texture, but creates the output itself and returns it at the end of the render pass.

Note that creating a resource like that does not allocate GPU memory every frame. Instead, the render graph system reuses pooled memory. In the context of the render graph, think of resource creation more in terms of data flow in the context of a render pass than actual allocation. If a render pass creates a whole new output then it “creates” a new texture in the render graph.

### Writing a render pass

Before Unity can execute the render graph, you must declare all the render passes. You write a render pass in two parts: setup and rendering.

#### Setup

During setup, you declare the render pass and all the data it needs to execute. The render graph represents data by a class specific to the render pass that contains all the relevant properties. These can be regular C# constructs (struct, PoDs, etc) and render graph resource handles. This data structure is accessible during the actual rendering code.

```c#
class MyRenderPassData
{
    public float parameter;
    public TextureHandle inputTexture;
    public TextureHandle outputTexture;
}
```

After you define the pass data, you can then declare the render pass itself:

```c#
using (var builder = renderGraph.AddRenderPass<MyRenderPassData>("My Render Pass", out var passData))
{
        passData.parameter = 2.5f;
    passData.inputTexture = builder.ReadTexture(inputTexture);

    TextureHandle output = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.black, name = "Output" });
    passData.outputTexture = builder.WriteTexture(output);

    builder.SetRenderFunc(myFunc); // details below.
}
```

You define the render pass in the `using` scope around the `AddRenderPass` function. At the end of the scope, the render graph adds the render pass to the internal structures of the render graph for later processing.



The `builder` variable is an instance of `RenderGraphBuilder`. This is the entry point to build the information relating to the render pass. There are several important parts to this:

- **Declaring resource usage**: This is one of the most important aspects of the RenderGraph API. Here you explicitly declare whether the render pass needs read and/or write access to the resources. This allows the render graph to have an overall view of the whole rendering frame and thus determine the best use of GPU memory and synchronization points between various render passes.
- **Declaring the rendering function**: This is the function in which you call graphics commands. It receives the pass data you define for the render pass as a parameter as well as the render graph context. You set the rendering function for a render pass via `SetRenderFunc` and the function runs after the graph compiles.
- **Creating transient resources**: Transient, or internal, resources are resources you create for the duration of this render pass only. You create them in the builder rather than the render graph itself to reflect their lifetime. Creating transient resources uses the same parameters as the equivalent function in the RenderGraph APIs. This is particularly useful when a pass uses temporary buffers that should not be accessible outside of the pass. Outside the pass where you declare a transient resource, the handle to the resource becomes invalid and Unity throws errors if you try to use it.

The `passData` variable is an instance of the type you provide when you declare the pass. This is where you set the data that the rendering code can access. Note that the render graph does not use the contents of `passData` right away, but later in the frame, after it registers all the passes and the render graph compiles and executes. This means that any reference the `passData` stores must be constant across the whole frame. Otherwise, if you change the content before the render pass executes, it does not contain the correct content during the render pass. For this reason, it is best practice to only store value types in the `passData` unless you are certain that a reference stays constant until the pass finishes execution.

For an overview of the `RenderGraphBuilder` APIs, see the below table. For more details, see the API documentation:

| Function                                                     | Purpose                                                      |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| TextureHandle ReadTexture(in TextureHandle input)            | Declares that the render pass reads from the `input` texture you pass into the function. |
| TextureHandle WriteTexture(in TextureHandle input)           | Declares that the render pass writes to the `input` texture you pass into the function. |
| TextureHandle UseColorBuffer(in TextureHandle input, int index) | Same as `WriteTexture` but also automatically binds the texture as a render texture at the provided binding index at the beginning of the pass. |
| TextureHandle UseDepthBuffer(in TextureHandle input, DepthAccess flags) | Same as `WriteTexture` but also automatically binds the texture as a depth texture with the access flags you pass into the function. |
| TextureHandle CreateTransientTexture(in TextureDesc desc)    | Create a transient texture. This texture exists for the duration of the pass. |
| RendererListHandle UseRendererList(in RendererListHandle input) | Declares that this render pass uses the Renderer List you pass in. The render pass uses the `RendererList.Draw` command to render the list. |
| ComputeBufferHandle ReadComputeBuffer(in ComputeBufferHandle input) | Declares that the render pass reads from the `input` ComputeBuffer you pass into the function. |
| ComputeBufferHandle WriteComputeBuffer(in ComputeBufferHandle input) | Declares that the render pass writes to the `input` Compute Buffer you pass into the function. |
| ComputeBufferHandle CreateTransientComputeBuffer(in ComputeBufferDesc desc) | Create a transient Compute Buffer. This texture exists for the duration of the Compute Buffer. |
| void SetRenderFunc<PassData>(RenderFunc<PassData> renderFunc) where PassData : class, new() | Set the rendering function for the render pass.              |
| void EnableAsyncCompute(bool value)                          | Declares that the render pass runs on the asynchronous compute pipeline. |
| void AllowPassCulling(bool value)                            | Specifies whether Unity should cull the render pass (default is true). This can be useful when the render pass has side effects and you never want the render graph system to cull. |

#### Rendering Code

After you complete the setup, you can declare the function to use for rendering via the `SetRenderFunc` method on the `RenderGraphBuilder`. The function you assign must use the following signature:

```c#
delegate void RenderFunc<PassData>(PassData data, RenderGraphContext renderGraphContext) where PassData : class, new();
```

You can either pass a render function as a `static` function or a lambda. The benefit of using a lambda function is that it can bring better code clarity because the rendering code is next to the setup code.

Note that if you use a lambda, be very careful not to capture any parameters from the main scope of the function as that generates garbage, which Unity later locates and frees during garbage collection. If you use Visual Studio and hover over the arrow **=>**, it tells you if the lambda captures anything from the scope. Avoid accessing members or member functions because using either captures `this`.

The render function takes two parameters:

- `PassData data`: This data is of the type you pass in when you declare the render pass. This is where you can access the properties initialized during the setup phase and use them for the rendering code.
- `RenderGraphContext renderGraphContext`. This stores references to the `ScriptableRenderContext` and the `CommandBuffer` that provide utility functions and allow you to write rendering code.

##### Accessing resources in the render pass

Inside the rendering function, you can access all the render graph resource handles stored inside the `passData`. The conversion to actual resources is automatic so, whenever a function needs an RTHandle, a ComputeBuffer, or a RendererList, you can pass the handle and the render graph converts the handle to the actual resource implicitly. Note that doing such implicit conversion outside of a rendering function results in an exception. This exception occurs because, outside of rendering, the render graph may have not allocated those resources yet.

##### Using the RenderGraphContext

The RenderGraphContext provides various functionality you need to write rendering code. The two most important are the `ScriptableRenderContext` and the `CommandBuffer`, which you use to call all rendering commands.

The RenderGraphContext also contains the `RenderGraphObjectPool`. This class helps you to manage temporary objects that you might need for rendering code.

##### Get temp functions

Two functions that are particularly useful during render passes are `GetTempArray` and `GetTempMaterialPropertyBlock`.

```c#
T[] GetTempArray<T>(int size);
MaterialPropertyBlock GetTempMaterialPropertyBlock();
```

`GetTempArray` returns a temporary array of type `T` and size `size`. This can be useful to allocate temporary arrays for passing parameters to materials or creating a `RenderTargetIdentifier` array to create multiple render target setups without the need to manage the array’s lifetime yourself.

`GetTempMaterialPropertyBlock` returns a clean material property block that you can use to set up parameters for a Material. This is particularly important because more than one pass might use a material and each pass could use it with different parameters. Because the rendering code execution is deferred via command buffers, copying material property blocks into the command buffer is mandatory to preserve data integrity on execution.

The render graph releases and pools all the resources these two functions return automatically after the pass execution. This means you don’t have to manage them yourself and does not create garbage.

#### Example render pass

The following code example contains a render pass with a setup and render function:

```c#
TextureHandle MyRenderPass(RenderGraph renderGraph, TextureHandle inputTexture, float parameter, Material material)
{
    using (var builder = renderGraph.AddRenderPass<MyRenderPassData>("My Render Pass", out var passData))
    {
        passData.parameter = parameter;
        passData.material = material;

        // Tells the graph that this pass will read inputTexture.
        passData.inputTexture = builder.ReadTexture(inputTexture);

        // Creates the output texture.
        TextureHandle output = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.black, name = "Output" });
        // Tells the graph that this pass will write this texture and needs to be set as render target 0.
        passData.outputTexture = builder.UseColorBuffer(output, 0);

        builder.SetRenderFunc(
        (MyRenderPassData data, RenderGraphContext ctx) =>
        {
            // Render Target is already set via the use of UseColorBuffer above.
            // If builder.WriteTexture was used, you'd need to do something like that:
            // CoreUtils.SetRenderTarget(ctx.cmd, data.output);

            // Setup material for rendering
            var materialPropertyBlock = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
            materialPropertyBlock.SetTexture("_MainTexture", data.input);
            materialPropertyBlock.SetFloat("_FloatParam", data.parameter);

            CoreUtils.DrawFullScreen(ctx.cmd, data.material, materialPropertyBlock);
        });

        return output;
    }
}
```

### Execution of the Render Graph

After you declare all the render passes, you then need to execute the render graph. To do this, call the Execute method.

```c#
m_RenderGraph.Execute();
```

This triggers the process that compiles and executes the render graph.

### Ending the frame

Over the course of your application, the render graph needs to allocate various resources. It might use these resources for a time but then might not need them. For the graph to free up those resources, call the `EndFrame()` method once a frame. This deallocates any resources that the render graph has not used since the last frame. This also executes all internal processing the render graph requires at the end of the frame.

Note that you should only call this once per frame and after all the rendering is complete (for example, after the last camera renders). This is because different cameras might have different rendering paths and thus need different resources. Calling the purge after each camera could result in the render graph releasing resources too early even though they might be necessary for the next camera.
