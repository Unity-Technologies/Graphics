# Write a render pass using the render graph system

This page describes how to write a render pass using the render graph system.

To illustrate the description, the page uses the example render pass that copies the camera's active color texture to a destination texture. To simplify the code, this example does not use the destination texture elsewhere in the frame. You can use the frame debugger to inspect its contents.

## Declare a render pass

Declare a render pass as a class that inherits from the [ScriptableRenderPass](xref:UnityEngine.Rendering.Universal.ScriptableRenderPass) class.

## Declare resources that a render pass uses

Inside the render pass, declare a class that contains the resources that the render pass uses.

The resources can be regular C# variables and render graph resource references. The render graph system can access this data structure during the rendering code execution. Ensure that you declare only the variables that the render pass uses. Adding unnecessary variables can reduce performance.

```C#
class PassData
{
    internal TextureHandle copySourceTexture;
}
```

The [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method populates the data and the render graph passes it as a parameter to the rendering function.

## Declare a rendering function that generates the rendering commands for the render pass

Declare a rendering function that generates the rendering commands for the render pass. Further in this example, the [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method instructs the render graph to use the function using the `SetRenderFunc` method.

```C#
static void ExecutePass(PassData data, RasterGraphContext context)
{
    // Records a rendering command to copy, or blit, the contents of the source texture
    // to the color render target of the render pass.
    // The RecordRenderGraph method sets the destination texture as the render target
    // with the UseTextureFragment method.
    Blitter.BlitTexture(context.cmd, data.copySourceTexture,
        new Vector4(1, 1, 0, 0), 0, false);
}
```

## <a name="recordrendergraph"></a>Implement the RecordRenderGraph method

Use the [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method to add and configure one or more render passes in the render graph system. 

Unity calls this method during the render graph configuration step and lets you register relevant passes and resources for the render graph execution. Use this method to implement custom rendering.

In the [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method you declare render pass inputs and outputs, but do not add commands to command buffers.

The following section describes the main elements of the [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method and provides an example implementation.

## The render graph builder variable add frame resources

The `builder` variable is an instance of the `IRasterRenderGraphBuilder` interface. This variable is the entry point for configuring the information related to the render pass.

The [UniversalResourceData](xref:UnityEngine.Rendering.Universal.UniversalResourceData) class contains all the texture resources used by URP, including the active color and depth textures of the camera.

The [UniversalCameraData](xref:UnityEngine.Rendering.Universal.UniversalCameraData) class contains the data related to the currently active camera.

For demonstrative purposes, this sample creates a temporary destination texture. `UniversalRenderer.CreateRenderGraphTexture` is a helper method that calls the `RenderGraph.CreateTexture` method.

```C#
TextureHandle destination =
    UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "CopyTexture", false);
```

THe `builder.UseTexture` method declares that this render pass uses the source texture as a read-only input:

```C#
builder.UseTexture(passData.copySourceTexture);
```

In this example, the `builder.SetRenderAttachment` method declares that this render pass uses the temporary destination texture as its color render target. This declaration is similar to the `cmd.SetRenderTarget` API which you can use in the Compatibility mode (without the render graph API).

The `SetRenderFunc` method sets the `ExecutePass` method as the rendering function that render graph calls when executing the render pass. This sample uses a lambda expression to avoid memory allocations.

```C#
builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
```

The complete example of the [RecordRenderGraph](xref:UnityEngine.Rendering.Universal.RenderObjectsPass.RecordRenderGraph*) method:

```C#
// This method adds and configures one or more render passes in the render graph.
// This process includes declaring their inputs and outputs,
// but does not include adding commands to command buffers.
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
{
    string passName = "Copy To Debug Texture";

    // Add a raster render pass to the render graph. The PassData type parameter determines
    // the type of the passData output variable.
    using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName,
        out var passData))
    {
        // UniversalResourceData contains all the texture references used by URP,
        // including the active color and depth textures of the camera.
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

        // Populate passData with the data needed by the rendering function
        // of the render pass.
        // Use the camera's active color texture
        // as the source texture for the copy operation.
        passData.copySourceTexture = resourceData.activeColorTexture;

        // Create a destination texture for the copy operation based on the settings,
        // such as dimensions, of the textures that the camera uses.
        // Set msaaSamples to 1 to get a non-multisampled destination texture.
        // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
        // creates a color texture and not a depth texture.
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
        desc.msaaSamples = 1;
        desc.depthBufferBits = 0;

        // For demonstrative purposes, this sample creates a temporary destination texture.
        // UniversalRenderer.CreateRenderGraphTexture is a helper method
        // that calls the RenderGraph.CreateTexture method.
        // Using a RenderTextureDescriptor instance instead of a TextureDesc instance
        // simplifies your code.
        TextureHandle destination =
            UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                "CopyTexture", false);

        // Declare that this render pass uses the source texture as a read-only input.
        builder.UseTexture(passData.copySourceTexture);

        // Declare that this render pass uses the temporary destination texture
        // as its color render target.
        // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
        builder.SetRenderAttachment(destination, 0);

        // RenderGraph automatically determines that it can remove this render pass
        // because its results, which are stored in the temporary destination texture,
        // are not used by other passes.
        // For demonstrative purposes, this sample turns off this behavior to make sure
        // that render graph executes the render pass. 
        builder.AllowPassCulling(false);

        // Set the ExecutePass method as the rendering function that render graph calls
        // for the render pass. 
        // This sample uses a lambda expression to avoid memory allocations.
        builder.SetRenderFunc((PassData data, RasterGraphContext context)
            => ExecutePass(data, context));
    }
}
```

## Inject the scriptable render pass instance into the renderer

To inject the scriptable render pass instance into the renderer, use the `AddRenderPasses` method from a Renderer Feature implementation. URP calls the `AddRenderPasses` method every frame, once for each Camera.

```C#
public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
{
    renderer.EnqueuePass(m_CopyRenderPass);
}
```

## Additional resources

* [Example of a complete Scriptable Renderer Feature](renderer-features/create-custom-renderer-feature.md)

* [Write a Scriptable Render Pass in Compatibility Mode](renderer-features/write-a-scriptable-render-pass.md)