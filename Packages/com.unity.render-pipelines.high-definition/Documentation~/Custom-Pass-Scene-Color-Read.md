# Scene Color Sampling in AfterPostProcess Custom Pass

## Overview
When using AfterPostProcess injection point with FullScreenShaderGraph that samples scene color, you might need to handle concurrent read/write operations on the color buffer. This page explains the recommended implementation for proper scene color sampling.

## Technical Details
At the AfterPostProcess injection point, the color buffer serves as both the render target and the source for scene color sampling. To ensure correct sampling, you can implement a solution using a temporary buffer.

## Implementation Example

Here's an example of how to properly sample scene color in an AfterPostProcess Custom Pass:

```c#
class SceneColorSamplingPass : CustomPass
{
    RTHandle m_TempColorBuffer;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        // Create temporary buffer with the same properties as camera color buffer
        m_TempColorBuffer = RTHandles.Alloc(
            Vector2.one, TextureXR.slices, dimension: TextureXR.dimension,
            colorFormat: GetColorBufferFormat(),
            name: "SceneColorSamplingBuffer"
        );
    }

    protected override void Execute(CustomPassContext ctx)
    {
        // Copy the scene color to temporary buffer
        ctx.cmd.CopyTexture(ctx.cameraColorBuffer, m_TempColorBuffer);
        
        // Bind temporary buffer for sampling
        ctx.cmd.SetGlobalTexture("_AfterPostProcessColorBuffer", m_TempColorBuffer);
        
        // Your custom pass rendering code here
        // ...
    }

    protected override void Cleanup()
    {
        // Release the temporary buffer
        RTHandles.Release(m_TempColorBuffer);
    }
}
```
## Performance Considerations

When implementing this solution, keep in mind:
- This approach allocates an additional full-resolution color buffer
- Only implement this solution when you need to sample the scene color
- Consider using a different injection point if your use case allows it

## See Also
- [Custom Pass Injection Points](Custom-Pass-Injection-Points.md)
- [Custom Pass Volume Workflow](Custom-Pass-Volume-Workflow.md)
- [Full Screen Custom Pass](custom-pass-create-gameobject.md)

