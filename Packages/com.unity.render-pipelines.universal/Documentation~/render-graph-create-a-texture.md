# Create a render graph system texture

You can create a render graph texture in a custom render pass. You can then [read from or write to the texture](render-graph-read-write-texture.md).

When the Universal Render Pipeline (URP) optimizes the render graph, it might not create a texture if the final frame doesn't use the texture, to reduce the memory and bandwidth the render passes use. Refer to [Introduction to the render graph system](render-graph-introduction.md) for more information.

If you need to use a texture in multiple frames or on multiple cameras, for example a texture asset you imported in your project, refer to [Import a texture into the render graph system](render-graph-import-a-texture.md).

## Create a texture

To create a texture, in the `RecordRenderGraph` method of your `ScriptableRenderPass` class, follow these steps:

1. Create a [`RenderTextureDescriptor`](https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor.html) object with the texture properties you need.
2. Use the [`UniversalRenderer.CreateRenderGraphTexture`](xref:UnityEngine.Rendering.Universal.UniversalRenderer.CreateRenderGraphTexture(UnityEngine.Experimental.Rendering.RenderGraphModule.RenderGraph,UnityEngine.RenderTextureDescriptor,System.String,System.Boolean,UnityEngine.FilterMode,UnityEngine.TextureWrapMode)) method to create a texture and return a texture handle.

For example, the following creates a texture the same size as the screen.

```csharp
RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
TextureHandle textureHandle = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);
```

You can then [use the texture](render-graph-read-write-texture.md) in the same custom render pass.

Only the current camera can access the texture. To access the texture somewhere else, for example from another camera or in custom rendering code, [import a texture](render-graph-import-a-texture.md) instead.

The render graph system manages the lifetime of textures you create with `CreateRenderGraphTexture`, so you don't need to manually release the memory they use when you're finished with them.

### Example

The following Scriptable Renderer Feature contains an example render pass that creates a texture and clears it to yellow. Refer to [Inject a pass using a Scriptable Renderer Feature](renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md#add-renderer-feature-to-asset) for instructions on how to add the render pass to a project.

Use the [Frame Debugger](https://docs.unity3d.com/2023.3/Documentation/Manual/frame-debugger-window.html) to check the texture the render pass adds.

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class CreateYellowTextureFeature : ScriptableRendererFeature
{
    CreateYellowTexture customPass;

    public override void Create()
    {
        customPass = new CreateYellowTexture();
        customPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass);
    }

    class CreateYellowTexture : ScriptableRenderPass
    {
        class PassData
        {
            internal TextureHandle cameraColorTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Create yellow texture", out var passData))
            {
                // Create texture properties that match the screen size
                RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

                // Create a temporary texture
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);

                // Set the texture as the render target
                builder.SetRenderAttachment(texture, 0, AccessFlags.Write);
    
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {          
            // Clear the render target to yellow
            context.cmd.ClearRenderTarget(true, true, Color.yellow);            
        }
    }

}
```

## Additional resources

* [Import a texture into the render graph system](render-graph-import-a-texture.md)
* [Use frame data](accessing-frame-data.md)
* [Textures](https://docs.unity3d.com/Manual/Textures.html)
