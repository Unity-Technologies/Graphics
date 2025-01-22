# Write a Scriptable Render Pass in URP

To create a Scriptable Render Pass in the Universal Render Pipeline (URP), follow these steps:

1. Create a C# script that inherits the `ScriptableRenderPass` class. For example:

    ```C#
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    public class ExampleRenderPass : ScriptableRenderPass
    {
    }
    ```

2. In the class, add variables for the materials and textures you use in the render pass.

    For example, the following code sets up a handle to a texture, and a descriptor to configure the texture.

    ```c#
    private RTHandle textureHandle;
    private RenderTextureDescriptor textureDescriptor;
    ```

4. Override the `Configure` method to set up the render pass. Unity calls this method before executing the render pass.

    For example:

    ```c#
    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        //Set the texture size to be the same as the camera target size.
        textureDescriptor.width = cameraTextureDescriptor.width;
        textureDescriptor.height = cameraTextureDescriptor.height;

        //Check if the descriptor has changed, and reallocate the texture handle if necessary.
        RenderingUtils.ReAllocateIfNeeded(ref textureHandle, textureDescriptor);
    }
    ```

5. Override the `Execute` method with your rendering commands. Unity calls this method every frame, once for each camera.

    For example:

    ```c#
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // Get a CommandBuffer from pool
        CommandBuffer cmd = CommandBufferPool.Get();

        // Add rendering commands to the CommandBuffer
        ...

        // Execute the command buffer and release it back to the pool
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }
    ```

## Inject a render pass into the render loop

To inject a render pass into the render loop, refer to the following:

- [Use the `RenderPipelineManager` API](../customize/inject-render-pass-via-script.md)
- [Use a Scriptable Renderer Feature](scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md)

For a complete example, refer to [Example of a complete Scriptable Renderer Feature](../renderer-features/how-to-fullscreen-blit.md).

## Additional resources

- [Custom render pass workflow](../renderer-features/custom-rendering-pass-workflow-in-urp.md)
- [Writing custom shaders in URP](../writing-custom-shaders-urp.md)