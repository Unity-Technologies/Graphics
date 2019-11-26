using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Resolves shadows in a screen space texture.
    /// </summary>
    public class ScreenSpaceShadowResolvePass : ScriptableRenderPass
    {
        Material m_ScreenSpaceShadowsMaterial;
        RenderTargetHandle m_ScreenSpaceShadowmap;
        RenderTextureDescriptor m_RenderTextureDescriptor;
        const string m_ProfilerTag = "Resolve Shadows";

        public ScreenSpaceShadowResolvePass(RenderPassEvent evt, Material screenspaceShadowsMaterial)
        {
            m_ScreenSpaceShadowsMaterial = screenspaceShadowsMaterial;
            m_ScreenSpaceShadowmap.Init("_ScreenSpaceShadowmapTexture");
            renderPassEvent = evt;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor)
        {
            m_RenderTextureDescriptor = baseDescriptor;
            m_RenderTextureDescriptor.depthBufferBits = 0;
            m_RenderTextureDescriptor.msaaSamples = 1;
            m_RenderTextureDescriptor.colorFormat = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8
                : RenderTextureFormat.ARGB32;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(m_ScreenSpaceShadowmap.id, m_RenderTextureDescriptor, FilterMode.Bilinear);

            RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceShadowmap.Identifier();
            ConfigureTarget(screenSpaceOcclusionTexture);
            ConfigureClear(ClearFlag.All, Color.white);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ScreenSpaceShadowsMaterial == null)
            {
                Debug.LogErrorFormat("Missing {0}. {1} render pass will not execute. Check for missing reference in the renderer resources.", m_ScreenSpaceShadowsMaterial, GetType().Name);
                return;
            }

            if (renderingData.lightData.mainLightIndex == -1)
                return;

            ref Camera camera = ref renderingData.cameraData.camera;
            bool stereo = renderingData.cameraData.isStereoEnabled;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            if (URPCameraMode.isPureURP)
            {
                Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(Matrix4x4.identity, true);
                RenderingUtils.SetViewProjectionMatrices(cmd, Matrix4x4.identity, projMatrix, true);
                //XRTODO: ScreenSpace Shader Uses unity_CameraInvProjection to map from world space to camera clip space.
                // Currently unity_CameraInvProjection is managed by built-in. This need to be managed by SRP
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ScreenSpaceShadowsMaterial);
                RenderingUtils.SetViewProjectionMatrices(cmd, camera.worldToCameraMatrix, GL.GetGPUProjectionMatrix(camera.projectionMatrix, true), true);
            }
            else
            {
                if (!stereo)
                {
                    cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_ScreenSpaceShadowsMaterial);
                    cmd.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
                }
                else
                {
                    // Avoid setting and restoring camera view and projection matrices when in stereo.
                    RenderTargetIdentifier screenSpaceOcclusionTexture = m_ScreenSpaceShadowmap.Identifier();
                    Blit(cmd, screenSpaceOcclusionTexture, screenSpaceOcclusionTexture, m_ScreenSpaceShadowsMaterial);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            cmd.ReleaseTemporaryRT(m_ScreenSpaceShadowmap.id);
        }
    }
}
