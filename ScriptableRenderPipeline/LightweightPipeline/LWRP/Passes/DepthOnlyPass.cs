using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        const string kProfilerTag = "Depth Prepass Setup";
        const string kCommandBufferTag = "Depth Prepass";

        int kDepthBufferBits = 32;
        FilterRenderersSettings m_FilterSettings;

        public DepthOnlyPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            RegisterShaderPassName("DepthOnly");

            m_FilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples)
        {
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = kDepthBufferBits;

            if (samples > 1)
            {
                baseDescriptor.bindMS = samples > 1;
                baseDescriptor.msaaSamples = samples;
            }

            cmd.GetTemporaryRT(RenderTargetHandles.Depth, baseDescriptor, FilterMode.Point);
            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(kCommandBufferTag);
            using (new ProfilingSample(cmd, kProfilerTag))
            {
                int depthSlice = LightweightPipeline.GetRenderTargetDepthSlice(cameraData.isStereoEnabled);
                CoreUtils.SetRenderTarget(cmd, GetSurface(RenderTargetHandles.Depth), ClearFlag.Depth, 0, CubemapFace.Unknown, depthSlice);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var opaqueDrawSettings = new DrawRendererSettings(cameraData.camera, m_ShaderPassNames[0]);
                opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;
                opaqueDrawSettings.rendererConfiguration = RendererConfiguration.None;

                if (cameraData.isStereoEnabled)
                {
                    context.StartMultiEye(cameraData.camera);
                    context.DrawRenderers(cullResults.visibleRenderers, ref opaqueDrawSettings, m_FilterSettings);
                    context.StopMultiEye(cameraData.camera);
                }
                else
                    context.DrawRenderers(cullResults.visibleRenderers, ref opaqueDrawSettings, m_FilterSettings);
            }
            //cmd.SetGlobalTexture(depthTextureID, depthTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (!m_Disposed)
            {
                cmd.ReleaseTemporaryRT(RenderTargetHandles.Depth);
                m_Disposed = true;
            }
        }
    }
}
