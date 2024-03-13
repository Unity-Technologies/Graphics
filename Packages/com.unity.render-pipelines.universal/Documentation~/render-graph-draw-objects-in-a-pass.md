# Draw objects in a render pass

To draw objects in a custom render pass that uses the render graph system, use the `RendererListHandle` API to create a list of objects to draw.

## Create a list of objects to draw

Follow these steps:

1. In your `ScriptableRenderPass` class, in the class you use for pass data, create a [`RendererListHandle`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.RenderGraphModule.RendererListHandle.html) field.

    For example:

    ```csharp
    private class PassData
    {
        public RendererListHandle objectsToDraw;
    }
    ```

2. Create a [RendererListParams](https://docs.unity3d.com/ScriptReference/Rendering.RendererListParams.html) object that contains the objects to draw, drawing settings, and culling data. Refer to [Creating a simple render loop in a custom render pipeline](https://docs.unity3d.com/Manual/srp-creating-simple-render-loop.html) for more information.

    Refer to [Example](#example) below for a detailed example.

3. In the `RecordRenderGraph` method, use the [`CreateRendererList` API](https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.CreateRendererList.html) to convert the `RendererListParams` object to a handle that the render graph system can use.

    For example:

    ```csharp
    RenderListHandle rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
    ```

4. Set the `RendererListHandle` field in the pass data.

    For example:

    ```csharp
    passData.objectsToDraw = rendererListHandle;
    ```

## Draw the objects

After you set a `RendererListHandle` in the pass data, you can draw the objects in the list.

Follow these steps:

1. In the `RecordRenderGraph` method, tell the render graph system to use the list of objects, using the `UseRendererList` API.

    For example:

    ```csharp
    builder.UseRendererList(passData.rendererListHandle);
    ```

2. Set the texture to draw the objects onto. Set both the color texture and the depth texture so URP renders the objects correctly.

    For example, the following tells URP to draw to the color texture and depth texture of the active camera texture.

    ```csharp
    UniversalResourceData frameData = frameContext.Get<UniversalResourceData>();
    builder.SetRenderAttachment(frameData.activeColorTexture, 0);
    builder.SetRenderAttachmentDepth(frameData.activeDepthTexture, AccessFlags.Write);
    ```

3. In your `SetRenderFunc` method, draw the renderers using the [`DrawRendererList`](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.DrawRendererList.html) API.
    
    For example:

    ```csharp
    context.cmd.DrawRendererList(passData.rendererListHandle);
    ```

## Example

The following Scriptable Renderer Feature redraws the objects in the scene that have their `Lightmode` tag set to `UniversalForward`, using an override material.

After you [add this Scriptable Reader Feature to the renderer](renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md), set the **Material To Use** parameter to any material.

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
 
public class DrawObjectsWithOverrideMaterial : ScriptableRendererFeature
{

    DrawObjectsPass drawObjectsPass;
    public Material overrideMaterial;
 
    public override void Create()
    {
        // Create the render pass that draws the objects, and pass in the override material
        drawObjectsPass = new DrawObjectsPass(overrideMaterial);

        // Insert render passes after URP's post-processing render pass
        drawObjectsPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }
 
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Add the render pass to the URP rendering loop
        renderer.EnqueuePass(drawObjectsPass);
    }

    class DrawObjectsPass : ScriptableRenderPass
    {
        private Material materialToUse;

        public DrawObjectsPass(Material overrideMaterial)
        {
            // Set the pass's local copy of the override material 
            materialToUse = overrideMaterial;
        }
       
        private class PassData
        {
            // Create a field to store the list of objects to draw
            public RendererListHandle rendererListHandle;
        }
 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Redraw objects", out var passData))
            {
                // Get the data needed to create the list of objects to draw
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                SortingCriteria sortFlags = cameraData.defaultOpaqueSortFlags;
                RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
                FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, ~0);

                // Redraw only objects that have their LightMode tag set to UniversalForward 
                ShaderTagId shadersToOverride = new ShaderTagId("UniversalForward");

                // Create drawing settings
                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

                // Add the override material to the drawing settings
                drawSettings.overrideMaterial = materialToUse;

                // Create the list of objects to draw
                var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

                // Convert the list to a list handle that the render graph system can use
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
                
                // Set the render target as the color and depth textures of the active camera texture
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Clear the render target to black
            context.cmd.ClearRenderTarget(true, true, Color.black);

            // Draw the objects in the list
            context.cmd.DrawRendererList(data.rendererListHandle);
        }

    }
 
}
```

## Additional resources

- [Use textures](working-with-textures.md)
- [Using frame data](accessing-frame-data.md)
- [Scriptable Renderer Features](renderer-features/scriptable-renderer-features/scriptable-renderer-features-landing.md)
