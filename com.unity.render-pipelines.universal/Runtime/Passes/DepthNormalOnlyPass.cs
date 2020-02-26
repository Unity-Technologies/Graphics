using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DepthNormalOnlyPass : ScriptableRenderPass
    {
        const int kDepthBufferBits = 32;

        private RenderTargetHandle depthNormalsAttachmentHandle { get; set; }
        internal RenderTextureDescriptor descriptor { get; private set; }

        private string m_ProfilerTag = "DepthNormal Prepass";
        private Material m_DepthNormalMaterial;
        private ShaderTagId m_ShaderTagId = new ShaderTagId("DepthNormals");
        private FilteringSettings m_FilteringSettings;

        /// <summary>
        /// Create the DepthNormalOnlyPass
        /// </summary>
        public DepthNormalOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, Material depthNormalMaterial)
        {
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            renderPassEvent = evt;
            m_DepthNormalMaterial = depthNormalMaterial;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle depthNormalAttachmentHandle)
        {
            depthNormalsAttachmentHandle = depthNormalAttachmentHandle;

            baseDescriptor.colorFormat = RenderTextureFormat.ARGBHalf;
            baseDescriptor.depthBufferBits = kDepthBufferBits;

            // Depth-Only pass don't use MSAA
            baseDescriptor.msaaSamples = 1;
            descriptor = baseDescriptor;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(depthNormalsAttachmentHandle.id, descriptor, FilterMode.Point);
            ConfigureTarget(depthNormalsAttachmentHandle.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingSample(cmd, m_ProfilerTag))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                //drawSettings.overrideMaterial = m_DepthNormalMaterial;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStereoEnabled)
                {
                    context.StartMultiEye(camera);
                }

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);

            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }

            if (depthNormalsAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(depthNormalsAttachmentHandle.id);
                depthNormalsAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
