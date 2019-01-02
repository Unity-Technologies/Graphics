using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        const string k_CollectShadowsTag = "Collect Shadows";
        RenderTextureFormat m_ColorFormat;
        Material m_ScreenSpaceShadowsMaterial;

        public ScreenSpaceShadowResolvePass(Material screenspaceShadowsMaterial)
        {
            m_ColorFormat = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;

            m_ScreenSpaceShadowsMaterial = screenspaceShadowsMaterial;
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
        
        /// <inheritdoc/>
        public override void Execute(ScriptableRenderer renderer, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ScreenSpaceShadowsMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_ScreenSpaceShadowsMaterial, GetType().Name);
                return;
            }

            if (renderer == null)
                throw new ArgumentNullException("renderer");
            
            if (renderingData.lightData.mainLightIndex == -1)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(k_CollectShadowsTag);

            cmd.GetTemporaryRT(colorAttachmentHandle.id, descriptor, FilterMode.Bilinear);

            // Note: The source isn't actually 'used', but there's an engine peculiarity (bug) that
            // doesn't like null sources when trying to determine a stereo-ized blit.  So for proper
            // stereo functionality, we use the screen-space shadow map as the source (until we have
            // a better solution).
            // An alternative would be DrawProcedural, but that would require further changes in the shader.
            RenderTargetIdentifier screenSpaceOcclusionTexture = colorAttachmentHandle.Identifier();
            SetRenderTarget(cmd, screenSpaceOcclusionTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                ClearFlag.Color | ClearFlag.Depth, Color.white, descriptor.dimension);

            // This blit is troublesome. When MSAA is enabled it will render a fullscreen quad + store resolved MSAA + extra blit
            // This consumes about 10MB of extra unnecessary bandwidth on boat attack.
            // In order to avoid it we can do a cmd.DrawMesh instead, however because LWRP doesn't setup camera matrices itself,
            // we would need to call an extra SetupCameraProperties here just to setup those matrices which is also troublesome.
            // We need get rid of SetupCameraProperties and setup camera matrices in LWRP ASAP. 
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

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");
            
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(colorAttachmentHandle.id);
                colorAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }
}
