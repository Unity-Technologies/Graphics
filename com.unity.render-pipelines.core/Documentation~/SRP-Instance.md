# SRP Instance
The [SRP Asset](SRP-Asset.md) controls configuration, but the SRP Instance is the rendering entry point. When developing an SRP, you need to also create this class as this is where all the rendering logic should be.

In it's simplest form, the SRP Instance just contains a single function, **Render**, the best way to think of this is that it's a blank canvas where you are free to perform rendering in any way that you see fit. The **Render** function takes takes two arguments

* A `ScriptableRenderContext` which is a type of Command Buffer where you can enqueue rendering operations to be performed.
* A set of `Camera`s that to use for rendering.

## A basic pipeline
The SRP Asset example from [here](SRP-Asset.html) returns an SRP Instance, this pipeline might look like what is below. 

```C#
public class BasicPipeInstance : RenderPipeline
{
    private Color m_ClearColor = Color.black;

    public BasicPipeInstance(Color clearColor)
    {
        m_ClearColor = clearColor;
    }

    public override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // does not so much yet :()
        base.Render(context, cameras);

        // clear buffers to the configured color
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, m_ClearColor);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();
        context.Submit();
    }
}
```

What this pipeline does is perform a simple clear the screen to the given clear colour that is set in the SRP Asset when Unity creates the the SRP Instance. There are a few things to note here:

* SRP uses existing Unity `CommandBuffers` for many operations (`ClearRenderTarget` in this case).
* SRP schedules CommandBuffers against the context passed in.
* The final step of rendering in SRP is to call `Submit`. This executes all the queued up commands on the render context.

The `RenderPipeline`'s **Render** function is where you enter the rendering code for your custom renderer. It is here that you perform steps like Culling, Filtering, Changing render targets, and Drawing.