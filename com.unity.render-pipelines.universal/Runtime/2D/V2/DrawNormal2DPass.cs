using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.UI;

namespace UnityEngine.Rendering.Universal
{
    internal class DrawNormal2DPass : ScriptableRenderPass
    {
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler("Draw 2D Normals");
        private static readonly ShaderTagId k_NormalsRenderingPassName = new ShaderTagId("NormalsRendering");
        private LayerBatch layerBatch;
        // private RTHandle[] gbuffers;
        // private GraphicsFormat[] gformats;
        // private RTHandle depthTexture;

        public DrawNormal2DPass(bool isNative)
        {
            useNativeRenderPass = isNative;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                var filterSettings = new FilteringSettings();
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = -1;
                filterSettings.renderingLayerMask = 0xFFFFFFFF;
                filterSettings.sortingLayerRange = new SortingLayerRange(layerBatch.layerRange.lowerBound, layerBatch.layerRange.upperBound);
                var drawSettings = CreateDrawingSettings(k_NormalsRenderingPassName, ref renderingData, SortingCriteria.CommonTransparent);

                // if (!useNativeRenderPass)
                // {
                //     cmd.DisableShaderKeyword("USE_MRT");
                //     // CoreUtils.SetRenderTarget(cmd, normalTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.Color, RendererLighting.k_NormalClearColor);
                // }
                // else
                // {
                     cmd.EnableShaderKeyword("USE_MRT");
                // }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        // {
        //     ConfigureTarget(gbuffers, depthTexture, gformats);
        //     ConfigureClear(ClearFlag.None, Color.black);
        // }

        public void Setup(LayerBatch layerBatch)//, RTHandle depthAttachment, GraphicsFormat[] formats, params RTHandle[] gbuffers)
        {
            this.layerBatch = layerBatch;
            // this.gbuffers = gbuffers;
            // this.gformats = formats;
            // this.depthTexture = depthAttachment;
        }
    }
}
