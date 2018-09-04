using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        const string k_CollectShadowsTag = "Collect Shadows";
        RenderTextureFormat m_ColorFormat;

        public ScreenSpaceShadowResolvePass()
        {
            m_ColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
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

        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.lightData.mainLightIndex == -1)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_CollectShadowsTag);

            cmd.GetTemporaryRT(colorAttachmentHandle.id, descriptor, FilterMode.Bilinear);

            VisibleLight shadowLight = renderingData.lightData.visibleLights[renderingData.lightData.mainLightIndex];
            SetShadowCollectPassKeywords(cmd, ref shadowLight, ref renderingData.shadowData);

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            RenderTargetIdentifier screenSpaceOcclusionTexture = colorAttachmentHandle.Identifier();
            SetRenderTarget(cmd, screenSpaceOcclusionTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                ClearFlag.Color | ClearFlag.Depth, Color.white, descriptor.dimension);
            cmd.Blit(screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, renderer.GetMaterial(MaterialHandles.ScreenSpaceShadow));

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

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }

        void SetShadowCollectPassKeywords(CommandBuffer cmd, ref VisibleLight shadowLight, ref ShadowData shadowData)
        {
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.SoftShadows, shadowData.supportsSoftShadows && shadowLight.light.shadows == LightShadows.Soft);
            CoreUtils.SetKeyword(cmd, LightweightKeywordStrings.CascadeShadows, shadowData.directionalLightCascadeCount > 1);
        }
    }
}
