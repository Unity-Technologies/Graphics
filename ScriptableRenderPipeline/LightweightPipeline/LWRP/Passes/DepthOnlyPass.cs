using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        public int depthTextureID { get; private set; }
        public RenderTargetIdentifier depthTexture { get; private set; }

        const string kProfilerTag = "Depth Prepass Setup";
        const string kCommandBufferTag = "Depth Prepass";
        int kDepthBufferBits = 32;
        bool m_Disposed;
        FilterRenderersSettings m_FilterSettings;

        public DepthOnlyPass(RenderTextureFormat[] colorAttachments, RenderTextureFormat depthAttachment) :
            base(colorAttachments, depthAttachment)
        {
            RegisterShaderPassName("DepthOnly");
            depthTextureID = Shader.PropertyToID("_CameraDepthTexture");
            depthTexture = new RenderTargetIdentifier(depthTextureID);

            m_Disposed = true;
            m_FilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque
            };
        }

        public override void BindSurface(CommandBuffer cmd, RenderTextureDescriptor attachmentDescriptor, int samples)
        {
            attachmentDescriptor.colorFormat = depthAttachment;
            attachmentDescriptor.depthBufferBits = kDepthBufferBits;

            if (samples > 1)
            {
                attachmentDescriptor.bindMS = samples > 1;
                attachmentDescriptor.msaaSamples = samples;
            }

            cmd.GetTemporaryRT(depthTextureID, attachmentDescriptor, FilterMode.Point);
            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData,
            Camera camera, bool stereoRendering)
        {
            CommandBuffer cmd = CommandBufferPool.Get(kCommandBufferTag);
            using (new ProfilingSample(cmd, kProfilerTag))
            {
                int depthSlice = LightweightPipeline.GetRenderTargetDepthSlice(stereoRendering);
                CoreUtils.SetRenderTarget(cmd, depthTexture, ClearFlag.Depth, 0, CubemapFace.Unknown, depthSlice);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var opaqueDrawSettings = new DrawRendererSettings(camera, m_ShaderPassNames[0]);
                opaqueDrawSettings.sorting.flags = SortFlags.CommonOpaque;

                if (stereoRendering)
                {
                    context.StartMultiEye(camera);
                    context.DrawRenderers(cullResults.visibleRenderers, ref opaqueDrawSettings, m_FilterSettings);
                    context.StopMultiEye(camera);
                }
                else
                    context.DrawRenderers(cullResults.visibleRenderers, ref opaqueDrawSettings, m_FilterSettings);
            }
            cmd.SetGlobalTexture(depthTextureID, depthTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (!m_Disposed)
                cmd.ReleaseTemporaryRT(depthTextureID);
        }
    }
}
