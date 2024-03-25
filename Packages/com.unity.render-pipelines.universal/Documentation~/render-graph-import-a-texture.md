# Import a texture into the render graph system

When you [create a render graph system texture](render-graph-create-a-texture.md) in a render pass, the render graph system handles the creation and disposal of the texture. This process means the texture might not exist in the next frame, and other cameras might not be able to use it.

To make sure a texture is available across frames and cameras, you can import it into the render graph system using the `ImportTexture` API.

You can import a texture if you use a texture created outside the render graph system. For example, you can create a render texture that points to a texture in your project, such as a [texture asset](https://docs.unity3d.com/Manual/ImportingTextures.html), and use it as the input to a render pass.

The render graph system doesn't manage the lifetime of imported textures. As a result, the following applies:

- You must [dispose of the imported render texture](#dispose-of-a-render-texture) to free up the memory it uses when you're finished with it.
- URP can't cull render passes that use imported textures. As a result, rendering might be slower.

Refer to [Using the RTHandle system](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/manual/rthandle-system-using.html) in the SRP Core manual for more information about the `RTHandle` API.

## Import a texture

To import a texture, in the `RecordRenderGraph` method of your `ScriptableRenderPass` class, follow these steps:

1. Create a render texture handle using the [RTHandle](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.RTHandle.html) API.

    For example:

    ```csharp
    private RTHandle renderTextureHandle;
    ```

2. Create a [RenderTextureDescriptor](https://docs.unity3d.com/ScriptReference/RenderTextureDescriptor.html) object with the texture properties you need.

    For example:

    ```csharp
    RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
    ```

3. Use the [ReAllocateIfNeeded](xref:UnityEngine.Rendering.Universal.RenderingUtils.ReAllocateIfNeeded(UnityEngine.Rendering.RTHandle@,UnityEngine.RenderTextureDescriptor@,UnityEngine.FilterMode,UnityEngine.TextureWrapMode,System.Boolean,System.Int32,System.Single,System.String)) method to create a render texture and attach it to the render texture handle. This method creates a render texture only if the render texture handle is null, or the render texture has different properties to the render texture descriptor.

    For example:

    ```csharp
    RenderingUtils.ReAllocateIfNeeded(ref renderTextureHandle, textureProperties, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "My render texture" );
    ```

4. Import the texture, to convert the `RTHandle` object to a `TextureHandle` object the render graph system can use. 

    For example:
    
    ```csharp
    TextureHandle texture = renderGraph.ImportTexture(renderTextureHandle);
    ```

You can then use the `TextureHandle` object to [read from or write to the render texture](render-graph-read-write-texture.md).

## Import a texture from your project

To import a texture from your project, such as an imported texture attached to a material, follow these steps:

1. Use the [`RTHandles.Alloc`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.RTHandles.html#UnityEngine_Rendering_RTHandles_Alloc_UnityEngine_RenderTexture_) API to create a render texture handle from the external texture.

    For example:

    ```csharp
    RTHandle renderTexture = RTHandles.Alloc(texture);
    ```

2. Import the texture, to convert the `RTHandle` object to a `TextureHandle` object that the render graph system can use.

    For example:

    ```csharp
    TextureHandle textureHandle = renderGraph.ImportTexture(renderTexture);
    ```

You can then use the `TextureHandle` object to [read from or write to the render texture](render-graph-read-write-texture.md).

## Dispose of the render texture

You must free the memory a render texture uses at the end of a render pass, using the `Dispose` method.

```csharp
public void Dispose()
{
    renderTexture.Release();
}
```

## Example

The following Scriptable Renderer Feature contains an example render pass that copies a texture asset to a temporary texture. To use this example, follow these steps:

1. Refer to [Inject a pass using a Scriptable Renderer Feature](renderer-features/scriptable-renderer-features/inject-a-pass-using-a-scriptable-renderer-feature.md#add-renderer-feature-to-asset) for instructions on how to add this render pass to a URP Asset.
2. In the Inspector window of the URP Asset, add a texture to the **Texture To Use** property.
3. Use the [Frame Debugger](https://docs.unity3d.com/2023.3/Documentation/Manual/frame-debugger-window.html) to check the texture the render pass adds.

```csharp
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BlitFromExternalTexture : ScriptableRendererFeature
{
    // The texture to use as input 
    public Texture2D textureToUse;

    BlitFromTexture customPass;

    public override void Create()
    {
        // Create an instance of the render pass, and pass in the input texture 
        customPass = new BlitFromTexture(textureToUse);

        customPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(customPass);
    }

    class BlitFromTexture : ScriptableRenderPass
    {
        class PassData
        {
            internal TextureHandle textureToRead;
        }

        private Texture2D texturePassedIn;

        public BlitFromTexture(Texture2D textureIn)
        {
            // In the render pass's constructor, set the input texture
            texturePassedIn = textureIn;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Copy texture", out var passData))
            {
                // Create a temporary texture and set it as the render target
                RenderTextureDescriptor textureProperties = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
                TextureHandle texture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, textureProperties, "My texture", false);
                builder.SetRenderAttachment(texture, 0, AccessFlags.Write);

                // Create a render texture from the input texture
                RTHandle rtHandle = RTHandles.Alloc(texturePassedIn);

                // Create a texture handle that the shader graph system can use
                TextureHandle textureToRead = renderGraph.ImportTexture(rtHandle);

                // Add the texture to the pass data
                passData.textureToRead = textureToRead;

                // Set the texture as readable
                builder.UseTexture(passData.textureToRead, AccessFlags.Read);

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {          
            // Copy the imported texture to the render target
            Blitter.BlitTexture(context.cmd, data.textureToRead, new Vector4(0.8f,0.6f,0,0), 0, false);
        }
    }
}
```

## Additional resources

* [Textures](https://docs.unity3d.com/Manual/Textures.html)
* [Render Texture assets](https://docs.unity3d.com/Manual/class-RenderTexture.html)
* [Custom Render Texture assets](https://docs.unity3d.com/Manual/class-CustomRenderTexture.html)
