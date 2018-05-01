using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        const string kProfilerTag = "Depth Prepass Setup";
        const string kCommandBufferTag = "Depth Prepass";
        int kDepthBufferBits = 32;
        bool m_Disposed;
        FilterRenderersSettings m_FilterSettings;

        public DepthOnlyPass(ForwardRenderer renderer, int[] inputs, int[] targets) :
            base(renderer, inputs, targets)
        {
            RegisterShaderPassName("DepthOnly");

            m_FilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };

            m_Disposed = true;
        }

        public override void BindSurface(CommandBuffer cmd, RenderTextureDescriptor attachmentDescriptor, int samples)
        {
            attachmentDescriptor.colorFormat = RenderTextureFormat.Depth;
            attachmentDescriptor.depthBufferBits = kDepthBufferBits;

            if (samples > 1)
            {
                attachmentDescriptor.bindMS = samples > 1;
                attachmentDescriptor.msaaSamples = samples;
            }

            cmd.GetTemporaryRT(targetHandles[0], attachmentDescriptor, FilterMode.Point);
            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref PassData passData)
        {
            CameraData cameraData = passData.cameraData;
            CommandBuffer cmd = CommandBufferPool.Get(kCommandBufferTag);
            using (new ProfilingSample(cmd, kProfilerTag))
            {
                int depthSlice = LightweightPipeline.GetRenderTargetDepthSlice(cameraData.stereoEnabled);
                CoreUtils.SetRenderTarget(cmd, attachments[0], ClearFlag.Depth, 0, CubemapFace.Unknown, depthSlice);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var opaqueDrawSettings = new DrawRendererSettings(cameraData.camera, m_ShaderPassNames[0]);
                opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;

                if (cameraData.stereoEnabled)
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
                cmd.ReleaseTemporaryRT(targetHandles[0]);
        }
    }
}
