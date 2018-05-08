using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        const string kProfilerTag = "Depth Prepass Setup";
        const string kCommandBufferTag = "Depth Prepass";

        int kDepthBufferBits = 32;

        public DepthOnlyPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            RegisterShaderPassName("DepthOnly");
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int[] colorAttachmentHandles, int depthAttachmentHandle = -1, int samples = 1)
        {
            base.Setup(cmd, baseDescriptor, colorAttachmentHandles, depthAttachmentHandle, samples);
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = kDepthBufferBits;

            if (samples > 1)
            {
                baseDescriptor.bindMS = samples > 1;
                baseDescriptor.msaaSamples = samples;
            }

            cmd.GetTemporaryRT(depthAttachmentHandle, baseDescriptor, FilterMode.Point);
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(kCommandBufferTag);
            using (new ProfilingSample(cmd, kProfilerTag))
            {
                SetRenderTarget(cmd, GetSurface(depthAttachmentHandle), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                    ClearFlag.Depth, Color.black);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = CreateDrawRendererSettings(cameraData.camera, SortFlags.CommonOpaque, RendererConfiguration.None);
                if (cameraData.isStereoEnabled)
                {
                    context.StartMultiEye(cameraData.camera);
                    context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.opaqueFilterSettings);
                    context.StopMultiEye(cameraData.camera);
                }
                else
                    context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.opaqueFilterSettings);
            }
            //cmd.SetGlobalTexture(depthTextureID, depthTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
