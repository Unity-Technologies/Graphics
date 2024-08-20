---
uid: urp-render-graph-get-previous-frames
---
# Get data from previous frames

To fetch the previous frames that the camera rendered in the Universal Render Pipeline (URP), use the [`UniversalCameraData.historyManager`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.UniversalCameraData.html) API. These textures are sometimes called history textures or history buffers.

The frames are the output of the GPU rendering pipeline, so they don't include any processing that occurs after GPU rendering, such as post-processing effects.

To fetch previous frames from outside a scriptable render pass, refer to [Get data from previous frames in a script](#get-data-from-previous-frames-in-a-script).

Follow these steps:

1. In the `RecordRenderGraph` method, get the `UniversalCameraData` object from the `ContextContainer` object. For example:

    ```csharp
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
    {
        UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
    }    
    ```

2. To request access to either the color textures or the depth textures in the rendering history, use the `RequestAccess` API. For example:

    ```csharp
    // Request access to the color textures
    cameraData.historyManager.RequestAccess<RawColorHistory>();
    ```

    Use `RawDepthHistory` instead to request access to the depth textures.

3. Get one of the previous textures. For example:

    ```csharp
    // Get the previous textures 
    RawColorHistory history = cameraData.historyManager.GetHistoryForRead<RawColorHistory>();

    // Get the first texture, which the camera rendered in the previous frame
    RTHandle historyTexture = history?.GetPreviousTexture(0);
    ```

4. Convert the texture into a handle the render graph system can use. For example:

    ```csharp
    passData.historyTexture = renderGraph.ImportTexture(historyTexture);
    ```

You can then read the texture in the render pass.

For more information about using the `historyManager` API, refer to [`UniversalCameraData.historyManager`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.UniversalCameraData.html).

## Example

The following is a Scriptable Renderer Feature that creates a material and uses the previous frame as the material's texture.

To use the example, follow these steps:

1. Create a URP shader that samples a texture called `_BaseMap`. For an example, refer to [Drawing a texture](writing-shaders-urp-unlit-texture.html).
2. Create a material from the shader.
3. Create a new C# script called `RenderLastFrameInMaterial.cs`, paste the following code into it, and save the file.
4. In the active URP renderer, [add the Scriptable Renderer Feature](urp-renderer-feature-how-to-add.md).
5. In the **Inspector** window of the active URP renderer, in the **Render Last Frame In Material** section of the Scriptable Renderer Feature you added in step 4, assign the material you created in step 2 to the **Object Material** field.

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
public class RenderLastFrameInMaterial : ScriptableRendererFeature
{
    public Material objectMaterial;
    CustomRenderPass renderLastFrame;

    public override void Create()
    {
        renderLastFrame = new CustomRenderPass();
        renderLastFrame.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderLastFrame.passMaterial = objectMaterial;
        renderer.EnqueuePass(renderLastFrame);
    }

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material passMaterial;

        public class PassData
        {
            internal Material material;
            internal TextureHandle historyTexture;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer contextData)
        {
            UniversalCameraData cameraData = contextData.Get<UniversalCameraData>();

            // Return if the history manager isn't available
            // For example, there are no history textures during the first frame
            if (cameraData.historyManager == null) { return; }
  
            // Request access to the color and depth textures
            cameraData.historyManager.RequestAccess<RawColorHistory>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Get last frame", out var passData))
            {
                UniversalResourceData resourceData = contextData.Get<UniversalResourceData>();

                // Set the render graph to render to the active color texture
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.Write);

                // Add the material to the pass data
                passData.material = passMaterial;
                
                // Get the color texture the camera rendered to in the previous frame
                RawColorHistory history = cameraData.historyManager.GetHistoryForRead<RawColorHistory>();
                RTHandle historyTexture = history?.GetPreviousTexture(0);
                passData.historyTexture = renderGraph.ImportTexture(historyTexture);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    // Set the material to use the texture
                    data.material.SetTexture("_BaseMap", data.historyTexture);
                });
            }
        }
    }
}
```

## Get data from previous frames in a script

To get data from previous frames in a script, for example a `MonoBehaviour`, do the following:

1. Use the Scriptable Render Pipeline (SRP) Core [`RequestAccess`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.IPerFrameHistoryAccessTracker.html#UnityEngine_Rendering_IPerFrameHistoryAccessTracker_RequestAccess__1) API to request the texture.
2. Use the [`UniversalAdditionalCameraData.history`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/api/UnityEngine.Rendering.Universal.UniversalAdditionalCameraData.html#UnityEngine_Rendering_Universal_UniversalAdditionalCameraData_history) API to get the data.

To make sure Unity finishes rendering the frame first, use the `UniversalAdditionalCameraData.history` API in the [`LateUpdate`](https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html) method.

For more information, refer to the following in the Scriptable Render Pipeline (SRP) Core package:

- [`ICameraHistoryReadAccess`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.ICameraHistoryReadAccess.html)
- [`IPerFrameHistoryTracker`](https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@17.0/api/UnityEngine.Rendering.IPerFrameHistoryAccessTracker.html)

