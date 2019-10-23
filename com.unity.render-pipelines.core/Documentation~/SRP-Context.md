# The SRP Context
SRP renders using the concept of delayed execution. You build up a list of commands and then execute them. The object that you use to build up these commands is called the `ScriptableRenderContext` and is passed as an argument to the **Render** function.

When you populate the SRP Context with operations, you can then call **Submit** to submit all the queued up rendering calls, which are generally a combination of `CommandBuffer` executions as well as SRP specific draw commands.

An example of this is using a `CommandBuffer` to clear a render target.

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
        // does not so much yet :(
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

For more information about the Scriptable Render Context, see the [API documentation](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html).