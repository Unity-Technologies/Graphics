using System;

namespace UnityEngine.Rendering.LWRP
{
    /// <summary>
    /// Render all objects that have a 'DepthOnly' pass into the given depth buffer.
    ///
    /// You can use this pass to prime a depth buffer for subsequent rendering.
    /// Use it as a z-prepass, or use it to generate a depth buffer.
    /// </summary>
    internal class DepthOnlyPass : ScriptableRenderPass
    {
        const string k_DepthPrepassTag = "Depth Prepass";

        int kDepthBufferBits = 32;

        private RenderTargetHandle depthAttachmentHandle { get; set; }
        internal RenderTextureDescriptor descriptor { get; private set; }

        FilteringSettings m_FilteringSettings;

        /// <summary>
        /// Create the DepthOnlyPass
        /// </summary>
        public DepthOnlyPass(RenderPassEvent evt, RenderQueueRange renderQueueRange)
        {
            RegisterShaderPassName("DepthOnly");
            m_FilteringSettings = new FilteringSettings(renderQueueRange);
            renderPassEvent = evt;
        }

        /// <summary>
        /// Configure the pass
        /// </summary>
        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle depthAttachmentHandle)
        {
            this.depthAttachmentHandle = depthAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.Depth;
            baseDescriptor.depthBufferBits = kDepthBufferBits;

            // Depth-Only pass don't use MSAA
            baseDescriptor.msaaSamples = 1;

            descriptor = baseDescriptor;
        }

        public override bool ShouldExecute(ref RenderingData renderingData)
        {
            // Depth prepass is generated in the following cases:
            // - We resolve shadows in screen space
            // - Scene view camera always requires a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            return renderingData.cameraData.isSceneViewCamera ||
                (renderingData.cameraData.requiresDepthTexture && (!CanCopyDepth(ref renderingData.cameraData)));
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
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

                m_FilteringSettings.layerMask = renderingData.cameraData.camera.cullingMask;
                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref m_FilteringSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
                depthAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
