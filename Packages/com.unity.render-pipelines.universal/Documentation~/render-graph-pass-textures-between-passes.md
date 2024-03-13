# Transfer a texture between render passes

You can transfer a texture between render passes, for example if you need to create a texture in one render pass and read it in a later render pass.

Use the following methods to transfer textures between render passes:

- [Add a texture to the frame data](#add-a-texture-to-the-frame-data)
- [Set a texture as a global texture](#set-a-texture-as-a-global-texture)

You can also store the texture outside the render passes, for example as a `TextureHandle` in a [Scriptable Renderer Feature](renderer-features/scriptable-renderer-features/scriptable-renderer-features-landing.md).

If you need to use make sure a texture is available across multiple frames, or that multiple cameras can access it, refer to [Import a texture into the render graph system](render-graph-import-a-texture.md) instead.

## Add a texture to the frame data

You can add a texture to the [frame data](accessing-frame-data.md) so you can fetch the texture in a later render pass.

Follow these steps:

1. Create a class that inherits [`ContextItem`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.ContextItem.html) and contains a texture handle field.

    For example:

    ```csharp
    public class MyCustomData : ContextItem {
        public TextureHandle textureToTransfer;
    }
    ```

2. You must implement the `Reset()` method in your class, to reset the texture when the frame resets.

    For example:

    ```csharp
    public class MyCustomData : ContextItem {
        public TextureHandle textureToTransfer;

        public override void Reset()
        {
            textureToTransfer = TextureHandle.nullHandle;
        }
    }    
    ```

3. In your `RecordRenderGraph` method, add an instance of your class to the frame data.

    For example:

    ```csharp
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        using (var builder = renderGraph.AddPass<PassData>("Get frame data", out var passData))
        {
            UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
            var customData = contextData.Create<MyCustomData>();
        }
    }
    ```

4. Set the texture handle to your texture.

    For example:

    ```csharp
    // Create texture properties that match the screen
    RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

    // Create the texture
    TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);

    // Set the texture in the custom data instance
    customData.textureToTransfer = texture;
    ```

In a later render pass, in your `RecordRenderGraph` method, you can get your custom data and fetch your texture:

For example:

```csharp
// Get the custom data
MyCustomData fetchedData = frameData.Get<MyCustomData>();

// Get the texture
TextureHandle customTexture = customData.textureToTransfer;
```

Refer to [Use frame data](accessing-frame-data.md) for more information about frame data.

### Example

The following example adds a `CustomData` class that contains a texture. The first render pass clears the texture to yellow, and the second render pass fetches the yellow texture and draws a triangle onto it.

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class AddOwnTextureToFrameData : ScriptableRendererFeature
{
    AddOwnTexturePass customPass1;
    DrawTrianglePass customPass2;

    public override void Create()
    {
        customPass1 = new AddOwnTexturePass();
        customPass2 = new DrawTrianglePass();

        customPass1.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        customPass2.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass1);
        renderer.EnqueuePass(customPass2);
    }
    
    // Create the first render pass, which creates a texture and adds it to the frame data
    class AddOwnTexturePass : ScriptableRenderPass
    {

        class PassData
        {
            internal TextureHandle copySourceTexture;
        }

        // Create the custom data class that contains the new texture
        public class CustomData : ContextItem {
            public TextureHandle newTextureForFrameData;

            public override void Reset()
            {
                newTextureForFrameData = TextureHandle.nullHandle;
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Create new texture", out var passData))
            {
                // Create a texture and set it as the render target
                RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);
                CustomData customData = frameContext.Create<CustomData>();
                customData.newTextureForFrameData = texture;
                builder.SetRenderAttachment(texture, 0, AccessFlags.Write);
    
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {          
            // Clear the render target (the texture) to yellow
            context.cmd.ClearRenderTarget(true, true, Color.yellow);
        }
 
    }

    // Create the second render pass, which fetches the texture and writes to it
    class DrawTrianglePass : ScriptableRenderPass
    {

        class PassData
        {
            // No local pass data needed
        }      

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Fetch texture and draw triangle", out var passData))
            {                                
                // Fetch the yellow texture from the frame data and set it as the render target
                var customData = frameContext.Get<AddOwnTexturePass.CustomData>();
                var customTexture = customData.newTextureForFrameData;
                builder.SetRenderAttachment(customTexture, 0, AccessFlags.Write);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {          
            // Generate a triangle mesh
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0) };
            mesh.triangles = new int[] { 0, 1, 2 };
            
            // Draw a triangle to the render target (the yellow texture)
            context.cmd.DrawMesh(mesh, Matrix4x4.identity, new Material(Shader.Find("Universal Render Pipeline/Unlit")));
        }
    }
}
```

## Set a texture as a global texture

If you need to use a texture as the input for the shader on a GameObject, you can set a texture as a global texture. A global texture is available to all shaders and render passes.

Setting a texture as a global texture can make rendering slower. Refer to [SetGlobalTexture](https://docs.unity3d.com/ScriptReference/Shader.SetGlobalTexture.html) for more information.

Don't use an [unsafe render pass](render-graph-unsafe-pass.md) and `CommandBuffer.SetGlobal` to set a texture as a global texture, because it might cause errors.

To set a global texture, in the `RecordRenderGraph` method, use the [`SetGlobalTextureAfterPass`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.RenderGraphModule.IBaseRenderGraphBuilder.html#UnityEngine_Rendering_RenderGraphModule_IBaseRenderGraphBuilder_SetGlobalTextureAfterPass_UnityEngine_Rendering_RenderGraphModule_TextureHandle__System_Int32_) method. 

For example:

```csharp
// Allocate a global shader texture called _GlobalTexture
private int shaderTextureID = Shader.globalTextureID("_GlobalTexture")

using (var builder = renderGraph.AddRasterRenderPass<PassData>("MyPass", out var passData)){

    // Set a texture to the global texture
    builder.SetGlobalTextureAfterPass(texture, globalTextureID);
}
```

You can now:

- Access the texture in a different render pass, using the `UseGlobalTexture()` or `UseAllGlobalTextures()` API.
- Use the texture on any material in your scene. URP automatically uses the `UseAllGlobalTextures()` API to enable this.
