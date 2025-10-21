using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    [SupportedOnRenderer(typeof(Renderer2DData))]
    public class CustomRendererFeature2D : ScriptableRendererFeature2D
    {
        private CustomRenderPass2D m_CustomRenderPass2D;

        public override void Create()
        {
            m_CustomRenderPass2D = new CustomRenderPass2D
            {
                renderPassEvent2D = injectionPoint2D,
                renderPassSortingLayerID = sortingLayerID
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(m_CustomRenderPass2D);
        }
    }

    internal class CustomRenderPass2D : ScriptableRenderPass2D
    {
        class PassData
        {
            internal TextureHandle copySourceTexture;
        }

        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // Records a rendering command to copy, or blit, the contents of the source texture
            // to the color render target of the render pass.
            Blitter.BlitTexture(context.cmd, data.copySourceTexture,
                new Vector4(1, 1, 0, 0), 0, false);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            string passName = "Debug Custom RenderPass2D";

#if UNITY_EDITOR
            if (LayerDebug.enabled)
                passName = passName + " - " + renderPassEvent2D.ToString();
#endif

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName,
            out var passData))
            {
                // UniversalResourceData contains all the texture references used by URP,
                // including the active color and depth textures of the camera.
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // Populate passData with the data needed by the rendering function
                // of the render pass.
                // Use the camera's active color texture
                // as the source texture for the copy operation.
                passData.copySourceTexture = resourceData.cameraColor;

                // Create a destination texture for the copy operation based on the settings,
                // such as dimensions, of the textures that the camera uses.
                // Set msaaSamples to 1 to get a non-multisampled destination texture.
                // Set depthBufferBits to 0 to ensure that the CreateRenderGraphTexture method
                // creates a color texture and not a depth texture.
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                RenderTextureDescriptor desc = cameraData.cameraTargetDescriptor;
                desc.msaaSamples = 1;
                desc.depthBufferBits = 0;

                // For demonstrative purposes, this sample creates a temporary destination texture.
                // UniversalRenderer.CreateRenderGraphTexture is a helper method
                // that calls the RenderGraph.CreateTexture method.
                // Using a RenderTextureDescriptor instance instead of a TextureDesc instance
                // simplifies your code.
                TextureHandle destination =
                    UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc,
                        "CopyTexture", false);

                // Declare that this render pass uses the source texture as a read-only input.
                builder.UseTexture(passData.copySourceTexture);

                // Declare that this render pass uses the temporary destination texture
                // as its color render target.
                // This is similar to cmd.SetRenderTarget prior to the RenderGraph API.
                builder.SetRenderAttachment(destination, 0);

                // RenderGraph automatically determines that it can remove this render pass
                // because its results, which are stored in the temporary destination texture,
                // are not used by other passes.
                // For demonstrative purposes, this sample turns off this behavior to make sure
                // that render graph executes the render pass. 
                builder.AllowPassCulling(false);

                // Set the ExecutePass method as the rendering function that render graph calls
                // for the render pass. 
                // This sample uses a lambda expression to avoid memory allocations.
                builder.SetRenderFunc((PassData data, RasterGraphContext context)
                    => ExecutePass(data, context));
            }
        }
    }
}
