# Use the CommandBuffer interface in a render graph in SRP Core

You can use the render graph `AddUnsafePass` API to use [CommandBuffer](ScriptRef:Rendering.CommandBuffer) interface APIs such as `SetRenderTarget` in render graph system render passes.

The `AddUnsafePass` API gives access to the [UnsafeCommandBuffer](ScriptRef:Rendering.UnsafeCommandBuffer), which gives access to more `CommandBuffer` functions than the [RasterCommandBuffer](ScriptRef:Rendering.RasterCommandBuffer) API.

**Note**: Both `AddUnsafePass` and `RasterCommandBuffer` restrict access to certain `commandBuffer` functions so that `RenderGraph` can better optimize the frame. The most important function `UnsafeCommandbuffer` gives you access to is `SetRenderTarget`.

If you use the `AddUnsafePass` API, the following applies:

- You can't use the `SetRenderAttachment` method in the `RecordRenderGraph` method. Use `SetRenderTarget` in the `SetRenderFunc` method instead.
- Rendering might be slower because the render graph system can't optimize the render pass. For example, if your render pass writes to the active color buffer, the render graph system can't detect if a later render pass writes to the same buffer. As a result, the render graph system can't merge the two render passes, and the GPU unnecessarily transfers the buffer in and out of memory.

## Create an unsafe render pass

To create an unsafe render pass, follow these steps:

1. In your `RecordRenderGraph` method, use the `AddUnsafePass` method instead of the `AddRasterRenderPass` method.

    For example:

    ``` lang-cs
    using (var builder = renderGraph.AddUnsafePass<PassData>("My unsafe render pass", out var passData))
    ```

2. When you call the `SetRenderFunc` method, use the `UnsafeGraphContext` type instead of `RasterGraphContext`.

    For example:

    ``` lang-cs
    builder.SetRenderFunc(
       static (PassData passData, UnsafeGraphContext context) => ExecutePass(passData, context)
    );
    ```

3. If your render pass writes to a texture, add the texture as a field in your pass data class.

    For example:

    ``` lang-cs
    private class PassData
    {
        internal TextureHandle textureToWriteTo;
    }
    ```

4. If your render pass writes to a texture, set the texture as writeable using the `UseTexture` method.

    For example:

    ``` lang-cs
    builder.UseTexture(passData.textureToWriteTo, AccessFlags.Write);
    ```

You can now use CommandBuffer interface APIs in your `SetRenderFunc` method. For example:

```lang-cs
static void ExecutePass(PassData passData, UnsafeGraphContext context)
{
    // Add a command to set the render target to the active color buffer so render graph draws to it
    context.cmd.SetRenderTarget(passData.activeColorBuffer);
}
```