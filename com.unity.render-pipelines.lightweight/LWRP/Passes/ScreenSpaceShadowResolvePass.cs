using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{

    public class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        RenderTextureFormat m_ColorFormat;
        Material m_ScreenSpaceShadowsMaterial;

        public ScreenSpaceShadowResolvePass(LightweightForwardRenderer renderer) : base(renderer)
        {
            m_ColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;

            m_ScreenSpaceShadowsMaterial = renderer.GetMaterial(MaterialHandles.ScrenSpaceShadow);
        }

        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;

            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.colorFormat = m_ColorFormat;
            descriptor = baseDescriptor;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            if (renderingData.shadowData.renderedDirectionalShadowQuality == LightShadows.None)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Collect Shadows");

            cmd.GetTemporaryRT(colorAttachmentHandle.id, descriptor, FilterMode.Bilinear);
            SetShadowCollectPassKeywords(cmd, ref renderingData.shadowData);

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            RenderTargetIdentifier screenSpaceOcclusionTexture = colorAttachmentHandle.Identifier();
            SetRenderTarget(cmd, screenSpaceOcclusionTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                ClearFlag.Color | ClearFlag.Depth, Color.white, descriptor.dimension);
            cmd.Blit(screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceShadowsMaterial);

            if (renderingData.cameraData.isStereoEnabled)
            {
                Camera camera = renderingData.cameraData.camera;
                context.StartMultiEye(camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(camera);
            }
            else
                context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

        void SetShadowCollectPassKeywords(CommandBuffer cmd, ref ShadowData shadowData)
        {
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftShadows, shadowData.renderedDirectionalShadowQuality == LightShadows.Soft);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);
        }
    }
}
