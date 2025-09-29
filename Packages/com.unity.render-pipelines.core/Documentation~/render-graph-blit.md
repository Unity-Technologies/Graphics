# Blit using the render graph system in SRP Core

To blit from one texture to another in the render graph system, use the [`AddBlitPass`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.3/api/UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.html#UnityEngine_Rendering_RenderGraphModule_Util_RenderGraphUtils_AddBlitPass_UnityEngine_Rendering_RenderGraphModule_RenderGraph_UnityEngine_Rendering_RenderGraphModule_Util_RenderGraphUtils_BlitMaterialParameters_System_String_System_Boolean_) API. The API generates a render pass automatically, so you don't need to use a method like `AddRasterRenderPass`.

Follow these steps:

1. To create a shader and material that works with a blit render pass, from the main menu select **Assets** > **Create** > **Shader** > **SRP Blit Shader**, then create a material from it.

1. Add `using UnityEngine.Rendering.RenderGraphModule.Util` to your render pass script.

1. In your render pass, create a field for the blit material. For example:

    ```lang-cs
    public class MyBlitPass : ScriptableRenderPass
    {
        Material blitMaterial;
    }
    ```

1. Set up the texture to blit from and blit to. For example:

    ```lang-cs
    TextureHandle sourceTexture = renderGraph.CreateTexture(sourceTextureProperties);
    TextureHandle destinationTexture = renderGraph.CreateTexture(destinationTextureProperties);
    ```

1. To set up the material, textures, and shader pass for the blit operation, create a [`RenderGraphUtils.BlitMaterialParameters`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.3/api/UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.BlitMaterialParameters.html) object. For example:

    ```lang-cs
    // Create a BlitMaterialParameters object with the blit material, source texture, destination texture, and shader pass to use.
    var blitParams = new RenderGraphUtils.BlitMaterialParameters(sourceTexture, destinationTexture, blitMaterial, 0);
    ```

1. To add a blit pass, call the [`AddBlitPass`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.3/api/UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.html#UnityEngine_Rendering_RenderGraphModule_Util_RenderGraphUtils_AddBlitPass_UnityEngine_Rendering_RenderGraphModule_RenderGraph_UnityEngine_Rendering_RenderGraphModule_Util_RenderGraphUtils_BlitMaterialParameters_System_String_System_Boolean_) method with the blit parameters. For example:

    ```lang-cs
    renderGraph.AddBlitPass(blitParams, "Pass created with AddBlitPass");
    ```

If you use `AddBlitPass` with a default material, Unity might use the `AddCopyPass` API instead, to optimize the render pass so it accesses the framebuffer from the on-chip memory of the GPU instead of video memory. This process is sometimes called framebuffer fetch. For more information, refer to [`AddCopyPass`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.3/api/UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils.html#UnityEngine_Rendering_RenderGraphModule_Util_RenderGraphUtils_AddCopyPass_UnityEngine_Rendering_RenderGraphModule_RenderGraph_UnityEngine_Rendering_RenderGraphModule_TextureHandle_UnityEngine_Rendering_RenderGraphModule_TextureHandle_System_String_System_Boolean_) API.

## Customize the render pass

To customize the render pass that the methods generate, for example to change settings or add more resources, call the APIs with the `returnBuilder` parameter set to `true`. The APIs then return the `IBaseRenderGraphBuilder` object that you usually receive as `var builder` from a method like `AddRasterRenderPass`.

For example:

```lang-cs
using (var builder = renderGraph.AddBlitPass(blitParams, "Pass created with AddBlitPass", returnBuilder: true))
{
    // Use the builder variable to customize the render pass here.
}
```

## Additional resources

- [The RTHandle System](rthandle-system.md)
- [Shaders](shaders.md)
