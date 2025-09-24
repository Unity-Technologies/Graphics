# Write a render pipeline with render graph in SRP Core

Create a custom render pipeline using the [render graph system](render-graph-system.md) in SRP Core.

**Note:** This page is about creating a custom render pipeline. To use the render graph system in a prebuilt Unity pipeline, refer to either [Render graph system in URP](https://docs.unity3d.com/Manual/urp/render-graph.html) or [Render graph system in HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest/index.html?subfolder=/manual/render-graph-introduction.html).

1. In your class that inherits `RenderPipeline`, create an instance of a [`RenderGraph`](../api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html). For example:

    ```c#
    public class MyRenderPipeline : RenderPipeline
    {
        ...

        RenderGraph myRenderGraph;
        myRenderGraph = new RenderGraph("MyRenderGraph");
    }
    ```

    **Note:**  To optimize memory usage, use only one instance. You can use more than one instance of a render graph, but Unity doesn't share resources across `RenderGraph` instances.

    For more information about creating a render pipeline class, refer to [Creating a custom render pipeline](srp-custom.md).

2. In your render loop, create the parameters to initialize the render graph, using the `RenderGraphParameters` API. For example:

    ```c#
    void RenderMyCamera(ScriptableRenderContext context, Camera cameraToRender)
    {
        context.SetupCameraProperties(cameraToRender);
        var cmd = CommandBufferPool.Get("ExampleCommandBuffer");

        RenderGraphParameters rgParams = new RenderGraphParameters
        {
            commandBuffer = cmd,
            scriptableRenderContext = context,
            currentFrameIndex = Time.frameCount,
        };
    }
    ```

    For more information about creating a render loop, refer to [Create a simple render loop in a custom render pipeline](srp-creating-simple-render-loop.md).


3. Initialize the render graph by calling the [`BeginRecording`](../api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html) method. For example:

    ```c#
    myRenderGraph.BeginRecording(renderGraphParams);
    ```

4. Run the render passes by calling the [`EndRecordingAndExecute`](../api/UnityEngine.Rendering.RenderGraphModule.RenderGraph.html) method. For example:

    ```c#
    myRenderGraph.BeginRecording(renderGraphParams);

    // Add render passes here.

    myRenderGraph.EndRecordingAndExecute();
    ```

    For more information about adding render passes, refer to [Write a render pass using the render graph system](render-graph-write-render-pass.md)

5. Execute the rendering commands. For more information, refer to [Execute rendering commands in a custom render pipeline](srp-using-scriptable-render-context.md).

6. In the render pipeline class, call the `Cleanup()` method on the RenderGraph instance to free the resources the render graph allocated. For example:

    ```c#
    myRenderGraph.Cleanup();
    myRenderGraph = null;
    ```

7. After the last camera renders, call the `EndFrame` method to deallocate any resources that the render graph hasn't used since the last frame.

    ```c#
    myRenderGraph.EndFrame();
    ```

## Example render pipeline template

This code template is simplified. It demonstrates the clearest workflow, rather than the most efficient runtime performance.

```c#
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

public class ExampleRenderPipeline : RenderPipeline
{
    private RenderGraph myRenderGraph;

    public ExampleRenderPipeline(ExampleRenderPipelineAsset asset)
    {
        myRenderGraph = new RenderGraph("My render graph");
    }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach (var cam in cameras)
        {
            if (cam.enabled)
            {
                RenderCamera(context, cam);
            }
        }
    }

    void RenderCamera(ScriptableRenderContext context, Camera cameraToRender)
    {
        context.SetupCameraProperties(cameraToRender);
        var cmd = CommandBufferPool.Get("Example command buffer");

        RenderGraphParameters rgParams = new RenderGraphParameters
        {
            commandBuffer = cmd,
            scriptableRenderContext = context,
            currentFrameIndex = Time.frameCount,
        };

        try
        {
            myRenderGraph.BeginRecording(rgParams);
            // Add render passes here.
            myRenderGraph.EndRecordingAndExecute();
        } 
        catch (Exception e)
        {
            if (renderGraph.ResetGraphAndLogException(e))
                throw;
        }

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
        context.Submit();            
    }
}
```
