---
uid: um-srp-using-scriptable-render-context
---

# Execute rendering commands in a custom render pipeline

This page explains how to schedule and execute rendering commands in the Scriptable Render Pipeline (SRP)ScriptableRenderPipeline, either by using CommandBuffers or by making direct API calls to the ScriptableRenderContext. The information on this page is applicable to the Universal Render Pipeline (URP), the High Definition Render Pipeline (HDRP), and custom render pipelines that are based on SRP.

In SRP, you use C# scripts to configure and schedule rendering commands. You then tell Unity's low-level graphics architecture to execute them, which sends instructions to the graphics API.

The main way of doing this is by making API calls to the ScriptableRenderContext, but you can also execute CommandBuffers immediately.

## Using the ScriptableRenderContext APIs

In SRP, the ScriptableRenderContext class acts as an interface between the C# render pipeline code and Unity's low-level graphics code. SRP rendering works using delayed execution; you use the ScriptableRenderContext to build up a list of rendering commands, and then you tell Unity to execute them. Unity's low-level graphics architecture then sends instructions to the graphics API.

To schedule rendering commands, you can:

* Pass [CommandBuffers](xref:UnityEngine.Rendering.CommandBuffer) to the ScriptableRenderContext, using [ScriptableRenderContext.ExecuteCommandBuffer](xref:UnityEngine.Rendering.ScriptableRenderContext.ExecuteCommandBuffer(UnityEngine.Rendering.CommandBuffer))
* Make direct API calls to the Scriptable Render Context, such as [ScriptableRenderContext.Cull](xref:UnityEngine.Rendering.ScriptableRenderContext.Cull(UnityEngine.Rendering.ScriptableCullingParameters&)) or [ScriptableRenderContext.DrawRenderers](xref:UnityEngine.Rendering.ScriptableRenderContext.DrawRenderers(UnityEngine.Rendering.CullingResults,UnityEngine.Rendering.DrawingSettings&,UnityEngine.Rendering.FilteringSettings&)) 

To tell Unity to perform the commands that you have scheduled, call [ScriptableRenderContext.Submit](xref:UnityEngine.Rendering.ScriptableRenderContext.Submit). Note that it does not matter whether you used a CommandBuffer to schedule the command, or whether you scheduled the command by calling an API; Unity schedules all rendering commands on the ScriptableRenderContext in the same way, and does not execute any of them until you call `Submit()`.

This example code demonstrates how to schedule and perform a command to clear the current render target, using a CommandBuffer.

```lang-csharp
using UnityEngine;
using UnityEngine.Rendering;

public class ExampleRenderPipeline : RenderPipeline
{
        public ExampleRenderPipeline() {
        }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras) {
        // Create and schedule a command to clear the current render target
        var cmd = new CommandBuffer();
        cmd.ClearRenderTarget(true, true, Color.red);
        context.ExecuteCommandBuffer(cmd);
        cmd.Release();

         // Tell the Scriptable Render Context to tell the graphics API to perform the scheduled commands
        context.Submit();
    }
}
```

## Executing CommandBuffers immediately

You can execute CommandBuffers immediately without using the ScriptableRenderContext, by calling [Graphics.ExecuteCommandBuffer](xref:UnityEngine.Graphics.ExecuteCommandBuffer(UnityEngine.Rendering.CommandBuffer)). Calls to this API take place outside of the render pipeline.

## Additional information

For more information on commands that you can schedule using CommandBuffers, see [CommandBuffers API documentation](xref:UnityEngine.Rendering.CommandBuffer).
