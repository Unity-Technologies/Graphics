using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class DepthOnlyPass : ScriptableRenderPass
    {
        const string k_DepthPrepassTag = "Depth Prepass";

        int kDepthBufferBits = 32;

        private RenderTargetHandle depthAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle depthAttachmentHandle,
            SampleCount samples)
        {
            this.depthAttachmentHandle = depthAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = kDepthBufferBits;

            if ((int)samples > 1)
            {
                baseDescriptor.bindMS = (int)samples > 1;
                baseDescriptor.msaaSamples = (int)samples;
            }

            descriptor = baseDescriptor;
        }

        public DepthOnlyPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            RegisterShaderPassName("DepthOnly");
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_DepthPrepassTag);
            using (new ProfilingSample(cmd, k_DepthPrepassTag))
            {
                cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
                SetRenderTarget(
                    cmd,
                    depthAttachmentHandle.Identifier(),
                    RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store,
                    ClearFlag.Depth,
                    Color.black,
                    descriptor.dimension);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = CreateDrawRendererSettings(renderingData.cameraData.camera, SortFlags.CommonOpaque, RendererConfiguration.None, renderingData.supportsDynamicBatching);
                if (renderingData.cameraData.isStereoEnabled)
                {
                    Camera camera = renderingData.cameraData.camera;
                    context.StartMultiEye(camera);
                    context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.opaqueFilterSettings);
                    context.StopMultiEye(camera);
                }
                else
                    context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, renderer.opaqueFilterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }


        public override void Dispose(CommandBuffer cmd)
        {
            if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
                depthAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

    }
}
