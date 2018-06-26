using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightDeferredRenderer : ScriptableRenderer
    {
        Material m_BlitMaterial;

        RenderPassAttachment m_GBufferAlbedo;
        RenderPassAttachment m_GBufferSpecRough;
        RenderPassAttachment m_GBufferNormal;
        RenderPassAttachment m_GBufferGIEmission;
        RenderPassAttachment m_DepthAttachment;

        const string k_GBufferProfilerTag = "Render GBuffer";

        public LightweightDeferredRenderer(LightweightPipelineAsset asset)
        {
            m_GBufferAlbedo = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferSpecRough = new RenderPassAttachment(RenderTextureFormat.ARGB32);
            m_GBufferNormal = new RenderPassAttachment(RenderTextureFormat.ARGB2101010);
            m_GBufferGIEmission = new RenderPassAttachment(asset.supportsHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            m_DepthAttachment = new RenderPassAttachment(RenderTextureFormat.Depth);

            m_GBufferGIEmission.Clear(Color.black, 1.0f, 0);
            m_DepthAttachment.Clear(Color.black, 1.0f, 0);

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(asset.blitTransientShader);
        }

        public override void Dispose()
        {

        }

        public override void Setup(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref RenderingData renderingData)
        {

        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            float renderScale = renderingData.cameraData.renderScale;
            int cameraPixelWidth = (int) (camera.pixelWidth * renderScale);
            int cameraPixelHeight = (int) (camera.pixelHeight * renderScale);

            m_GBufferAlbedo.BindSurface(BuiltinRenderTextureType.CameraTarget, false, true);

            context.SetupCameraProperties(renderingData.cameraData.camera, renderingData.cameraData.isStereoEnabled);

            using (RenderPass rp = new RenderPass(context, cameraPixelWidth, cameraPixelHeight, 1, 
                new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_GBufferGIEmission }, m_DepthAttachment))
            {
                using (new RenderPass.SubPass(rp, new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_GBufferGIEmission }, null))
                {
                    GBufferPass(ref context, ref cullResults, ref renderingData.cameraData);
                }

                //using (new RenderPass.SubPass(rp, new[] { m_LightAccumulation }, new[] { m_GBufferAlbedo, m_GBufferSpecRough, m_GBufferNormal, m_GBufferDepth }, true))
                //{
                //    LightingPass();
                //}

                //using (new RenderPass.SubPass(rp, new[] { m_LightAccumulation }, null))
                //{
                //    context.DrawSkybox(camera);
                //}

                //using (new RenderPass.SubPass(rp, new[] { m_LightAccumulation }, null))
                //{
                //   TransparentPass();
                //}

                using (new RenderPass.SubPass(rp, new[] { m_GBufferAlbedo }, new[] { m_GBufferGIEmission }))
                {
                    FinalPass(ref context, ref cullResults, ref renderingData.cameraData);
                }
            }
        }

        public void GBufferPass(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_GBufferProfilerTag);
            using (new ProfilingSample(cmd, k_GBufferProfilerTag))
            {
                Camera camera = cameraData.camera;

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("LightweightDeferred"))
                {
                    sorting = {flags = SortFlags.CommonOpaque},
                    rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightmaps,
                    flags = DrawRendererFlags.EnableInstancing,
                };

                var filterSettings = new FilterRenderersSettings(true)
                {
                    renderQueueRange = RenderQueueRange.opaque,
                };

                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            //using (var cmd = new CommandBuffer { name = "Create G-Buffer" })
            //{

            //    cmd.EnableShaderKeyword("UNITY_HDR_ON");
            //    cmd.ClearRenderTarget(true, true, new Color(0, 0, 0, 0));
            //    loop.ExecuteCommandBuffer(cmd);

            //    // render opaque objects using Deferred pass
            //    var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("LightweightDeferred"))
            //    {
            //        sorting = { flags = SortFlags.CommonOpaque },
            //        rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe
            //    };
            //    var filterSettings = new FilterRenderersSettings(true) { renderQueueRange = RenderQueueRange.opaque };
            //    loop.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);

            //}
        }

        public void LightingPass()
        {
            //using (var cmd = new CommandBuffer { name = "Deferred Lighting and Reflections Pass" })
            //{
            //    RenderLightsDeferred(camera, cullResults, cmd, loop);
            //    RenderReflections(camera, cmd, cullResults, loop);

            //    loop.ExecuteCommandBuffer(cmd);
            //}
        }

        public void TransparentPass()
        {
            //using (var cmd = new CommandBuffer { name = "Forwward Lighting Setup" })
            //{

            //    SetupLightShaderVariables(cullResults, camera, loop, cmd);
            //    loop.ExecuteCommandBuffer(cmd);

            //    var settings = new DrawRendererSettings(camera, new ShaderPassName("ForwardSinglePass"))
            //    {
            //        sorting = { flags = SortFlags.CommonTransparent },
            //        rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
            //    };
            //    var filterSettings = new FilterRenderersSettings(true) { renderQueueRange = RenderQueueRange.transparent };
            //    loop.DrawRenderers(cullResults.visibleRenderers, ref settings, filterSettings);
            //}
        }

        public void FinalPass(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Final Blit Pass");
            LightweightPipeline.DrawFullScreen(cmd, m_BlitMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
