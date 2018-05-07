using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class ScreenSpaceShadowOcclusionPass : ScriptableRenderPass
    {
        public bool softShadows { get; set; }

        RenderTextureFormat m_ColorFormat;
        Material m_ScreenSpaceShadowsMaterial;

        public ScreenSpaceShadowOcclusionPass(LightweightForwardRenderer renderer) : base(renderer)
        {
            m_ColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;

            m_ScreenSpaceShadowsMaterial = renderer.GetMaterial(MaterialHandles.ScrenSpaceShadow);
            softShadows = false;
            m_Disposed = true;
        }

        public override void Setup(CommandBuffer cmd, RenderTextureDescriptor baseDescriptor, int samples)
        {
            baseDescriptor.depthBufferBits = 0;
            baseDescriptor.colorFormat = m_ColorFormat;
            cmd.GetTemporaryRT(RenderTargetHandles.ScreenSpaceOcclusion, baseDescriptor, FilterMode.Bilinear);
            m_Disposed = false;
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            if (lightData.shadowData.renderedDirectionalShadowQuality == LightShadows.None)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Collect Shadows");
            SetShadowCollectPassKeywords(cmd, lightData.shadowData.directionalLightCascadeCount);

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            RenderTargetIdentifier screenSpaceOcclusionTexture = GetSurface(RenderTargetHandles.ScreenSpaceOcclusion);
            cmd.SetRenderTarget(screenSpaceOcclusionTexture);
            cmd.ClearRenderTarget(true, true, Color.white);
            cmd.Blit(screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceShadowsMaterial);

            if (cameraData.isStereoEnabled)
            {
                context.StartMultiEye(cameraData.camera);
                context.ExecuteCommandBuffer(cmd);
                context.StopMultiEye(cameraData.camera);
            }
            else
                context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Dispose(CommandBuffer cmd)
        {
            if (!m_Disposed)
            {
                cmd.ReleaseTemporaryRT(RenderTargetHandles.ScreenSpaceOcclusion);
                m_Disposed = true;
            }
        }

        void SetShadowCollectPassKeywords(CommandBuffer cmd, int cascadeCount)
        {
            CoreUtils.SetKeyword(cmd, LightweightKeywords.SoftShadowsText, softShadows);
            CoreUtils.SetKeyword(cmd, LightweightKeywords.CascadeShadowsText, cascadeCount > 1);
        }
    }
}
